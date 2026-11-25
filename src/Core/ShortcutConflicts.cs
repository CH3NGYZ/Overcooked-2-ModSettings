using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace OC2ModSettings
{
    internal sealed class ShortcutBinding
    {
        internal readonly SettingRow Row;
        internal readonly ModConfig Mod;

        internal ShortcutBinding(SettingRow row, ModConfig mod)
        { Row = row; Mod = mod; }
    }

    internal sealed class ShortcutConflicts
    {
        private readonly Dictionary<ConfigEntryBase, IList<ShortcutBinding>> duplicates =
            new Dictionary<ConfigEntryBase, IList<ShortcutBinding>>();
        private readonly Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);

        internal ShortcutConflicts(IList<ModConfig> mods, SettingRow toggle)
        {
            Dictionary<KeyboardShortcut, List<ShortcutBinding>> groups = new Dictionary<KeyboardShortcut, List<ShortcutBinding>>();
            HashSet<ConfigEntryBase> seen = new HashSet<ConfigEntryBase>();
            if (toggle != null) Add(groups, seen, toggle, null);
            foreach (ModConfig mod in mods)
                foreach (SettingRow row in mod.Rows)
                    if (!row.Metadata.Hidden) Add(groups, seen, row, mod);

            foreach (List<ShortcutBinding> group in groups.Values)
            {
                if (group.Count < 2) continue;
                IList<ShortcutBinding> matches = group.AsReadOnly();
                foreach (ShortcutBinding binding in group) duplicates.Add(binding.Row.Entry, matches);
            }

            if (duplicates.Count == 0) return;
            ConfigEntryBase toggleEntry = toggle == null ? null : toggle.Entry;
            foreach (ModConfig mod in mods)
            {
                seen.Clear();
                foreach (SettingRow row in mod.Rows)
                    if (!row.Metadata.Hidden && row.Entry != null && !ReferenceEquals(row.Entry, toggleEntry)
                        && duplicates.ContainsKey(row.Entry)) seen.Add(row.Entry);
                if (seen.Count > 0) counts.Add(mod.Guid, seen.Count);
            }
        }

        internal IList<ShortcutBinding> Find(ConfigEntryBase entry)
        {
            IList<ShortcutBinding> matches;
            return entry != null && duplicates.TryGetValue(entry, out matches) ? matches : null;
        }

        internal int Count(ModConfig mod)
        {
            int count;
            return mod != null && counts.TryGetValue(mod.Guid, out count) ? count : 0;
        }

        internal static bool Tracks(ConfigEntryBase entry)
        { return entry != null && (entry.SettingType == typeof(KeyCode) || entry.SettingType == typeof(KeyboardShortcut)); }

        private static void Add(Dictionary<KeyboardShortcut, List<ShortcutBinding>> groups,
            HashSet<ConfigEntryBase> seen, SettingRow row, ModConfig mod)
        {
            ConfigEntryBase entry = row.Entry;
            if (!Tracks(entry) || !seen.Add(entry)) return;

            KeyboardShortcut shortcut = entry.SettingType == typeof(KeyboardShortcut)
                ? (KeyboardShortcut)entry.BoxedValue : new KeyboardShortcut((KeyCode)entry.BoxedValue);
            if (shortcut.MainKey == KeyCode.None) return;

            List<ShortcutBinding> group;
            if (!groups.TryGetValue(shortcut, out group)) groups.Add(shortcut, group = new List<ShortcutBinding>());
            group.Add(new ShortcutBinding(row, mod));
        }
    }
}
