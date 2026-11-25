extern alias ConsumerSdk;
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using Consumer = ConsumerSdk::OC2ModSettings.SDK.ModSettingsClient;
using Keys = ConsumerSdk::OC2ModSettings.SDK.ModSettingsTranslations;

internal static partial class CoreTests
{
    private static void VerifyTranslations(string root)
    {
        const string owner = "fixture.translation";
        const string section = "Cfg", group = "Layout~/Pan";
        const string actionId = "inspect/status", configKey = "Zoom~/Max";
        string nameKey = Keys.ConfigName(section, configKey), descriptionKey = Keys.ConfigDescription(section, configKey);
        string titleKey = Keys.ActionTitle(actionId), labelKey = Keys.ActionLabel(actionId), groupKey = Keys.Group(group);
        Check(groupKey == "group/Layout~0~1Pan" && nameKey == "config/Cfg/Zoom~0~1Max" && descriptionKey == nameKey + "/description"
            && titleKey == "action/inspect~1status/title" && labelKey == "action/inspect~1status/label"
            && Keys.ActionDescription(actionId) == "action/inspect~1status/description", "independent source SDK generates the exact escaped field keys");
        Check(!new Consumer().SupportsTranslations && typeof(ModSettingsPlugin).GetMethod("RegisterTranslationsV1",
            new Type[] { typeof(string), typeof(IDictionary<string, IDictionary<string, string>>) }).ReturnType == typeof(IDisposable),
            "translation is an optional BCL-only API v1 capability");

        string translationRoot = Path.Combine(root, "resolver");
        string english = Directory.CreateDirectory(Path.Combine(translationRoot, "en")).FullName;
        string japanese = Directory.CreateDirectory(Path.Combine(translationRoot, "ja")).FullName;
        File.WriteAllLines(Path.Combine(english, owner + ".txt"), new string[] {
            Keys.ModName + "=English file mod", groupKey + "=English file group", nameKey + "=English file zoom", titleKey + "=English file action"
        });
        File.WriteAllLines(Path.Combine(japanese, owner + ".txt"), new string[] {
            Keys.ModName + "=ファイルMOD", nameKey + "=ファイル倍率", descriptionKey + "=ファイル説明", titleKey + "=ファイル操作"
        });
        ConfigFile file = new ConfigFile(Path.Combine(root, "translation-values.cfg"), false) { SaveOnConfigSet = false };
        ConfigEntry<string> entry = file.Bind(section, configKey, "near", new ConfigDescription("Raw help", new AcceptableValueList<string>("near", "far")));
        EntryMetadata metadata = EntryMetadata.Read(entry, new Dictionary<string, object> { { "Category", group }, { "DispName", "Raw zoom" }, { "Order", 7 } });

        using (ConfigCatalog catalog = new ConfigCatalog())
        using (catalog.RegisterModDisplayName(owner, "Single-language MOD name"))
        {
            catalog.Translations.LoadFiles(translationRoot, null);
            catalog.ConsumeDirty();
            SettingRow row = new SettingRow { Owner = owner, Entry = entry, Metadata = metadata, Translations = catalog.Translations };
            RegisteredAction action = RegisteredAction.Read(owner, actionId, new Dictionary<string, object> {
                { "Version", 1 }, { "DisplayName", "Raw action" }, { "Execute", new Action(delegate {}) }, { "Category", group }, { "Entry", entry }
            });
            row.Actions.Add(action);
            ModConfig mod = new ModConfig { Guid = owner, Name = "Raw MOD" };
            Dictionary<string, string> exact = new Dictionary<string, string> {
                { Keys.ModName, "APIのMOD名" }, { nameKey, "  " }, { descriptionKey, "" }, { labelKey, "ボタン" }
            };
            Dictionary<string, string> fallback = new Dictionary<string, string> {
                { nameKey, "English API zoom" }, { labelKey, "English API button" }, { titleKey, null }
            };
            Dictionary<string, IDictionary<string, string>> tables = new Dictionary<string, IDictionary<string, string>> {
                { "JA", exact }, { "en", fallback }, { "unsupported", new Dictionary<string, string> { { Keys.ModName, "Not English" } } }
            };
            int revision = catalog.Revision, text = catalog.TextRevision;
            using (IDisposable old = catalog.Translations.Register(owner, tables))
            {
                exact[Keys.ModName] = "caller mutation"; tables.Clear();
                Check(catalog.TextRevision == text + 1 && catalog.Revision == revision && !catalog.ConsumeDirty(),
                    "one translation registration requests only a presentation refresh");
                catalog.Translations.SetLanguage(SupportedLanguages.Japanese);
                Check(catalog.GetDisplayName(mod) == "APIのMOD名" && row.DisplayName == "ファイル倍率" && row.DisplayDescription == ""
                    && row.DisplayCategory == "English file group", "per-field precedence uses exact API, exact file, English file and accepts empty help");
                Check(row.GetActionName(action) == "ファイル操作" && row.GetActionCaption(action) == "ボタン" && row.DisplayName != row.GetActionName(action)
                    && row.Matches("ファイル操作", false) && !row.Matches("ボタン", false), "attached action title is searchable while its button label is independent");
                ChoiceItems choices = ChoiceItems.Read(row);
                Check(choices.Title == "ファイル倍率" && (string)choices.Values[1] == "far" && choices.Names[1] == "far",
                    "localized dropdown title never replaces candidate values or labels");
                catalog.Translations.SetLanguage(SupportedLanguages.French);
                Check(catalog.GetDisplayName(mod) == "English file mod" && row.DisplayName == "English API zoom"
                    && row.GetActionName(action) == "English file action" && row.DisplayDescription == "Raw help",
                    "missing language uses English API then English file then raw independently, treating null as missing");
                catalog.Translations.SetLanguage((SupportedLanguages)999);
                Check(row.DisplayName == "English API zoom" && ChoicesRetainIdentity(row, choices), "unknown game language maps to English with stable choices");

                int beforeFailure = catalog.TextRevision;
                bool rejected = false;
                try { catalog.Translations.Register(owner, new Dictionary<string, IDictionary<string, string>> { { "EN", fallback }, { "en", fallback } }); }
                catch (ArgumentException error) { rejected = error.ParamName == "translations"; }
                Check(rejected && catalog.TextRevision == beforeFailure && row.DisplayName == "English API zoom",
                    "ambiguous locale aliases fail before replacing the existing lease");
                using (catalog.Translations.Register(owner, new Dictionary<string, IDictionary<string, string>> {
                    { "en", new Dictionary<string, string> { { titleKey, "Replacement action title" } } }
                }))
                {
                    int replaced = catalog.TextRevision;
                    old.Dispose();
                    Check(catalog.TextRevision == replaced && row.GetActionName(action) == "Replacement action title"
                        && row.GetActionCaption(action) == "Replacement action title" && row.DisplayName == "English file zoom",
                        "replacement takes the whole table, old lease is inert and an unspecified button uses its action title");
                }
                Check(row.GetActionName(action) == "English file action" && row.DisplayName == "English file zoom",
                    "releasing the current table restores the cached file, not the previously registered table");
                Check(catalog.GetDisplayName(new ModConfig { Guid = "Fixture.translation", Name = "Exact GUID only" }) == "Exact GUID only"
                    && entry.Definition.Section == section && entry.Definition.Key == configKey && entry.Value == "near" && row.Metadata.Order == 7
                    && !File.Exists(file.ConfigFilePath) && catalog.Revision == revision && !catalog.ConsumeDirty(),
                    "translation lookup is GUID-ordinal and does not change config, order, disk or catalog revision");
            }
            VerifyTranslationLanguages(catalog, owner);
        }
    }

