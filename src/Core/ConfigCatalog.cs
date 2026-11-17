using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace OC2ModSettings
{
    internal sealed class ConfigCatalog : IDisposable
    {
        internal sealed class Registration { internal string Name, Description; internal ConfigFile File; }
        internal readonly LeaseTable<string, Registration> Registrations;
        internal readonly LeaseTable<ConfigEntryBase, IDictionary<string, object>> Metadata;
        internal readonly LeaseTable<ConfigEntryBase, IDictionary<string, object>> Renderers;
        internal readonly LeaseTable<string, RegisteredAction> Actions;
        private readonly HashSet<ConfigFile> observed = new HashSet<ConfigFile>();
        private int dirty = 1;
        private int revision;
        internal int Revision { get { return Interlocked.CompareExchange(ref revision, 0, 0); } }
        internal ConfigCatalog()
        {
            Registrations = new LeaseTable<string, Registration>(Invalidate);
            Metadata = new LeaseTable<ConfigEntryBase, IDictionary<string, object>>(Invalidate);
            Renderers = new LeaseTable<ConfigEntryBase, IDictionary<string, object>>(Invalidate);
            Actions = new LeaseTable<string, RegisteredAction>(Invalidate);
        }
        internal void Invalidate() { Interlocked.Increment(ref revision); MarkDirty(); }
        internal void MarkDirty() { Interlocked.Exchange(ref dirty, 1); }
        internal bool ConsumeDirty() { return Interlocked.Exchange(ref dirty, 0) != 0; }
        private void Changed(object sender, SettingChangedEventArgs args) { MarkDirty(); }
        private void Reloaded(object sender, EventArgs args) { MarkDirty(); }

        internal List<ModConfig> Snapshot()
        {
            Dictionary<string, Registration> sources = new Dictionary<string, Registration>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, PluginInfo> pair in Chainloader.PluginInfos)
            {
                BaseUnityPlugin instance = pair.Value.Instance;
                if (instance == null) continue;
                sources[pair.Key] = new Registration { Name = pair.Value.Metadata.Name,
                    Description = pair.Value.Metadata.Version.ToString(), File = instance.Config };
            }
            foreach (string key in Registrations.Keys)
            {
                Registration registration;
                if (Registrations.TryGet(key, out registration)) sources[key] = registration;
            }
            List<RegisteredAction> actions = new List<RegisteredAction>();
            foreach (string key in Actions.Keys)
            {
                RegisteredAction action;
                if (!Actions.TryGet(key, out action)) continue;
                actions.Add(action);
                if (!sources.ContainsKey(action.Owner)) sources[action.Owner] = new Registration { Name = action.Owner };
            }
            List<ModConfig> result = new List<ModConfig>();
            HashSet<ConfigFile> used = new HashSet<ConfigFile>();
            foreach (KeyValuePair<string, Registration> pair in sources)
            {
                ConfigFile file = pair.Value.File;
                if (file != null)
                {
                    used.Add(file);
                    if (observed.Add(file)) { file.SettingChanged += Changed; file.ConfigReloaded += Reloaded; }
                }
                ModConfig mod = new ModConfig { Guid = pair.Key, Name = pair.Value.Name, Description = pair.Value.Description };
                // Keys is a loader-provided locked snapshot, unlike its enumerator.
                if (file != null) foreach (ConfigDefinition definition in file.Keys)
                {
                    try
                    {
                    if (!file.ContainsKey(definition)) continue;
                    ConfigEntryBase entry = file[definition];
                    IDictionary<string, object> metadata;
                    Metadata.TryGet(entry, out metadata);
                    EntryMetadata display = EntryMetadata.Read(entry, metadata);
                    if (!display.Hidden) mod.Rows.Add(new SettingRow { Owner = pair.Key, Entry = entry, Metadata = display });
                    }
                    catch (Exception error)
                    { if (ModSettingsPlugin.Instance != null) ModSettingsPlugin.Instance.Report("ConfigCatalog " + pair.Key + "/" + definition, error); }
                }
                foreach (RegisteredAction action in actions)
                {
                    if (action.Owner != pair.Key) continue;
                    if (action.Entry == null) mod.Rows.Add(new SettingRow { Owner = pair.Key, Action = action, Metadata = action.Metadata() });
                    else foreach (SettingRow row in mod.Rows)
                        if (ReferenceEquals(row.Entry, action.Entry)) row.Actions.Add(action);
                }
                foreach (SettingRow row in mod.Rows) row.Actions.Sort(delegate(RegisteredAction a, RegisteredAction b)
                { int c = b.Order.CompareTo(a.Order); return c == 0 ? StringComparer.Ordinal.Compare(a.Id, b.Id) : c; });
                CategorySort.Sort(mod.Rows);
                if (mod.Rows.Count > 0) result.Add(mod);
            }
            foreach (ConfigFile old in new List<ConfigFile>(observed))
                if (!used.Contains(old)) { old.SettingChanged -= Changed; old.ConfigReloaded -= Reloaded; observed.Remove(old); }
            result.Sort(delegate(ModConfig a, ModConfig b)
            {
                int c = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
                return c == 0 ? StringComparer.Ordinal.Compare(a.Guid, b.Guid) : c;
            });
            return result;
        }
        public void Dispose()
        {
            foreach (ConfigFile file in observed) { file.SettingChanged -= Changed; file.ConfigReloaded -= Reloaded; }
            observed.Clear();
        }
    }
}
