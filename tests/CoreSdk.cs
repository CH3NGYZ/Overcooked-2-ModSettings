extern alias ConsumerSdk;
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using ModSettingsAttributes = ConsumerSdk::OC2ModSettings.SDK.ModSettingsAttributes;

internal static partial class CoreTests
{
    private static void VerifySdk(string root)
    {
        Check(typeof(ModSettingsAttributes).Assembly != typeof(EntryMetadata).Assembly,
            "SDK tags are consumed from a separate assembly without type identity coupling");
        string[] references = Array.ConvertAll(typeof(ModSettingsAttributes).Assembly.GetReferencedAssemblies(),
            delegate(System.Reflection.AssemblyName name) { return name.Name; });
        Check(Array.IndexOf(references, "OC2ModSettings") < 0 && Array.IndexOf(references, "ConfigurationManager") < 0,
            "source SDK has no service or Configuration Manager assembly dependency");
        ConfigFile file = new ConfigFile(Path.Combine(root, "sdk.cfg"), false); file.SaveOnConfigSet = false;
        ConfigurationManagerAttributes legacy = new ConfigurationManagerAttributes { DispName = "Legacy", Category = "Legacy category",
            Description = "Legacy help", CategoryOrder = 10, Order = 3, Browsable = false, ReadOnly = true };
        ModSettingsAttributes attributes = new ModSettingsAttributes { DispName = "Enabled", Category = "General", Description = "Help",
            CategoryOrder = 100, Order = 20, Browsable = true, ReadOnly = false, IsAdvanced = true,
            ShowRangeAsPercent = true, HideDefaultButton = true };
        ConfigEntry<int> entry = file.Bind("StableSection", "StableKey", 4, new ConfigDescription("Fallback", null, attributes, legacy));
        EntryMetadata original = EntryMetadata.Read(entry, null);
        Check(original.Name == "Enabled" && original.Category == "General" && original.Description == "Help"
            && original.CategoryOrder == 100 && original.Order == 20, "SDK fields override legacy tags even when legacy tag is last");
        Check(!original.Hidden && !original.ReadOnly && original.Advanced && original.Percent && original.HideDefault,
            "SDK nullable true and false flags override legacy metadata");
        ConfigEntry<int> reverse = file.Bind("StableSection", "ReversedTags", 4, new ConfigDescription("Fallback", null, legacy, attributes));
        Check(original.SameAs(EntryMetadata.Read(reverse, null)), "new metadata precedence is independent of tag order");
        EntryMetadata explicitDisplay = EntryMetadata.Read(entry, new Dictionary<string, object> {
            { "DispName", "Explicit" }, { "CategoryOrder", -10 }, { "Browsable", false }, { "Order", 0 } });
        Check(explicitDisplay.Name == "Explicit" && explicitDisplay.CategoryOrder == -10 && explicitDisplay.Hidden
            && explicitDisplay.Order == 0 && explicitDisplay.Category == "General", "API dictionary remains the final metadata override");
        ConfigEntry<string> partial = file.Bind("StableSection", "Partial", "value",
            new ConfigDescription("Fallback", null, new ModSettingsAttributes { DispName = "Only name" }, legacy));
        EntryMetadata fallback = EntryMetadata.Read(partial, null);
        Check(fallback.Name == "Only name" && fallback.Category == "Legacy category" && fallback.Hidden && fallback.ReadOnly
            && fallback.Order == 3, "null SDK fields preserve earlier metadata rather than injecting defaults");
        ConfigEntry<string> empty = file.Bind("RawSection", "RawKey", "value", new ConfigDescription("Raw help", null, new ModSettingsAttributes()));
        EntryMetadata defaults = EntryMetadata.Read(empty, null);
        Check(defaults.Name == "RawKey" && defaults.Category == "RawSection" && defaults.Description == "Raw help"
            && !defaults.Hidden && !defaults.CategoryOrder.HasValue, "empty SDK attributes preserve native ConfigEntry defaults");
        attributes.DispName = "开启功能"; attributes.Category = "常用功能"; attributes.Description = "当前语言说明";
        EntryMetadata translated = EntryMetadata.Read(entry, null);
        Check(!original.SameAs(translated) && original.Name == "Enabled" && translated.Name == "开启功能"
            && translated.Category == "常用功能" && translated.Description == "当前语言说明", "localized mutable tags produce a fresh changed snapshot");
        Check(entry.Definition.Section == "StableSection" && entry.Definition.Key == "StableKey" && entry.Value == 4
            && translated.Order == original.Order && translated.CategoryOrder == original.CategoryOrder
            && !file.SaveOnConfigSet && !File.Exists(file.ConfigFilePath), "language metadata does not rewrite config identity, value, order or disk");
        attributes.Description = "";
        Check(EntryMetadata.Read(entry, null).Description == "", "empty localized description clears previous help");
        attributes.Order = 0; attributes.CategoryOrder = -1;
        Check(EntryMetadata.Read(entry, null).Order == 0 && EntryMetadata.Read(entry, null).CategoryOrder == -1,
            "nullable integer fields preserve explicit zero and negative priorities");
        attributes.ObjToStr = delegate(object value) { return "v=" + value; };
        attributes.StrToObj = delegate(string value) { return int.Parse(value.Substring(2)); };
        SettingRow row = new SettingRow { Entry = entry, Metadata = EntryMetadata.Read(entry, null) };
        Check(ValueCodec.Format(row, 7) == "v=7" && (int)ValueCodec.Parse(row, "v=9") == 9,
            "SDK conversion delegates reach the shared value codec");
        using (ConfigCatalog catalog = new ConfigCatalog())
        {
            Check(catalog.ConsumeDirty() && !catalog.ConsumeDirty(), "catalog consumes metadata dirtiness once");
            int revision = catalog.Revision;
            using (catalog.Metadata.Set(entry, new Dictionary<string, object> { { "DispName", "Registered" } }))
                Check(catalog.Revision == revision + 1 && catalog.ConsumeDirty(), "metadata registration requests the existing page rebuild path");
            Check(catalog.Revision == revision + 2 && catalog.ConsumeDirty(), "metadata lease disposal requests a fresh display snapshot");
            catalog.Invalidate(); catalog.Invalidate();
            Check(catalog.Revision == revision + 4 && catalog.ConsumeDirty() && !catalog.ConsumeDirty(),
                "explicit refresh notifications coalesce into one pending UI update");
        }
    }
}
