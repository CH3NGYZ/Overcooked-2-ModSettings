using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;

internal static partial class CoreTests
{
    private static void VerifyShortcutConflicts(string root)
    {
        ConfigFile first = new ConfigFile(Path.Combine(root, "shortcuts-first.cfg"), false);
        ConfigFile second = new ConfigFile(Path.Combine(root, "shortcuts-second.cfg"), false);
        first.SaveOnConfigSet = second.SaveOnConfigSet = false;
        SettingRow single = Row(first, "Single", KeyCode.F8, null);
        SettingRow exact = Row(first, "Exact", new KeyboardShortcut(KeyCode.F8), null);
        SettingRow pair = Row(first, "Pair", new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl, KeyCode.LeftShift), null);
        SettingRow otherModifier = Row(first, "OtherModifier", new KeyboardShortcut(KeyCode.F8, KeyCode.RightControl, KeyCode.LeftShift), null);
        SettingRow unique = Row(first, "Unique", KeyCode.F11, null);
        SettingRow none = Row(first, "None", KeyCode.None, null);
        SettingRow leftControl = Row(second, "LeftControl", new KeyboardShortcut(KeyCode.F8, KeyCode.LeftControl), null);
        SettingRow reordered = Row(second, "Reordered", new KeyboardShortcut(KeyCode.F8, KeyCode.LeftShift, KeyCode.LeftControl), null);
        SettingRow hidden = Row(second, "Hidden", KeyCode.F11, null);
        SettingRow unbound = Row(second, "Unbound", default(KeyboardShortcut), null);
        SettingRow text = Row(second, "Text", "F8", new AcceptableValueList<string>("F8", "F11"));
        SettingRow number = Row(second, "Number", (int)KeyCode.F8, null);
        SettingRow readOnly = Row(second, "Single", KeyCode.F8, null);
        SettingRow toggle = Row(second, "Toggle", KeyCode.F8, null);
        SettingRow toggleAlias = new SettingRow { Entry = toggle.Entry, Metadata = EntryMetadata.Read(toggle.Entry, null) };
        SettingRow alias = new SettingRow { Entry = single.Entry, Metadata = EntryMetadata.Read(single.Entry, null) };
        SettingRow action = new SettingRow { Metadata = new EntryMetadata { Name = "F8" } };
        pair.Metadata.Advanced = true;
        readOnly.Metadata.ReadOnly = true;
        hidden.Metadata.Hidden = toggle.Metadata.Hidden = true;
        unique.Metadata.Format = delegate { return "F8"; };
        single.Metadata.Name = "Local name";
        ModConfig firstMod = new ModConfig { Guid = "first", Name = "First MOD" };
        ModConfig secondMod = new ModConfig { Guid = "second", Name = "Second MOD" };
        firstMod.Rows.AddRange(new[] { single, exact, pair, otherModifier, unique, none, alias, toggleAlias });
        secondMod.Rows.AddRange(new[] { leftControl, reordered, hidden, unbound, text, number, readOnly, alias, action });
        ModConfig[] mods = { firstMod, secondMod };
        ShortcutConflicts conflicts = new ShortcutConflicts(mods, toggle);

        IList<ShortcutBinding> singles = conflicts.Find(single.Entry);
        Check(singles != null && singles.Count == 4 && singles == conflicts.Find(exact.Entry)
            && singles == conflicts.Find(readOnly.Entry) && singles == conflicts.Find(toggle.Entry),
            "same and cross-MOD single-key bindings share one group, including the explicit panel toggle");
        Check(singles[0].Row == toggle && singles[0].Mod == null,
            "panel toggle uses its explicit hidden descriptor once, instead of its catalog alias");
        Check(singles[1].Row == single && singles[1].Mod == firstMod && singles[3].Mod == secondMod,
            "duplicate descriptors retain catalog row and MOD identity while aliases do not add candidates");
        Check(singles.IsReadOnly && conflicts.Find(null) == null,
            "callers get a read-only group and no matches for action-only rows");
        IList<ShortcutBinding> modifiers = conflicts.Find(pair.Entry);
        Check(modifiers != null && modifiers.Count == 2 && modifiers == conflicts.Find(reordered.Entry)
            && modifiers != singles, "matching modifiers in any order form a different duplicate group from the same main key alone");
        Check(conflicts.Find(otherModifier.Entry) == null && conflicts.Find(leftControl.Entry) == null,
            "modifier sides and different modifier sets do not count as the same shortcut");
        Check(conflicts.Count(firstMod) == 3 && conflicts.Count(secondMod) == 3,
            "counts include advanced and read-only rows, count entries once per MOD, and exclude the panel toggle row");
        Check(conflicts.Count(new ModConfig { Guid = firstMod.Guid }) == 3 && conflicts.Count(null) == 0,
            "counts follow MOD identity between catalog snapshots and handle no selected MOD");
        Check(conflicts.Find(none.Entry) == null && conflicts.Find(unbound.Entry) == null,
            "None and the default KeyboardShortcut are unbound");
        Check(conflicts.Find(unique.Entry) == null && conflicts.Find(hidden.Entry) == null,
            "hidden settings do not conflict with visible ones unless explicitly supplied as the panel toggle");
        Check(conflicts.Find(text.Entry) == null && conflicts.Find(number.Entry) == null && conflicts.Find(unique.Entry) == null,
            "detection never parses strings, numbers, names or formatted key labels");

        exact.Entry.BoxedValue = new KeyboardShortcut(KeyCode.F12);
        readOnly.Entry.BoxedValue = KeyCode.F12;
        toggle.Entry.BoxedValue = KeyCode.F9;
        ShortcutConflicts changed = new ShortcutConflicts(mods, toggle);
        Check(changed.Find(single.Entry) == null && changed.Find(toggle.Entry) == null && singles.Count == 4,
            "new snapshots reflect saved values without modifying existing groups");
        Check(changed.Find(exact.Entry).Count == 2 && changed.Find(exact.Entry) == changed.Find(readOnly.Entry)
            && changed.Count(firstMod) == 2 && changed.Count(secondMod) == 2,
            "replacing the snapshot clears old duplicates and discovers newly repeated bindings across MODs");
        Check(!File.Exists(first.ConfigFilePath) && !File.Exists(second.ConfigFilePath), "shortcut detection never saves configuration");
    }
}
