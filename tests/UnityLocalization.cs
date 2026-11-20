using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using OC2ModSettings.SDK;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private static SupportedLanguages testLanguage;
    private static bool OverrideLanguage(ref SupportedLanguages __result) { __result = testLanguage; return false; }
    private static bool SkipConsumerPolling() { return false; }
    private static string Caption(Transform target) { return target.GetComponentInChildren<T17Text>(true).text; }
    private void IsolateLanguagePoll(List<MethodInfo> isolated, string typeName, string methodName)
    {
        Type consumer = AccessTools.TypeByName(typeName);
        MethodInfo poll = consumer == null ? null : AccessTools.Method(consumer, methodName, Type.EmptyTypes);
        if (poll == null) return;
        harmony.Patch(poll, new HarmonyMethod(typeof(ModSettingsUnityTests), "SkipConsumerPolling"));
        isolated.Add(poll);
    }
    private IEnumerator VerifyLocalization(NativePage page, T17Button entryButton)
    {
        testLanguage = Localization.GetLanguage();
        List<MethodInfo> isolated = new List<MethodInfo>();
        MethodInfo readLanguage = AccessTools.Method(typeof(Localization), "GetLanguage");
        ConfigFile file = new ConfigFile(Path.Combine(output, "localization.cfg"), false); file.SaveOnConfigSet = false;
        ConfigEntry<string> language = file.Bind("Language", "Language", "en", new ConfigDescription("MOD language", new AcceptableValueList<string>("en", "zh")));
        ModSettingsAttributes roleDisplay = new ModSettingsAttributes { DispName = "Role", Category = "General", Order = 20, CategoryOrder = 100 };
        roleDisplay.ObjToStr = delegate(object value) { return language.Value == "zh"
            ? ((string)value == "host" ? "强制主机" : "游戏默认") : ((string)value == "host" ? "Force host" : "Game default"); };
        ConfigEntry<string> role = file.Bind("General", "Role", "default",
            new ConfigDescription("Role description", new AcceptableValueList<string>("default", "host"), roleDisplay));
        file.Bind("General", "Boolean", true, "boolean"); file.Bind("General", "Shortcut", new KeyboardShortcut(KeyCode.K), "shortcut");
        ConfigEntry<int> number = file.Bind("General", "Number", 10, "number");
        file.Bind("General", "Color", Color.cyan, "color");
        file.Save();
        ModSettingsClient client = new ModSettingsClient(); Check(client.Connect(), "localization fixture connects through source SDK");
        IDisposable registered = client.Register("fixture.localization", "Language refresh / 语言刷新", "Two independent language sources", file);
        IDisposable action = client.Action("fixture.localization", "status", new System.Collections.Generic.Dictionary<string, object> {
            { "Version", 1 }, { "DisplayName", "Check status" }, { "Category", "General" }, { "CategoryOrder", 100 }, { "Execute", new Action(delegate {}) } });
        EventHandler changed = delegate {
            roleDisplay.DispName = language.Value == "zh" ? "角色" : "Role";
            roleDisplay.Description = language.Value == "zh" ? "独立语言说明" : "Role description";
            roleDisplay.Category = language.Value == "zh" ? "常用功能" : "General";
            IDisposable next = client.Action("fixture.localization", "status", new System.Collections.Generic.Dictionary<string, object> {
                { "Version", 1 }, { "DisplayName", language.Value == "zh" ? "检查状态" : "Check status" },
                { "Category", roleDisplay.Category }, { "CategoryOrder", 100 }, { "Execute", new Action(delegate {}) } });
            action.Dispose(); action = next; client.Refresh(); };
        language.SettingChanged += changed;
        try
        {
            // Test in-place public labels separately from installed consumers' intentional
            // catalog rebuilds. The fixture's explicit SDK Refresh still runs below.
            IsolateLanguagePoll(isolated, "OC2HudCustomization.SettingsLanguageSync", "Poll");
            IsolateLanguagePoll(isolated, "HostUtilities.Language", "Update");
            harmony.Patch(readLanguage, new HarmonyMethod(typeof(ModSettingsUnityTests), "OverrideLanguage"));
            yield return new WaitForSecondsRealtime(.4f);
            Submit(page.Left.Content.Find("Mod_fixture.localization"), page); yield return new WaitForSecondsRealtime(.4f);
            NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
            NativeTextInput numeric = page.Right.Content.Find("Setting_General_Number/ExactValue").GetComponent<NativeTextInput>();
            page.Domain.SetSelectedGameObject(numeric.gameObject); yield return null; yield return null;
            numeric.Begin(); numeric.ChangeText("-"); int caret = numeric.Caret;
            byte[] before = File.ReadAllBytes(file.ConfigFilePath);
            Transform footer = page.transform.Find("SettingsBody/ToggleShortcut");
            string currentKey = ValueCodec.Format(Shortcut.Setting, Shortcut.Key);
            foreach (SupportedLanguages current in Enum.GetValues(typeof(SupportedLanguages)))
            {
                testLanguage = current; yield return new WaitForSecondsRealtime(.6f); yield return null;
                Check(Caption(entryButton.transform) == Texts.Title, "native entry follows " + current);
                Check(Caption(page.transform.Find("SettingsBody/HeaderBacker/Header")) == Texts.Title && page.MenuName == Texts.Title, "cached banner follows " + current);
                Check(Caption(page.transform.Find("SettingsBody/CloseModSettings")) == Texts.Close && Caption(search.transform) == Texts.Search, "cached close/search follow " + current);
                Check(Caption(footer) == currentKey, "idle shortcut shows only the key in " + current);
                T17Button[] toolbar = page.Right.Content.GetComponentsInChildren<T17Button>();
                Check(Caption(toolbar[0].transform).StartsWith(Texts.Advanced) && Caption(toolbar[1].transform) == Texts.Defaults
                    && Caption(toolbar[2].transform) == Texts.Refresh, "toolbar follows " + current);
                Check(Caption(page.Right.Content.Find("Setting_General_Shortcut/Capture")) == Texts.Rebind
                    && Caption(page.Right.Content.Find("Setting_General_Color/PickColor")) == Texts.PickColor, "row actions follow " + current);
                Check(Caption(page.Right.Content.Find("Setting_General_Boolean/Toggle")) == Texts.Boolean(true), "toggle uses official translation for " + current);
                File.AppendAllText(Path.Combine(output, "draft-state.txt"), current + " alive=" + (numeric != null)
                    + (numeric == null ? "" : " editing=" + numeric.IsEditing + " invalid=" + numeric.Invalid + " caret=" + numeric.Caret + "/" + caret)
                    + " value=" + number.Value + " help=" + Caption(page.transform.Find("SettingsBody/Explanation"))
                    + " selected=" + page.Domain.currentSelectedGameObject + " revision=" + ModSettingsPlugin.Instance.Catalog.Revision + "\n");
                Check(numeric != null && numeric.IsEditing && numeric.Invalid && numeric.Caret == caret && number.Value == 10
                    && Caption(page.transform.Find("SettingsBody/Explanation")) == Texts.InvalidValue, "language refresh preserves invalid draft, caret and saved value for " + current);
                Check(Caption(page.Right.Content.Find("Setting_General_Role/Dropdown")) == "Game default ▾", "consumer language stays English under game language " + current);
                numeric.Cancel(); page.Right.Content.anchoredPosition = Vector2.zero;
                Canvas.ForceUpdateCanvases();
                foreach (T17Text label in page.GetComponentsInChildren<T17Text>())
                    File.AppendAllText(Path.Combine(output, "localized-labels.tsv"), current + "\t" + label.name + "\t" + label.text.Replace("\n", " / ")
                        + "\t" + label.rectTransform.rect + "\tfont=" + label.font.name + "\tbestFit=" + label.cachedTextGenerator.fontSizeUsedForBestFit + "\n");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "language-" + current + ".png")); yield return new WaitForEndOfFrame(); yield return null;
                page.Domain.SetSelectedGameObject(numeric.gameObject); yield return null; yield return null; numeric.Begin(); numeric.ChangeText("-");
            }
            numeric.Cancel();
            Check(Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(file.ConfigFilePath)), "all game language changes leave config file untouched");
            Submit(footer, page); yield return null; yield return null;
            Check(page.IsRecordingToggleKey && Caption(footer) == Texts.PressKey, "listening prompt uses the current game language");
            testLanguage = SupportedLanguages.French; yield return new WaitForSecondsRealtime(.4f); yield return null;
            Check(page.IsRecordingToggleKey && page.Domain.IsDisabled() && Caption(footer) == Texts.PressKey
                && Caption(page.transform.Find("SettingsBody/Explanation")).Contains(Texts.Cancel), "game language refresh translates the active shortcut prompt without cancelling it");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-recording-french.png")); yield return new WaitForEndOfFrame(); yield return null;
            page.Cancel(); yield return null; yield return null;
            testLanguage = SupportedLanguages.English; yield return new WaitForSecondsRealtime(.4f);
            Submit(page.Right.Content.Find("Setting_Language_Language/Dropdown"), page); yield return null; yield return null;
            Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1"), page);
            yield return new WaitForSecondsRealtime(.4f); yield return null;
            Check(language.Value == "zh" && Caption(page.Right.Content.Find("Setting_General_Role/Dropdown")) == "游戏默认 ▾",
                "selecting the MOD's real language dropdown refreshes other dropdown current values without Enter");
            Submit(page.Right.Content.Find("Setting_General_Role/Dropdown"), page); yield return null; yield return null;
            Check(Caption(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1")) == "强制主机", "reopened candidates use the language chosen through the settings UI");
            page.Overlay.Close(); language.Value = "en"; yield return new WaitForSecondsRealtime(.4f); yield return null;
            Submit(page.Right.Content.Find("Setting_General_Role/Dropdown"), page); yield return null; yield return null;
            Check(Caption(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1")) == "Force host", "dropdown initially reads consumer English formatter");
            page.Overlay.FocusChoice(1); yield return null; yield return null;
            Transform originalOverlay = page.Overlay.Root;
            language.Value = "zh"; yield return new WaitForSecondsRealtime(.4f); yield return null; yield return null;
            Check(page.Overlay.IsOpen && page.Overlay.Root != originalOverlay
                && Caption(page.Overlay.Root.Find("Dialog/Title")) == "角色", "SDK refresh rebuilds and restores the open dropdown with localized title");
            Check(Caption(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1")) == "强制主机"
                && page.Domain.currentSelectedGameObject.name == "Choice_1", "open dropdown follows consumer language and keeps focused value");
            Check(Caption(page.Right.Content.Find("Setting_General_Role/Dropdown")) == "游戏默认 ▾"
                && Caption(page.Right.Content.Find("Setting_@Action_status/Action_status")) == "检查状态", "current value and registered business action follow consumer language");
            Check(Caption(page.transform.Find("SettingsBody/CloseModSettings")) == "Close" && role.Value == "default", "consumer language does not overwrite English game chrome or saved value");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "consumer-dropdown-zh.png")); yield return new WaitForEndOfFrame(); yield return null;
            Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1"), page);
            ConfigFile saved = new ConfigFile(file.ConfigFilePath, true);
            Check(role.Value == "host" && saved.Bind("General", "Role", "default").Value == "host", "translated selection commits original stable ID to disk");
            yield return null; yield return null;
            Check(page.Domain.currentSelectedGameObject == page.Right.Content.Find("Setting_General_Role/Dropdown").gameObject,
                "restored dropdown closes back to its newly built invoker rather than a destroyed control");
            language.SettingChanged -= changed; language.Value = "en";
            yield return new WaitForSecondsRealtime(.4f);
            Submit(page.Right.Content.Find("Setting_General_Role/Dropdown"), page); yield return null; yield return null;
            Transform liveOverlay = page.Overlay.Root;
            Transform liveFirst = liveOverlay.Find("Dialog/Choices/Viewport/Content/Choice_0");
            yield return new WaitForSecondsRealtime(.5f);
            Check(page.Overlay.Root == liveOverlay && liveFirst == liveOverlay.Find("Dialog/Choices/Viewport/Content/Choice_0"),
                "unchanged dropdown polling preserves existing buttons");
            language.Value = "zh"; yield return new WaitForSecondsRealtime(.4f); yield return null;
            Check(page.Overlay.Root == liveOverlay && Caption(liveOverlay.Find("Dialog/Choices/Viewport/Content/Choice_1")) == "强制主机"
                && role.Value == "host", "same formatter updates the open list in place without explicit Refresh or changing saved value");
            language.SettingChanged += changed;
            string[] values = ((AcceptableValueList<string>)role.Description.AcceptableValues).AcceptableValues;
            values[0] = "host"; values[1] = "default";
            yield return new WaitForSecondsRealtime(.4f); yield return null; yield return null;
            Check(Caption(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_0")) == "强制主机", "open dropdown detects candidate reordering without an API rebuild");
            Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_0"), page);
            Check(role.Value == "host", "candidate reordering cannot redirect click to previous index value");
            Submit(page.Right.Content.Find("Setting_General_Color/PickColor"), page); yield return null; yield return null;
            ColorPicker picker = page.Overlay.Root.GetComponentInChildren<ColorPicker>(); Transform colorOverlay = page.Overlay.Root;
            picker.Hex.Begin(); picker.Hex.ChangeText("#invalid");
            testLanguage = SupportedLanguages.French; yield return new WaitForSecondsRealtime(.4f); yield return null;
            Check(page.Overlay.Root == colorOverlay && picker.Hex.IsEditing && picker.Hex.Invalid
                && picker.Hex.ValidationLabel.text == Texts.InvalidValue, "color overlay updates validation without losing its active draft");
            Check(Caption(picker.transform.Find("ClosePicker")) == Texts.Close && Caption(picker.transform.Find("AlphaLabel")) == Texts.Opacity
                && Caption(picker.transform.Find("ControlsHint")) == Texts.ColorHint, "open color overlay follows game language in place");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "language-French-color.png")); yield return new WaitForEndOfFrame(); yield return null;
            page.Overlay.Close();
            page.Overlay.Capture("Shortcut", true, delegate(object value) {}); yield return null;
            testLanguage = SupportedLanguages.Polish; yield return new WaitForSecondsRealtime(.4f);
            Check(page.Domain.IsDisabled() && Caption(page.Overlay.Root.Find("Dialog/Clear")) == Texts.Clear
                && Caption(page.Overlay.Root.Find("Dialog/Title")).EndsWith(Texts.Capture), "capture changes language while retaining input suppression");
            page.Overlay.Close(); Check(!page.Domain.IsDisabled(), "localized capture releases input suppression on close");
            page.Overlay.ChooseDefaults(delegate(int ignored) {}); testLanguage = SupportedLanguages.German;
            yield return new WaitForSecondsRealtime(.4f);
            Check(Caption(page.Overlay.Root.Find("Dialog/Title")) == Texts.Defaults
                && Caption(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_0")) == Texts.Confirm, "open defaults dialog refreshes title and confirmation");
            page.Overlay.Close();
            search.Begin(); search.TypeText("__nothing__");
            testLanguage = SupportedLanguages.Korean; yield return new WaitForSecondsRealtime(.4f);
            Check(Caption(page.Right.Content.Find("Empty")) == Texts.Empty && search.IsEditing && search.Value == "__nothing__",
                "empty results refresh language without losing live search");
            search.EditKey(KeyCode.A, true, false); search.EditKey(KeyCode.Backspace, false, false); search.Cancel();
            page.Hide(true, false); testLanguage = SupportedLanguages.Japanese; yield return new WaitForSecondsRealtime(.4f);
            ModSettingsPlugin.Instance.OpenSettings(); yield return new WaitForSecondsRealtime(.4f);
            Check(Caption(page.transform.Find("SettingsBody/CloseModSettings")) == Texts.Close && Caption(search.transform) == Texts.Search,
                "cached hidden page refreshes language before reopening");
            File.WriteAllText(Path.Combine(output, "localization-geometry.txt"), "Screen=" + Screen.width + "x" + Screen.height
                + "\nSearch=" + search.GetComponent<RectTransform>().rect + "\nClose=" + page.transform.Find("SettingsBody/CloseModSettings").GetComponent<RectTransform>().rect
                + "\nLanguages=" + Enum.GetValues(typeof(SupportedLanguages)).Length + "\nLanguageSource=Harmony test override of Localization.GetLanguage; no player language written\n");
        }
        finally
        {
            page.Overlay.Close(); language.SettingChanged -= changed; action.Dispose(); registered.Dispose();
            harmony.Unpatch(readLanguage, HarmonyPatchType.Prefix, harmony.Id);
            foreach (MethodInfo poll in isolated) harmony.Unpatch(poll, HarmonyPatchType.Prefix, harmony.Id);
        }
    }
}
