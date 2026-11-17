using System;
using System.Collections.Generic;
using System.Threading;
using BepInEx.Configuration;

namespace OC2ModSettings
{
    internal sealed class PinnedMods : IDisposable
    {
        internal const string FixedFirstGuid = "com.ch3ngyz.plugin.HostUtilities";
        internal static bool IsFixed(string guid) { return StringComparer.Ordinal.Equals(guid, FixedFirstGuid); }
        private sealed class ConfigurationManagerAttributes { public readonly bool Browsable = false; }
        private readonly ConfigEntry<string> entry;
        private readonly object gate = new object();
        private HashSet<string> values;
        private int revision;
        internal int Revision { get { return Interlocked.CompareExchange(ref revision, 0, 0); } }
        internal PinnedMods(ConfigFile file)
        {
            entry = file.Bind("Navigation", "PinnedMods", "", new ConfigDescription(
                "Pinned MOD GUIDs, one per line. Display names may change without losing pins.", null,
                new ConfigurationManagerAttributes()));
            values = Read(entry.Value); entry.SettingChanged += Changed;
        }
        private static HashSet<string> Read(string raw)
        { return new HashSet<string>((raw ?? "").Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal); }
        private void Changed(object sender, EventArgs args)
        { lock (gate) values = Read(entry.Value); Interlocked.Increment(ref revision); }
        internal bool Contains(string guid) { lock (gate) return !IsFixed(guid) && values.Contains(guid); }
        internal int Compare(ModConfig a, ModConfig b)
        {
            int order = IsFixed(b.Guid).CompareTo(IsFixed(a.Guid));
            if (order == 0) order = Contains(b.Guid).CompareTo(Contains(a.Guid));
            if (order == 0) order = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name);
            return order == 0 ? StringComparer.Ordinal.Compare(a.Guid, b.Guid) : order;
        }
        internal CommitResult Toggle(string guid)
        {
            if (IsFixed(guid)) return new CommitResult { Success = true, ActualValue = entry.Value };
            List<string> next;
            lock (gate)
            {
                HashSet<string> copy = new HashSet<string>(values, StringComparer.Ordinal);
                copy.Remove(FixedFirstGuid);
                if (!copy.Remove(guid)) copy.Add(guid);
                next = new List<string>(copy);
            }
            next.Sort(StringComparer.Ordinal);
            return ConfigCommit.Apply(new SettingRow { Entry = entry, Metadata = EntryMetadata.Read(entry, null) }, string.Join("\n", next.ToArray()));
        }
        public void Dispose() { entry.SettingChanged -= Changed; }
    }
}
