using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private ShortcutConflicts shortcutConflicts;
        private int shortcutRevision;

        internal bool HasShortcutConflict(SettingRow row)
        { return shortcutConflicts != null && shortcutConflicts.Find(row.Entry) != null; }

        private void RefreshShortcutConflicts(IList<ModConfig> snapshot)
        {
            shortcutRevision = Catalog.ShortcutRevision;
            shortcutConflicts = new ShortcutConflicts(snapshot, Shortcut.Setting);
            shortcutWarning.Show(HasShortcutConflict(Shortcut.Setting));
            foreach (EditorRow editor in editors) editor.RefreshShortcutWarning();
        }

        internal CommitResult CommitSetting(SettingRow row, object value)
        {
            CommitResult result = ConfigCommit.Apply(row, value);
            if (ShortcutConflicts.Tracks(row.Entry)) RefreshShortcutConflicts(mods);
            return result;
        }

        private string ShortcutDetail(SettingRow row)
        {
            IList<ShortcutBinding> group = row == null || shortcutConflicts == null ? null : shortcutConflicts.Find(row.Entry);
            if (group == null) return null;
            List<string> others = new List<string>(2);
            foreach (ShortcutBinding binding in group)
            {
                if (ReferenceEquals(binding.Row.Entry, row.Entry)) continue;
                others.Add(binding.Mod == null ? Texts.Title + " · " + Texts.TogglePanel
                    : CompactShortcutName(Catalog.GetDisplayName(binding.Mod)) + " · " + CompactShortcutName(binding.Row.DisplayName));
                if (others.Count == 2) break;
            }
            int remaining = group.Count - 1 - others.Count;
            string key = row == Shortcut.Setting ? ValueCodec.Format(row, Shortcut.Key) : Convert.ToString(row.Entry.BoxedValue);
            return Texts.ShortcutDuplicate(key, string.Join("; ", others.ToArray())
                + (remaining > 0 ? Texts.ShortcutDuplicateMore(remaining) : ""));
        }

        private static string CompactShortcutName(string value)
        {
            string name = (value ?? "").Replace('\r', ' ').Replace('\n', ' ').Trim();
            return name.Length > 36 ? name.Substring(0, 35) + "…" : name;
        }
    }
}
