extern alias ConsumerSdk;
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using ModSettingsAttributes = ConsumerSdk::OC2ModSettings.SDK.ModSettingsAttributes;

internal static partial class CoreTests
{
    private enum LocalChoice { Default, Host, Client }
    [Flags] private enum LocalFlags { None = 0, First = 1, Second = 2, Both = 3 }
    private static void VerifyLocalization(string root)
    {
        SupportedLanguages[] languages = (SupportedLanguages[])Enum.GetValues(typeof(SupportedLanguages));
        Check(languages.Length == 12 && (int)SupportedLanguages.ChineseTraditional == 11, "translation slots match the shipped twelve-language enum");
        Dictionary<UiText, string[]> translations = (Dictionary<UiText, string[]>)typeof(Texts)
            .GetField("translations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
        foreach (UiText key in Enum.GetValues(typeof(UiText)))
        {
            Check(translations[key].Length == 12, "all twelve translations are supplied for " + key);
            foreach (SupportedLanguages language in languages)
                Check(!string.IsNullOrEmpty(Texts.Resolve(key, language)), key + " is available in " + language);
            Check(Texts.Resolve(key, (SupportedLanguages)100) == Texts.Resolve(key, SupportedLanguages.English)
                && Texts.Resolve(key, (SupportedLanguages)(-1)) == Texts.Resolve(key, SupportedLanguages.English), "unknown languages fall back to English for " + key);
        }
        Check(Texts.Resolve(UiText.Close, SupportedLanguages.German) == "Schließen"
            && Texts.Resolve(UiText.InvalidValue, SupportedLanguages.Korean) == "잘못된 값입니다"
            && Texts.Resolve(UiText.Search, SupportedLanguages.ChineseTraditional) == "搜尋目前 MOD…", "language mapping keeps accented, Korean and Traditional Chinese text distinct");
        ConfigFile file = new ConfigFile(Path.Combine(root, "choices.cfg"), false); file.SaveOnConfigSet = false;
        bool chinese = false;
        ModSettingsAttributes attributes = new ModSettingsAttributes { DispName = "Role", ObjToStr = delegate(object value) {
            return chinese ? (value.Equals("host") ? "强制主机" : "游戏默认") : (value.Equals("host") ? "Force host" : "Game default"); } };
        ConfigEntry<string> entry = file.Bind("Original", "Mode", "default",
            new ConfigDescription("stable", new AcceptableValueList<string>("default", "host"), attributes));
        SettingRow row = new SettingRow { Entry = entry, Metadata = EntryMetadata.Read(entry, null) };
        ChoiceItems english = ChoiceItems.Read(row);
        chinese = true;
        ChoiceItems translated = ChoiceItems.Read(row);
        Check(english.SameValuesAs(translated) && translated.Names[1] == "强制主机" && english.Names[1] == "Force host",
            "same formatter delegate re-evaluates live consumer language without changing the earlier snapshot");
        Check(translated.Values[1].Equals("host") && translated.Values[0].Equals("default")
            && entry.Value == "default" && !File.Exists(file.ConfigFilePath), "localized choices preserve original typed values and do not save on refresh");
        string[] allowed = ((AcceptableValueList<string>)entry.Description.AcceptableValues).AcceptableValues;
        allowed[0] = "host"; allowed[1] = "default";
        ChoiceItems reordered = ChoiceItems.Read(row);
        Check(!translated.SameValuesAs(reordered) && reordered.Values[0].Equals("host")
            && translated.Values[0].Equals("default"), "candidate reordering is detected without mutating a captured selection snapshot");
        attributes.DispName = "模式"; row.Metadata = EntryMetadata.Read(entry, null);
        Check(ChoiceItems.Read(row).Title == "模式" && entry.Definition.Section == "Original" && entry.Definition.Key == "Mode",
            "choice title follows refreshed metadata while historical config identity stays stable");
        Check(ConfigCommit.Apply(row, reordered.Values[0]).Success && entry.Value == "host"
            && !file.SaveOnConfigSet && File.Exists(file.ConfigFilePath), "translated choice commits its original string through the real ConfigFile save path");
        ConfigEntry<LocalChoice> enumEntry = file.Bind("Original", "Enum", LocalChoice.Host, "enum");
        SettingRow enumRow = new SettingRow { Entry = enumEntry, Metadata = EntryMetadata.Read(enumEntry, null) };
        enumRow.Metadata.Format = delegate(object value) { return value.Equals(LocalChoice.Host) ? "强制主机" : "其他"; };
        Check(ChoiceItems.Read(enumRow).Values[1].Equals(LocalChoice.Host) && ChoiceItems.Read(enumRow).Names[1] == "强制主机",
            "enum choices also use the consumer formatter while retaining enum values");
        ConfigEntry<LocalFlags> flags = file.Bind("Original", "Flags", LocalFlags.First, "flags");
        SettingRow flagsRow = new SettingRow { Entry = flags, Metadata = EntryMetadata.Read(flags, null) };
        flagsRow.Metadata.Format = delegate(object value) { return "位 " + value; };
        ChoiceItems flagsItems = ChoiceItems.Read(flagsRow);
        Check(flagsItems.Names[0].StartsWith("位 ") && flagsItems.Names[1].StartsWith("✓ 位 ")
            && !flagsItems.Names[2].StartsWith("✓"), "flags localize display names without losing checked bit states");
        flags.Value = LocalFlags.Both;
        Check(flagsItems.SameValuesAs(ChoiceItems.Read(flagsRow)) && ChoiceItems.Read(flagsRow).Names[2].StartsWith("✓ "),
            "external flag changes invalidate only the display snapshot");
        ConfigEntry<KeyCode> keyEntry = file.Bind("Original", "Key", KeyCode.Space, "key");
        object[] keys = ChoiceItems.ValuesFor(keyEntry);
        HashSet<object> seen = new HashSet<object>(keys);
        Check(keys.Length == seen.Count && seen.Contains(KeyCode.None) && seen.Contains(KeyCode.Space),
            "shared choice path retains KeyCode alias deduplication and None");
    }
}
