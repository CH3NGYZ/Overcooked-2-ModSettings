using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class ModSettingsUnityTests
{
    private static KeyCode scriptedShortcutKey;
    private static KeyCode scriptedShortcutModifier;
    private static string scriptedShortcutText;
    private static bool scriptedShortcutRelease;
    private static int scriptedShortcutFrame = -1;
    private static bool ScriptedShortcutDown(KeyCode key)
    { return Time.frameCount == scriptedShortcutFrame && !scriptedShortcutRelease && key == scriptedShortcutKey; }
    private static bool ScriptedShortcutUp(KeyCode key)
    { return Time.frameCount == scriptedShortcutFrame && scriptedShortcutRelease && key == scriptedShortcutKey; }
    private static bool ScriptedShortcutHeld(KeyCode key)
    { return Time.frameCount == scriptedShortcutFrame && key == scriptedShortcutModifier && key != KeyCode.None; }
    private static string ScriptedShortcutText()
    { return Time.frameCount == scriptedShortcutFrame ? scriptedShortcutText ?? "" : ""; }
    private static IEnumerable<CodeInstruction> UseScriptedShortcutInput(IEnumerable<CodeInstruction> instructions)
    {
        MethodInfo down = AccessTools.Method(typeof(Input), "GetKeyDown", new Type[] { typeof(KeyCode) });
        MethodInfo up = AccessTools.Method(typeof(Input), "GetKeyUp", new Type[] { typeof(KeyCode) });
        MethodInfo held = AccessTools.Method(typeof(Input), "GetKey", new Type[] { typeof(KeyCode) });
        MethodInfo text = AccessTools.PropertyGetter(typeof(Input), "inputString");
        foreach (CodeInstruction instruction in instructions)
        {
            if (Equals(instruction.operand, down)) instruction.operand = AccessTools.Method(typeof(ModSettingsUnityTests), "ScriptedShortcutDown");
            if (Equals(instruction.operand, up)) instruction.operand = AccessTools.Method(typeof(ModSettingsUnityTests), "ScriptedShortcutUp");
            if (Equals(instruction.operand, held)) instruction.operand = AccessTools.Method(typeof(ModSettingsUnityTests), "ScriptedShortcutHeld");
            if (Equals(instruction.operand, text)) instruction.operand = AccessTools.Method(typeof(ModSettingsUnityTests), "ScriptedShortcutText");
            yield return instruction;
        }
    }
    private IEnumerator PlayShortcutKey(KeyCode key, bool release)
    {
        scriptedShortcutKey = key; scriptedShortcutRelease = release; scriptedShortcutFrame = Time.frameCount + 1;
        yield return null;
        // Also exercise Tick after capture has possibly committed a new key, independent
        // of the relative execution order of the two Unity Update callbacks.
        if (Shortcut.Pressed) Shortcut.Tick(true);
        yield return null;
    }
    private IEnumerator VerifyShortcutCapture(NativePage page)
    {
        T17Button button = page.transform.Find("SettingsBody/ToggleShortcut").GetComponent<T17Button>();
        RectTransform rect = button.GetComponent<RectTransform>(), body = button.transform.parent as RectTransform;
        Rect bounds = BoundsIn(rect, body);
        float listBottom = BoundsIn(page.Left.Viewport, body).yMin;
        Check(rect.rect.width <= 180 && rect.rect.height == 40 && listBottom - bounds.yMax >= 6
            && (bounds.yMin - body.rect.yMin) / body.rect.height > .125f
            && bounds.xMax < body.rect.xMin + body.rect.width * .21f, "compact shortcut fits the left notch without touching the list or description");
        CheckRaycast(button, page.Domain, "shortcut button is a real topmost hit target");
        File.WriteAllText(Path.Combine(output, "shortcut-button-geometry.txt"), "Screen=" + Screen.width + "x" + Screen.height
            + "\nShortcut=" + rect.rect + "\nGap below list=" + (listBottom - bounds.yMax));
        KeyCode original = Shortcut.Key;
        Check(Caption(button.transform) == ValueCodec.Format(Shortcut.Setting, original), "idle footer shows only the current key");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-button.png")); yield return new WaitForEndOfFrame(); yield return null;

        T17Button top = page.Left.Content.GetChild(0).GetComponent<T17Button>();
        page.Domain.SetSelectedGameObject(top.gameObject); yield return null; yield return null;
        ExecuteEvents.Execute(top.gameObject, new AxisEventData(page.Domain) { moveDir = MoveDirection.Up }, ExecuteEvents.moveHandler);
        yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == button.gameObject && button.navigation.selectOnUp != null
            && button.navigation.selectOnRight != null, "native directional navigation reaches the compact shortcut");

        MethodInfo[] readers = { AccessTools.PropertyGetter(typeof(NativeShortcut), "Pressed"),
            AccessTools.PropertyGetter(typeof(NativeShortcut), "CommandModifierHeld"), AccessTools.Method(typeof(KeyCapture), "Update"),
            AccessTools.Method(typeof(NativePage), "Update"), AccessTools.Method(typeof(NativeTextInput), "Update") };
        // Replace hardware reads only; the actual Unity input owner, capture and HUD prefix still run.
        try
        {
            foreach (MethodInfo reader in readers)
                harmony.Patch(reader, null, null, new HarmonyMethod(typeof(ModSettingsUnityTests), "UseScriptedShortcutInput"));
            Submit(button, page); yield return null; yield return null;
            Check(page.IsRecordingToggleKey && page.Domain.IsDisabled() && !page.Overlay.IsOpen
                && Caption(button.transform) == Texts.PressKey, "native submit starts listening inline, without a dropdown");
            Check(Caption(body.Find("Explanation")).Contains(Texts.PressKey) && Caption(body.Find("Explanation")).Contains("Esc")
                && !Caption(body.Find("Explanation")).Contains("F1"), "recording hint no longer suggests a fixed list of keys");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-recording.png")); yield return new WaitForEndOfFrame(); yield return null;

            ConfigEntryBase setting = Shortcut.Setting.Entry;
            string path = setting.ConfigFile.ConfigFilePath;
            string before = File.Exists(path) ? File.ReadAllText(path) : null;
            foreach (KeyCode ignored in new KeyCode[] { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftAlt, KeyCode.RightAlt,
                KeyCode.AltGr, KeyCode.LeftShift, KeyCode.RightShift, KeyCode.LeftCommand, KeyCode.RightCommand,
                KeyCode.LeftWindows, KeyCode.RightWindows, KeyCode.CapsLock, KeyCode.Numlock, KeyCode.Tab, KeyCode.Space,
                KeyCode.Backspace, KeyCode.Return, KeyCode.KeypadEnter, KeyCode.LeftArrow, KeyCode.Escape, KeyCode.Mouse0, KeyCode.Joystick1Button0 })
            {
                IEnumerator input = PlayShortcutKey(ignored, false); while (input.MoveNext()) yield return input.Current;
                Check(page.IsRecordingToggleKey && page.Domain.IsDisabled() && Shortcut.Key == original,
                    "listening safely ignores " + ignored + " without changing the shortcut");
            }
            IEnumerator press = PlayShortcutKey(KeyCode.Escape, true); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == page && !page.IsRecordingToggleKey && !page.Domain.IsDisabled()
                && Caption(button.transform) == ValueCodec.Format(Shortcut.Setting, original) && button.GetComponent<KeyCapture>() == null,
                "Escape cancels listening without closing the page, and releases its input owner");
            Check((File.Exists(path) ? File.ReadAllText(path) : null) == before, "ignored and cancelled keys leave the config file untouched");

            ConfigEntry<KeyCode> configEntry = (ConfigEntry<KeyCode>)setting;
            Check(!ConfigCommit.Apply(Shortcut.Setting, (KeyCode)9999).Success && configEntry.Value == original,
                "undefined KeyCode cannot be saved by the shared configuration path");
            ConfigFile manuallyEdited = new ConfigFile(Path.Combine(output, "reserved-shortcut.cfg"), false);
            File.WriteAllText(manuallyEdited.ConfigFilePath, "[Navigation]\nToggleKey = Tab\n"); manuallyEdited.Reload();
            Check(manuallyEdited.Bind(setting.Definition, NativeShortcut.DefaultKey, setting.Description).Value == NativeShortcut.DefaultKey,
                "manually configured reserved key falls back to BackQuote");

            ClickShortcutButton(page, button); yield return null; yield return null;
            Check(page.IsRecordingToggleKey, "click on the real footer hit starts listening");
            press = PlayShortcutKey(original, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == page && Shortcut.Key == original && !page.IsRecordingToggleKey && !page.Domain.IsDisabled(),
                "pressing the current key while listening retains it without toggling the panel");

            KeyValuePair<KeyCode, string>[] samples = { new KeyValuePair<KeyCode, string>(KeyCode.A, "A"),
                new KeyValuePair<KeyCode, string>(KeyCode.Minus, "-"), new KeyValuePair<KeyCode, string>(KeyCode.KeypadPlus, "Num +"),
                new KeyValuePair<KeyCode, string>(KeyCode.F15, "F15") };
            foreach (KeyValuePair<KeyCode, string> sample in samples)
            {
                Submit(button, page); yield return null; yield return null;
                press = PlayShortcutKey(sample.Key, false); while (press.MoveNext()) yield return press.Current;
                Check(Shortcut.ActivePage == page && Shortcut.Key == sample.Key && Caption(button.transform) == sample.Value,
                    "records " + sample.Key + " and displays only its key label");
            }

            ClickShortcutButton(page, button); yield return null; yield return null;
            KeyCode replacement = original == KeyCode.Alpha7 ? KeyCode.Alpha8 : KeyCode.Alpha7;
            string digit = replacement == KeyCode.Alpha7 ? "7" : "8";
            scriptedShortcutModifier = KeyCode.LeftControl;
            press = PlayShortcutKey(replacement, false); while (press.MoveNext()) yield return press.Current;
            scriptedShortcutModifier = KeyCode.None;
            Check(page.IsRecordingToggleKey && Shortcut.Key == KeyCode.F15, "a Control chord does not get recorded as a bare key");
            press = PlayShortcutKey(replacement, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == page && !page.IsRecordingToggleKey && !page.Overlay.IsOpen && !page.Domain.IsDisabled()
                && page.Domain.currentSelectedGameObject == button.gameObject && Caption(button.transform) == digit,
                "captured key updates the button and releases input without toggling on the assignment frame");
            ConfigFile disk = new ConfigFile(path, false);
            Check(disk.Bind(setting.Definition, NativeShortcut.DefaultKey, setting.Description).Value == replacement && Shortcut.Key == replacement,
                "captured key saves immediately to the existing config entry");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-rebound.png")); yield return new WaitForEndOfFrame(); yield return null;

            scriptedShortcutModifier = KeyCode.LeftControl;
            press = PlayShortcutKey(replacement, false); while (press.MoveNext()) yield return press.Current;
            scriptedShortcutModifier = KeyCode.None;
            Check(Shortcut.ActivePage == page, "a Control chord does not toggle the panel");
            IEnumerator typing = VerifyShortcutTyping(page, replacement, digit); while (typing.MoveNext()) yield return typing.Current;

            press = PlayShortcutKey(original, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == page, "previous key no longer toggles the page");
            press = PlayShortcutKey(replacement, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == null, "next press of the new key closes the page");
            press = PlayShortcutKey(original, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == null, "previous key cannot reopen the hidden page");
            press = PlayShortcutKey(replacement, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == page, "new key opens the cached page");

            Transform rebind = page.Right.Content.Find("Setting_Types_enum/Rebind");
            object previousBinding = fixture["Types", "enum"].BoxedValue;
            Submit(rebind, page); yield return null; yield return null;
            KeyCapture capture = rebind.parent.GetComponent<KeyCapture>(); capture.AcceptKey(replacement, new KeyCode[0]);
            Check(capture.enabled && Equals(previousBinding, fixture["Types", "enum"].BoxedValue), "ordinary rebind still reserves the global key");
            press = PlayShortcutKey(original, false); while (press.MoveNext()) yield return press.Current;
            Check(Equals(original, fixture["Types", "enum"].BoxedValue) && !page.Domain.IsDisabled(), "the former global key can now be captured in another MOD");
            fixture["Types", "enum"].BoxedValue = previousBinding;

            Submit(button, page); yield return null; yield return null;
            ModSettingsPlugin.Instance.RefreshV1(); yield return new WaitForSecondsRealtime(.4f); yield return null;
            Check(!page.IsRecordingToggleKey && !page.Domain.IsDisabled() && button.GetComponent<KeyCapture>() == null
                && Shortcut.Key == replacement && Caption(button.transform) == digit, "catalog rebuild cancels listening without changing the bound key");
            Submit(button, page); yield return null; yield return null;
            Check(page.IsRecordingToggleKey && page.Domain.IsDisabled(), "listening can start again after the rebuild");
            page.gameObject.SetActive(false); yield return null; yield return null;
            Check(!page.IsRecordingToggleKey && !page.Domain.IsDisabled() && button.GetComponent<KeyCapture>() == null
                && Shortcut.Key == replacement && Caption(button.transform) == digit, "external disable releases listening and restores idle caption");
            Shortcut.Open(); yield return new WaitForSecondsRealtime(.35f);
            Submit(button, page); yield return null; yield return null;
            press = PlayShortcutKey(original, false); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.Key == original && Shortcut.ActivePage == page && !page.IsRecordingToggleKey,
                "same button can restore the previous binding by pressing it");

            Submit(button, page); yield return null;
            Check(page.IsRecordingToggleKey, "listening is active before the page is closed");
            page.Hide(true, false); yield return null; yield return null;
            Check(Shortcut.ActivePage == null && !page.Domain.IsDisabled() && button.GetComponent<KeyCapture>() == null
                && Shortcut.Key == original, "closing the page while listening releases its own suppression without saving a different key");
            Shortcut.Open(); yield return new WaitForSecondsRealtime(.35f);
        }
        finally
        {
            foreach (MethodInfo reader in readers) harmony.Unpatch(reader, HarmonyPatchType.Transpiler, harmony.Id);
            scriptedShortcutFrame = -1; scriptedShortcutModifier = KeyCode.None; scriptedShortcutText = null;
        }
    }
    private IEnumerator VerifyShortcutTyping(NativePage page, KeyCode key, string digit)
    {
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        search.Select(); yield return null; yield return null; Submit(search, page); yield return null;
        scriptedShortcutText = digit;
        IEnumerator press = PlayShortcutKey(key, false); while (press.MoveNext()) yield return press.Current;
        Check(Shortcut.ActivePage == page && search.IsEditing && search.Value == digit && page.Right.Content.Find("Empty") != null,
            "typing the bound digit in live search filters without closing settings");
        search.ChangeText(""); search.Cancel(); yield return null; yield return null;

        ConfigEntryBase entry = fixture["Types", "int"];
        object original = entry.BoxedValue;
        NativeTextInput number = page.Right.Content.Find("Setting_Types_int/ExactValue").GetComponent<NativeTextInput>();
        number.Select(); yield return null; yield return null; Submit(number, page); yield return null;
        number.EditKey(KeyCode.A, true, false);
        press = PlayShortcutKey(key, false); while (press.MoveNext()) yield return press.Current;
        scriptedShortcutText = null;
        ConfigFile disk = new ConfigFile(fixture.ConfigFilePath, false);
        Check(Shortcut.ActivePage == page && number.IsEditing && number.Value == digit && disk.Bind("Types", "int", 0).Value.ToString() == digit,
            "typing the bound digit in a numeric input saves immediately without closing settings");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-typing.png")); yield return new WaitForEndOfFrame(); yield return null;
        number.Cancel(); entry.BoxedValue = original;
    }
    private void ClickShortcutButton(NativePage page, T17Button button)
    {
        RectTransform rect = (RectTransform)button.transform;
        PointerEventData pointer = new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
            "footer click routes through the native canvas hit");
        pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
}
