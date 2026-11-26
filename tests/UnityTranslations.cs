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

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyTranslations(NativePage page)
    {
        const string owner = "fixture.modsettings.translation", section = "Fixture", category = "Base category";
        const string apiButton = "立即\n运行", updatedButton = "再次\n运行", fileTitle = "文件动作标题";
        ModSettingsClient client = new ModSettingsClient();
        Check(client.Connect() && client.SupportsTranslations && client.SupportsActions,
            "the elected MOD Settings service exposes its optional translation API");
        ConfigFile file = new ConfigFile(Path.Combine(output, "translation-catalog-fixture.cfg"), false) { SaveOnConfigSet = false };
        ModSettingsAttributes display = new ModSettingsAttributes { Category = category };
        ConfigEntry<int> number = file.Bind(section, "Number", 10, new ConfigDescription("Raw number help", null, display));
        ConfigEntry<string> choice = file.Bind(section, "Choice", "low",
            new ConfigDescription("Raw choice help", new AcceptableValueList<string>("low", "high"), display));
        file.Bind(section, "Raw", true, new ConfigDescription("Raw option help", null, display));
        file.Save();
        byte[] originalConfig = File.ReadAllBytes(file.ConfigFilePath);
        int callbacks = 0;
        List<MethodInfo> isolated = new List<MethodInfo>();
        MethodInfo readLanguage = AccessTools.Method(typeof(Localization), "GetLanguage");
        SupportedLanguages originalLanguage = Localization.GetLanguage();
        IDisposable apiLease = null, replacement = null;
        using (client.Register(owner, "Raw Translation Fixture", "", file))
        using (client.RegisterAction(owner, "run", new ModSettingsAction("Raw Action Title", delegate { callbacks++; }) {
            Category = category, ButtonLabel = "Raw Run", Description = "Raw action help" }))
        using (client.RegisterAction(owner, "inspect", new ModSettingsAction("Raw Detail Title", delegate { callbacks++; }) {
            Entry = number, ButtonLabel = "Raw Inspect" }))
        {
            try
            {
                IsolateLegacyLanguageRefresh(isolated);
                testLanguage = SupportedLanguages.English;
                harmony.Patch(readLanguage, new HarmonyMethod(typeof(ModSettingsUnityTests), "OverrideLanguage"));
                Dictionary<string, string> english = new Dictionary<string, string> {
                    { ModSettingsTranslations.ModName, "API Translation Fixture" },
                    { ModSettingsTranslations.ActionLabel("run"), "API Run" }
                };
                Dictionary<string, string> chinese = new Dictionary<string, string> {
                    { ModSettingsTranslations.ModName, "注册翻译模组" },
                    { ModSettingsTranslations.ConfigName(section, "Number"), "注册数字" },
                    { ModSettingsTranslations.ConfigName(section, "Choice"), "选择强度" },
                    { ModSettingsTranslations.ConfigDescription(section, "Number"), "" },
                    { ModSettingsTranslations.ActionLabel("run"), apiButton },
                    { ModSettingsTranslations.ActionLabel("inspect"), "注册详情" },
                    { ModSettingsTranslations.ActionDescription("inspect"), "查看当前数字的详情" }
                };
                Dictionary<string, IDictionary<string, string>> tables = new Dictionary<string, IDictionary<string, string>> {
                    { "en", english }, { "zh-CN", chinese }
                };
                apiLease = client.RegisterTranslations(owner, tables);
                chinese[ModSettingsTranslations.ActionLabel("run")] = updatedButton;
                yield return new WaitForSecondsRealtime(.4f);
                Transform tab = page.Left.Content.Find("Mod_" + owner);
                Check(tab != null && Caption(tab) == "API Translation Fixture", "English registration overrides filename-backed MOD name");
                Submit(tab, page); yield return null; yield return null; yield return null;
                T17Text groupLabel = page.Right.Content.Find("Category/Label").GetComponent<T17Text>();
                Transform group = groupLabel.transform;
                RectTransform row = page.Right.Content.Find("Setting_Fixture_Number") as RectTransform;
                Transform rawRow = page.Right.Content.Find("Setting_Fixture_Raw");
                RectTransform actionRow = page.Right.Content.Find("Setting_@Action_run") as RectTransform;
                T17Button run = actionRow.Find("Action_run").GetComponent<T17Button>();
                T17Button inspect = row.Find("Action_inspect").GetComponent<T17Button>();
                NativeTextInput input = row.Find("ExactValue").GetComponent<NativeTextInput>();
                T17Text numberLabel = row.Find("Name/Text").GetComponent<T17Text>();
                T17Text actionTitle = actionRow.Find("Name/Text").GetComponent<T17Text>();
                T17Text actionLabel = run.GetComponentInChildren<T17Text>();
                Transform explanation = page.transform.Find("SettingsBody/Explanation");
                Check(Caption(group) == "File Group" && numberLabel.text == "File Number"
                    && Caption(actionRow.Find("Name")) == "File Action Title" && actionLabel.text == "API Run"
                    && Caption(inspect.transform) == "File Inspect" && Caption(rawRow.Find("Name")) == "Raw",
                    "base category, configuration label, separate action captions and raw fallback resolve independently");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "mod-translation-en.png"));
                yield return new WaitForEndOfFrame(); yield return null;
                VerifySettingsTypography(page, groupLabel, numberLabel, actionTitle);

                page.Right.Reveal(row); page.Domain.SetSelectedGameObject(input.gameObject); yield return null; yield return null;
                page.KeyboardFeedback();
                Check(Caption(explanation) == "English file guidance", "the actual focused config row uses a file-backed description");
                page.Left.Reveal(tab as RectTransform); yield return null; yield return null;
                Submit(input, page); input.ChangeText("-");
                int caret = input.Caret;
                Canvas.ForceUpdateCanvases();
                RectTransform body = page.transform.Find("SettingsBody") as RectTransform;
                Rect tabBefore = BoundsIn(tab as RectTransform, body);
                testLanguage = SupportedLanguages.Chinese; yield return new WaitForSecondsRealtime(.4f); yield return null;
                Transform currentTab = page.Left.Content.Find("Mod_" + owner);
                File.WriteAllLines(Path.Combine(output, "translation-phase.txt"), new string[] {
                    "lang=" + Localization.GetLanguage(), "sameTab=" + (currentTab == tab), "tab=" + Caption(currentTab),
                    "sameRow=" + (page.Right.Content.Find("Setting_Fixture_Number") == row),
                    "selected=" + page.Domain.currentSelectedGameObject, "revision=" + page.Catalog.Revision
                });
                Check(page.Left.Content.Find("Mod_" + owner) == tab && Caption(tab) == "● 注册翻译模组"
                    && Caption(group) == "文件分区" && numberLabel.text == "注册数字"
                    && Caption(actionRow.Find("Name")) == fileTitle && actionLabel.text == apiButton
                    && Caption(inspect.transform) == "注册详情",
                    "exact API overrides Chinese file, exact file overrides English and caller dictionary mutation is ignored");
                Check(page.Right.Content.Find("Setting_Fixture_Number") == row && page.Right.Content.Find("Setting_@Action_run") == actionRow
                    && ReferenceEquals(page.Domain.currentSelectedGameObject, input.gameObject)
                    && input.IsEditing && input.Invalid && Caption(input.transform) == "-" && input.Caret == caret
                    && number.Value == 10 && Caption(explanation) == Texts.InvalidValue,
                    "game language repaints existing controls without changing the focused invalid draft, validation or config value");

                replacement = client.RegisterTranslations(owner, tables);
                apiLease.Dispose(); apiLease = null;
                yield return new WaitForSecondsRealtime(.4f); yield return null;
                Check(actionLabel.text == updatedButton && input.IsEditing && input.Caret == caret && numberLabel.text == "注册数字",
                    "same-GUID replacement repaints the current action caption, old lease disposal preserves the new values and the draft survives");
                Check(Convert.ToBase64String(originalConfig) == Convert.ToBase64String(File.ReadAllBytes(file.ConfigFilePath)),
                    "translation registration and language switching do not write the real fixture config");
                input.Cancel(); page.KeyboardFeedback();
                Check(Caption(explanation) == "", "an explicit empty translated description does not fall through to a file or raw text");

                page.Domain.SetSelectedGameObject(inspect.gameObject); yield return null; yield return null;
                page.KeyboardFeedback();
                Check(Caption(explanation) == "查看当前数字的详情", "attached action help is independent of its configuration row");

                page.Right.Reveal(actionRow); yield return null; yield return null;
                page.Domain.SetSelectedGameObject(run.gameObject); yield return null; yield return null;
                page.KeyboardFeedback();
                Check(Caption(explanation) == "文件动作说明", "standalone action focus reads its exact-locale file description");
                Canvas.ForceUpdateCanvases();
                Rect tabAfter = BoundsIn(tab as RectTransform, body);
                Rect actionBounds = BoundsIn((RectTransform)run.transform, body), labelBounds = BoundsIn(actionLabel.rectTransform, body);
                TextGenerator glyphs = actionLabel.cachedTextGenerator;
                bool rendered = glyphs.Populate(actionLabel.text, actionLabel.GetGenerationSettings(actionLabel.rectTransform.rect.size));
                Check(RectNear(tabBefore, tabAfter) && labelBounds.width > 0 && labelBounds.height > 0
                    && rendered && glyphs.lineCount == 2 && glyphs.characterCountVisible >= 4 && labelBounds.xMin >= actionBounds.xMin - .1f
                    && labelBounds.xMax <= actionBounds.xMax + .1f && labelBounds.yMin >= actionBounds.yMin - .1f
                    && labelBounds.yMax <= actionBounds.yMax + .1f,
                    "translated two-line action fits its native control and the MOD tab keeps its original bounds");
                File.WriteAllLines(Path.Combine(output, "mod-translation-geometry.txt"), new string[] {
                    "Screen=" + Screen.width + "x" + Screen.height, "MOD tab before=" + tabBefore, "MOD tab after=" + tabAfter,
                    "Action=" + actionBounds, "Label=" + labelBounds, "Lines=" + glyphs.lineCount,
                    "FontSize=" + glyphs.fontSizeUsedForBestFit, "Caption=" + actionLabel.text.Replace('\n', '/'),
                    "GUID=" + owner + " section=" + number.Definition.Section + " key=" + number.Definition.Key
                });
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "mod-translation-zh-CN.png"));
                yield return new WaitForEndOfFrame(); yield return null;
                VerifySettingsTypography(page, groupLabel, numberLabel, actionTitle);

                testLanguage = SupportedLanguages.French; yield return new WaitForSecondsRealtime(.4f); yield return null;
                Check(Caption(tab) == "● API Translation Fixture" && Caption(group) == "File Group" && numberLabel.text == "File Number"
                    && actionLabel.text == "API Run" && Caption(inspect.transform) == "File Inspect"
                    && Caption(rawRow.Find("Name")) == "Raw" && Caption(explanation) == "English file action guidance",
                    "missing French translations use English API, then English file, then raw fields independently");
                testLanguage = SupportedLanguages.Chinese; replacement.Dispose(); replacement = null;
                yield return new WaitForSecondsRealtime(.4f); yield return null;
                Check(Caption(tab) == "● 文件翻译模组" && numberLabel.text == "文件数字" && actionLabel.text == "文件运行"
                    && Caption(inspect.transform) == "文件详情" && page.Right.Content.Find("Setting_Fixture_Number") == row,
                    "disposing the current lease restores direct file-backed Chinese text in place");
                Submit(run, page); Submit(inspect, page);
                Check(callbacks == 2 && number.Value == 10, "translated standalone and attached captions keep their original callbacks and config identity");

                replacement = client.RegisterTranslations(owner, tables);
                yield return new WaitForSecondsRealtime(.4f);
                Transform dropdown = page.Right.Content.Find("Setting_Fixture_Choice/Dropdown");
                Submit(dropdown, page); yield return null; yield return null;
                Transform overlay = page.Overlay.Root;
                Transform choices = overlay.Find("Dialog/Choices/Viewport/Content");
                Transform high = choices.Find("Choice_1");
                page.Domain.SetSelectedGameObject(high.gameObject); yield return null; yield return null;
                Check(Caption(overlay.Find("Dialog/Title")) == "选择强度" && Caption(high) == "high", "built-in dropdown translates its title, not its values");
                testLanguage = SupportedLanguages.English; yield return new WaitForSecondsRealtime(.4f); yield return null;
                Check(page.Overlay.Root == overlay && choices.Find("Choice_1") == high && Caption(overlay.Find("Dialog/Title")) == "Choice"
                    && page.Domain.currentSelectedGameObject == high.gameObject && choice.Value == "low",
                    "language updates the open dropdown title without rebuilding choices or changing focus and value");
                page.Overlay.Close(); yield return null; yield return null;

                testLanguage = SupportedLanguages.Chinese; yield return new WaitForSecondsRealtime(.4f);
                NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
                page.Domain.SetSelectedGameObject(search.gameObject); yield return null; yield return null;
                Submit(search, page); search.ChangeText("注册数字"); yield return null; yield return null;
                Check(page.Right.Content.Find("Setting_Fixture_Number") != null && page.Right.Content.Find("Setting_Fixture_Choice") == null,
                    "search filters by translated config display text");
                testLanguage = SupportedLanguages.English; yield return new WaitForSecondsRealtime(.4f); yield return null;
                Check(page.Right.Content.Find("Empty") != null && search.IsEditing && Caption(search.transform) == "注册数字"
                    && page.Domain.currentSelectedGameObject == search.gameObject, "changed search matches refresh without losing the query or caret focus");
            }
            finally
            {
                if (replacement != null) replacement.Dispose();
                if (apiLease != null) apiLease.Dispose();
                harmony.Unpatch(readLanguage, HarmonyPatchType.Prefix, harmony.Id);
                foreach (MethodInfo poll in isolated) harmony.Unpatch(poll, HarmonyPatchType.Prefix, harmony.Id);
                testLanguage = originalLanguage;
            }
        }
        yield return new WaitForSecondsRealtime(.4f);
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
    }
}