    private static bool ChoicesRetainIdentity(SettingRow row, ChoiceItems original)
    {
        ChoiceItems changed = ChoiceItems.Read(row);
        return changed.SameValuesAs(original) && changed.Title != original.Title;
    }

    private static void VerifyTranslationLanguages(ConfigCatalog catalog, string owner)
    {
        string[] codes = { "en", "fr", "it", "de", "es", "ru", "pt-BR", "pl", "zh-CN", "ja", "ko", "zh-TW" };
        SupportedLanguages[] actual = { SupportedLanguages.English, SupportedLanguages.French, SupportedLanguages.Italian,
            SupportedLanguages.German, SupportedLanguages.Spanish, SupportedLanguages.Russian, SupportedLanguages.Brazilian,
            SupportedLanguages.Polish, SupportedLanguages.Chinese, SupportedLanguages.Japanese, SupportedLanguages.Korean, SupportedLanguages.ChineseTraditional };
        Dictionary<string, IDictionary<string, string>> all = new Dictionary<string, IDictionary<string, string>>();
        foreach (string code in codes) all.Add(code, new Dictionary<string, string> { { Keys.ModName, code } });
        using (catalog.Translations.Register(owner, all))
        {
            ModConfig mod = new ModConfig { Guid = owner, Name = "Raw" };
            for (int index = 0; index < actual.Length; index++)
            {
                catalog.Translations.SetLanguage(actual[index]);
                Check(catalog.GetDisplayName(mod) == codes[index], "production resolver maps " + actual[index] + " to " + codes[index]);
            }
        }
    }
}
