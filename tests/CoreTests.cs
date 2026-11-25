using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;

internal static partial class CoreTests
{
    private static int assertions;
    private static void Check(bool value, string label) { assertions++; if (!value) throw new Exception(label); }
    private static SettingRow Row<T>(ConfigFile file, string key, T value, AcceptableValueBase limits)
    {
        ConfigEntry<T> entry = file.Bind("Core", key, value, new ConfigDescription("Test", limits, new object[0]));
        return new SettingRow { Entry = entry, Metadata = EntryMetadata.Read(entry, null) };
    }
    public static int Main(string[] args)
    {
        try
        {
            string root = args[0]; Directory.CreateDirectory(root);
            int changed = 0; LeaseTable<string, int> leases = new LeaseTable<string, int>(delegate { changed++; });
            IDisposable old = leases.Set("same", 1), current = leases.Set("same", 2); old.Dispose();
            int value; Check(leases.TryGet("same", out value) && value == 2, "stale lease cannot erase successor");
            current.Dispose(); current.Dispose(); Check(!leases.TryGet("same", out value) && changed == 3, "current lease release idempotent");
            ConfigFile file = new ConfigFile(Path.Combine(root, "normal.cfg"), false); file.SaveOnConfigSet = false;
            SettingRow decimalRow = Row(file, "decimal", decimal.MaxValue, null);
            Check((decimal)ValueCodec.Parse(decimalRow, decimal.MinValue.ToString()) == decimal.MinValue, "decimal exact minimum");
            SettingRow longRow = Row(file, "long", long.MaxValue, null);
            Check((long)ValueCodec.Parse(longRow, "9223372036854775807") == long.MaxValue, "long exact maximum");
            SettingRow ulongRow = Row(file, "ulong", ulong.MaxValue, null);
            Check((ulong)ValueCodec.Parse(ulongRow, "18446744073709551615") == ulong.MaxValue, "ulong exact maximum");
            SettingRow range = Row(file, "range", 4, new AcceptableValueRange<int>(0, 10));
            Check((int)ValueCodec.Parse(range, "10") == 10, "range inclusive bound");
            bool rejected = false; try { ValueCodec.Parse(range, "11"); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected && (int)range.Entry.BoxedValue == 4, "out of range has no write");
            CommitResult success = ConfigCommit.Apply(range, 9);
            Check(success.Success && (int)success.ActualValue == 9 && File.Exists(file.ConfigFilePath), "disabled autosave explicitly saved");
            Check(!file.SaveOnConfigSet, "save policy unchanged");
            SettingRow text = Row(file, "string", "first", null);
            Check((string)ValueCodec.Parse(text, "raw \\ value") == "raw \\ value", "string preserves literal content");
            Dictionary<string, object> metadata = new Dictionary<string, object> { { "Browsable", false }, { "ReadOnly", true }, { "IsAdvanced", true }, { "DispName", "Name" } };
            EntryMetadata display = EntryMetadata.Read(text.Entry, metadata);
            Check(display.Hidden && display.ReadOnly && display.Advanced && display.Name == "Name", "metadata fields");
            text.Metadata.ReadOnly = true; Check(!ConfigCommit.Apply(text, "next").Success, "readonly rejects commit");
            foreach (bool autosave in new bool[] { false, true })
            {
                ConfigFile broken = new ConfigFile(Path.Combine(root, autosave ? "broken-auto" : "broken-manual"), false);
                broken.SaveOnConfigSet = false; SettingRow brokenRow = Row(broken, "value", 2, null);
                Directory.CreateDirectory(broken.ConfigFilePath); broken.SaveOnConfigSet = autosave;
                CommitResult failed = ConfigCommit.Apply(brokenRow, 3);
                Check(!failed.Success && (int)failed.ActualValue == 3 && !string.IsNullOrEmpty(failed.Error), "save failure reports actual mutated value");
                Check(broken.SaveOnConfigSet == autosave, "failure preserves save policy");
            }
            SettingRow floating = Row(file, "floating", 0.5, null);
            rejected = false; try { ValueCodec.Parse(floating, "NaN"); } catch (FormatException) { rejected = true; }
            Check(rejected, "NaN rejected");
            Check(!ConfigCommit.Apply(floating, double.PositiveInfinity).Success && (double)floating.Entry.BoxedValue == .5, "direct custom commit rejects non-finite value");
            Check(ValueCodec.Format(longRow, long.MaxValue) == "9223372036854775807", "no precision loss from float conversion");
            Dictionary<string, object> actionSpec = new Dictionary<string, object> { { "Version", 1 }, { "DisplayName", "Business" }, { "Execute", new Action(delegate { }) } };
            RegisteredAction action = RegisteredAction.Read("owner", "action", actionSpec);
            actionSpec["DisplayName"] = "mutated";
            Check(action.Name == "Business" && action.Entry == null && action.Metadata().HideDefault, "actions copy descriptors and require no config entry");
            Check(new SettingRow().GetActionCaption(action) == "Business", "an action without a custom caption uses its title");
            foreach (string absent in new string[] { null, "" })
            {
                actionSpec["ButtonLabel"] = absent;
                Check(new SettingRow().GetActionCaption(RegisteredAction.Read("owner", "action", actionSpec)) == "mutated",
                    "null or empty action caption uses its title");
            }
            actionSpec["ButtonLabel"] = "First\nSecond";
            RegisteredAction captioned = RegisteredAction.Read("owner", "action", actionSpec);
            actionSpec["ButtonLabel"] = "mutated again";
            Check(captioned.Metadata().Name == "mutated" && captioned.ButtonLabel == "First\nSecond",
                "custom caption is copied and does not change the row title");
            SettingRow captionedRow = new SettingRow { Action = captioned, Metadata = captioned.Metadata() };
            Check(captionedRow.Matches("mutated", false) && !captionedRow.Matches("First", false), "only the action title, not its button caption, is searchable");
            SettingRow actionRow = new SettingRow { Action = action, Metadata = action.Metadata() };
            Check(!ValueCodec.Editable(actionRow) && !ConfigCommit.Apply(actionRow, true).Success, "business rows cannot be parsed or committed as configuration");
            rejected = false; try { RegisteredAction.Read("owner", "bad\nid", actionSpec); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "action IDs cannot collide through separator injection");
            actionSpec["CanExecute"] = "invalid";
            rejected = false; try { RegisteredAction.Read("owner", "action", actionSpec); } catch (ArgumentException) { rejected = true; }
            Check(rejected, "action contract rejects incorrectly typed optional callbacks");
            ConfigFile navigation = new ConfigFile(Path.Combine(root, "pins.cfg"), false); navigation.SaveOnConfigSet = false;
            ConfigEntry<string> savedPins = navigation.Bind("Navigation", "PinnedMods", PinnedMods.FixedFirstGuid + "\nfixture.pinned", "");
            using (PinnedMods pins = new PinnedMods(navigation))
            {
                ModConfig host = new ModConfig { Guid = PinnedMods.FixedFirstGuid, Name = "ZZ HostUtilities" };
                ModConfig pinned = new ModConfig { Guid = "fixture.pinned", Name = "AA Pinned" };
                ModConfig normal = new ModConfig { Guid = "fixture.normal", Name = "ZZ Normal" };
                List<ModConfig> mods = new List<ModConfig>(new ModConfig[] { normal, pinned, host }); mods.Sort(pins.Compare);
                Check(mods[0] == host && mods[1] == pinned && mods[2] == normal, "HostUtilities fixed row precedes pinned and ordinary MODs");
                string oldPins = savedPins.Value; int oldRevision = pins.Revision;
                Check(pins.Toggle(host.Guid).Success && !pins.Contains(host.Guid) && savedPins.Value == oldPins && pins.Revision == oldRevision,
                    "fixed row ignores pin toggles without saving or invalidating the page");
                Check(pins.Toggle(normal.Guid).Success && pins.Contains(pinned.Guid) && pins.Contains(normal.Guid)
                    && !savedPins.Value.Contains(host.Guid), "normal pin updates preserve other pins and remove legacy fixed-row entry");
                mods.Remove(host); mods.Sort(pins.Compare);
                Check(mods[0] == normal && mods[1] == pinned, "absent HostUtilities creates no placeholder and pinned MODs sort by GUID rather than display name");
            }
            VerifyCategoryOrder(root);
            VerifySdk(root);
            VerifyLocalization(root);
            VerifyTranslationFiles(root);
            VerifyTranslations(root);
            VerifyShortcutConflicts(root);
            Console.WriteLine("PASS: " + assertions + " core assertions"); return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
