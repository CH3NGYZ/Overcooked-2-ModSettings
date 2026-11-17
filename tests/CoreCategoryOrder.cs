using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;

internal sealed class ConfigurationManagerAttributes
{
    public int? CategoryOrder;
    public int Order;
    public string Category;
}

internal static partial class CoreTests
{
    private static void VerifyCategoryOrder(string root)
    {
        ConfigFile config = new ConfigFile(Path.Combine(root, "category-order.cfg"), false);
        config.SaveOnConfigSet = false;
        ConfigEntry<bool> mode = config.Bind("03-LegacyOptions", "EnableScore", false,
            new ConfigDescription("Mode", null, new ConfigurationManagerAttributes { Category = "常用功能", CategoryOrder = 300, Order = 20 }));
        ConfigEntry<string> language = config.Bind("01-LegacyLanguage", "Language", "zh",
            new ConfigDescription("Language", null, new ConfigurationManagerAttributes { Category = "语言", CategoryOrder = 500, Order = 0 }));
        ConfigEntry<string> name = config.Bind("02-LegacyName", "PlayerName", "original", "Name");
        EntryMetadata original = EntryMetadata.Read(mode, null);
        EntryMetadata overridden = EntryMetadata.Read(mode, new Dictionary<string, object> { { "CategoryOrder", 400 } });
        Check(original.CategoryOrder == 300 && original.Order == 20 && overridden.CategoryOrder == 400 && overridden.Order == 20,
            "category order reads nullable public tags and override without changing row order");
        Check(!original.SameAs(overridden), "category order changes invalidate page shape");
        RegisteredAction update = RegisteredAction.Read("owner", "check-update", new Dictionary<string, object> {
            { "Version", 1 }, { "DisplayName", "检查更新" }, { "Execute", new Action(delegate {}) },
            { "Category", "常用功能" }, { "CategoryOrder", 300 }, { "Order", 19 } });
        Check(update.CategoryOrder == 300 && update.Metadata().CategoryOrder == 300 && update.Metadata().Order == 19,
            "standalone actions carry independent category and row order");
        List<SettingRow> rows = new List<SettingRow>(new SettingRow[] {
            new SettingRow { Entry = mode, Metadata = original }, new SettingRow { Action = update, Metadata = update.Metadata() },
            new SettingRow { Entry = language, Metadata = EntryMetadata.Read(language, null) },
            new SettingRow { Entry = name, Metadata = EntryMetadata.Read(name, new Dictionary<string, object> { { "Category", "改名" }, { "CategoryOrder", 400 } }) } });
        CategorySort.Sort(rows);
        Check(rows[0].Entry == language && rows[1].Entry == name && rows[2].Entry == mode && rows[3].Action == update,
            "category rank descends independently and update directly follows mode");
        foreach (SettingRow row in rows) row.Metadata.Category = row.Entry == language ? "ZZ Language" : row.Entry == name ? "AA Rename" : "MM Options";
        CategorySort.Sort(rows);
        Check(rows[0].Entry == language && rows[1].Entry == name && rows[2].Entry == mode && rows[3].Action == update,
            "translated display categories preserve section and action positions");
        rows[2].Metadata.CategoryOrder = -2; rows[3].Metadata.CategoryOrder = -1; rows[1].Metadata.CategoryOrder = null;
        rows[0].Metadata.CategoryOrder = -4; CategorySort.Sort(rows);
        Check(rows[0].Entry == name && rows[1].Entry == mode && rows[2].Action == update && rows[3].Entry == language,
            "group takes highest explicit rank including negative values and defaults to zero only when absent");
        foreach (SettingRow row in rows) row.Metadata.CategoryOrder = 0;
        CategorySort.Sort(rows); string stable = rows[0].Identity + rows[1].Identity + rows[2].Identity + rows[3].Identity;
        foreach (SettingRow row in rows) row.Metadata.Category = "translated-" + (row.Entry == language ? "Z" : row.Entry == name ? "Y" : "A");
        CategorySort.Sort(rows);
        Check(stable == rows[0].Identity + rows[1].Identity + rows[2].Identity + rows[3].Identity,
            "equal rank categories use original section identity instead of localized text");
        config.Save(); ConfigFile disk = new ConfigFile(config.ConfigFilePath, true);
        Check(!disk.Bind("03-LegacyOptions", "EnableScore", true).Value
            && disk.Bind("01-LegacyLanguage", "Language", "missing").Value == "zh"
            && disk.Bind("02-LegacyName", "PlayerName", "missing").Value == "original", "display order preserves historical cfg section and key identities");
        bool rejected = false;
        try { RegisteredAction.Read("owner", "bad", new Dictionary<string, object> { { "Version", 1 }, { "DisplayName", "bad" },
            { "Execute", new Action(delegate {}) }, { "CategoryOrder", "300" } }); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "action category order rejects incorrect field types");
    }
}
