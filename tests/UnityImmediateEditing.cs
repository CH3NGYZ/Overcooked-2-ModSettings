using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyImmediateEditing(NativePage page)
    {
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        page.Domain.SetSelectedGameObject(search.gameObject); yield return null; yield return null;
        search.Begin(); search.TypeText("__no_results__");
        Check(page.Right.Content.Find("Empty") != null && search.IsEditing, "search filters immediately without Enter and keeps editing active");
        yield return null; yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == search.gameObject && search.IsEditing,
            "live search rebuild preserves typing focus across layout restoration");
        search.EditKey(KeyCode.A, true, false); search.TypeText("boolean");
        Check(page.Right.Content.Find("Setting_Types_boolean") != null && page.Right.Content.Find("Setting_Types_int") == null,
            "replacing search text updates results in the same edit");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "live-search.png")); yield return new WaitForEndOfFrame(); yield return null;
        T17Button clear = page.transform.Find("SettingsBody/ClearSearch").GetComponent<T17Button>();
        ClickButton(page, clear);
        Check(search.Value == "" && page.Right.Content.Find("Setting_Types_int") != null,
            "clicking the clear button removes the filter and restores the current MOD's results without Enter");
        yield return null; yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == search.gameObject && search.IsEditing && search.Caret == 0,
            "clicking clear returns focus and the caret to the empty search field");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "search-cleared.png")); yield return new WaitForEndOfFrame(); yield return null;
        search.TypeText("__no_results__");
        Check(page.Right.Content.Find("Empty") != null, "typing resumes immediately after clicking clear");
        search.Confirm(); clear.Select(); yield return null; yield return null;
        ExecuteEvents.Execute(clear.gameObject, new BaseEventData(page.Domain), ExecuteEvents.submitHandler);
        yield return null; yield return null; yield return null;
        Check(search.Value == "" && search.IsEditing && page.Domain.currentSelectedGameObject == search.gameObject
            && page.Right.Content.Find("Empty") == null, "native submit on clear removes a confirmed no-results query and restores typing focus");
        search.TypeText("boolean");
        search.EditKey(KeyCode.A, true, false); search.EditKey(KeyCode.Backspace, false, false);
        Check(page.Right.Content.Find("Setting_Types_int") != null && search.IsEditing, "clearing search restores results without confirming");
        search.Cancel();
        NativeTextInput number = page.Right.Content.Find("Setting_Types_int/ExactValue").GetComponent<NativeTextInput>();
        page.Domain.SetSelectedGameObject(number.gameObject); yield return null; yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == number.gameObject,
            "delayed search layout cannot steal focus after selecting an input result");
        number.Begin(); number.EditKey(KeyCode.A, true, false); number.TypeText("12");
        Check((int)fixture["Types", "int"].BoxedValue == 12 && number.IsEditing, "numeric input persists valid changes before Enter or blur");
        ConfigFile saved = new ConfigFile(fixture.ConfigFilePath, true);
        Check(saved.Bind("Types", "int", 0).Value == 12, "immediate edit is written to owning file with autosave disabled");
        number.ChangeText("-");
        Check(number.Invalid && (int)fixture["Types", "int"].BoxedValue == 12, "incomplete numeric input stays invalid without replacing saved value");
        T17Text help = page.transform.Find("SettingsBody/Explanation/Help").GetComponent<T17Text>();
        Check(help.text == "数值有误", "invalid input shows the requested validation message below the settings");
        page.KeyboardFeedback(); yield return null; yield return null;
        Check(help.text == "数值有误" && number.Invalid, "focus feedback cannot overwrite active validation error with Description");
        saved = new ConfigFile(fixture.ConfigFilePath, true);
        Check(saved.Bind("Types", "int", 0).Value == 12, "invalid draft never replaces last valid value on disk");
        Canvas.ForceUpdateCanvases();
        Rect inputBounds = BoundsIn(number.GetComponent<RectTransform>(), page.transform.Find("SettingsBody") as RectTransform);
        Rect helpBounds = BoundsIn(help.rectTransform, page.transform.Find("SettingsBody") as RectTransform);
        Check(helpBounds.yMax < inputBounds.yMin && help.gameObject.activeInHierarchy && help.text == "数值有误",
            "validation footer is visible below edited numeric input");
        File.WriteAllText(Path.Combine(output, "live-input.txt"), "input=" + inputBounds + "\nhelp=" + helpBounds + "\nerror=" + help.text + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "invalid-input.png")); yield return new WaitForEndOfFrame(); yield return null;
        number.ChangeText("-13"); number.Cancel();
        Check((int)fixture["Types", "int"].BoxedValue == -13, "cancel ends live editing without rolling back already saved changes");
        Check(help.color == Color.white && !help.text.Contains("立即保存") && !help.text.Contains("save immediately"),
            "valid input clears the validation error without restoring the removed fixed hint");
        RectTransform stringRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        float originalWidth = (stringRow.Find("ExactValue") as RectTransform).rect.width;
        IDisposable hidden = ModSettingsPlugin.Instance.ConfigureEntryV1(fixture["Types", "string"],
            new Dictionary<string, object> { { "HideDefaultButton", true }, { "DispName", "Very long configuration name / 这是一项用于检查长配置名不会被空控件挤压的配置" } });
        yield return new WaitForSecondsRealtime(.35f); yield return null;
        stringRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        Check(stringRow.Find("Default") == null && stringRow.Find("Confirm") == null, "absent row actions are not created as hidden placeholders");
        Rect nameBounds = BoundsIn(stringRow.Find("Name") as RectTransform, stringRow);
        Rect editorBounds = BoundsIn(stringRow.Find("ExactValue") as RectTransform, stringRow);
        Check(nameBounds.width > stringRow.rect.width * .45f && editorBounds.xMin > nameBounds.xMax
            && editorBounds.xMax > stringRow.rect.xMax - 15, "long name grows and editor reclaims missing default action space");
        hidden.Dispose(); yield return new WaitForSecondsRealtime(.35f); yield return null;
        stringRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        Check(Mathf.Abs((stringRow.Find("ExactValue") as RectTransform).rect.width - originalWidth) < 1,
            "removing metadata override restores measured normal row geometry");
        IEnumerator failures = VerifyInputSaveFailures(page, help); while (failures.MoveNext()) yield return failures.Current;
    }
    private IEnumerator VerifyInputSaveFailures(NativePage page, T17Text help)
    {
        string blocker = Path.Combine(output, "live-save-blocker"); File.WriteAllText(blocker, "file");
        foreach (bool autosave in new bool[] { false, true })
        {
            ConfigFile failed = new ConfigFile(Path.Combine(blocker, autosave ? "auto.cfg" : "manual.cfg"), false);
            failed.SaveOnConfigSet = false;
            ConfigEntry<int> value = failed.Bind("General", "Number", 2, "save failure"); failed.SaveOnConfigSet = autosave;
            IDisposable lease = ModSettingsPlugin.Instance.RegisterConfigV1("fixture.livefailure", "Live save failure", "", failed);
            yield return new WaitForSecondsRealtime(.35f);
            Submit(page.Left.Content.Find("Mod_fixture.livefailure"), page); yield return null; yield return null; yield return null;
            NativeTextInput input = page.Right.Content.Find("Setting_General_Number/ExactValue").GetComponent<NativeTextInput>();
            page.Domain.SetSelectedGameObject(input.gameObject); yield return null; yield return null;
            input.Begin(); input.EditKey(KeyCode.A, true, false); input.TypeText("3");
            Check(input.Invalid && value.Value == 3 && !File.Exists(failed.ConfigFilePath) && failed.SaveOnConfigSet == autosave,
                "failed immediate save keeps actual memory state and save policy, autosave=" + autosave);
            page.KeyboardFeedback();
            Check(!string.IsNullOrEmpty(input.ValidationError) && help.text == input.ValidationError && help.color.r > help.color.g,
                "save failure keeps the actual error below the input instead of claiming validation success");
            input.Cancel(); lease.Dispose(); yield return new WaitForSecondsRealtime(.35f);
        }
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
    }
}
