using System.Collections;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyTextFocus(NativePage page)
    {
        T17StandaloneInputModule module = page.Domain.GetComponent<T17StandaloneInputModule>();
        FieldInfo inputOverride = AccessTools.Field(typeof(BaseInputModule), "m_InputOverride");
        FieldInfo windowFocused = AccessTools.Field(typeof(T17StandaloneInputModule), "m_bProcessMouseEvents");
        object originalInput = inputOverride.GetValue(module), originalFocus = windowFocused.GetValue(module);
        TextFocusMouse mouse = module.gameObject.AddComponent<TextFocusMouse>();
        MethodInfo textUpdate = AccessTools.Method(typeof(NativeTextInput), "Update");
        IMECompositionMode ime = Input.imeCompositionMode;
        ConfigEntryBase numberEntry = fixture["Types", "int"], colorEntry = fixture["Types", "color"];
        object originalNumber = numberEntry.BoxedValue, originalColor = colorEntry.BoxedValue;
        try
        {
            // Inject only device readings. T17 still raycasts, dispatches pointer events,
            // and updates focus in its real Process() on the installed Unity assemblies.
            inputOverride.SetValue(module, mouse); windowFocused.SetValue(module, true);
            harmony.Patch(textUpdate, null, null, new HarmonyMethod(typeof(ModSettingsUnityTests), "UseScriptedShortcutInput"));
            Transform body = page.transform.Find("SettingsBody");
            NativeTextInput search = body.Find("Search").GetComponent<NativeTextInput>();
            IEnumerator click = TextFocusClick(module, mouse, (RectTransform)search.transform);
            while (click.MoveNext()) yield return click.Current;
            Check(page.Domain.currentSelectedGameObject == search.gameObject && search.IsEditing,
                "native mouse click starts editing the search field");
            IEnumerator typing = TextFocusType("bool"); while (typing.MoveNext()) yield return typing.Current;
            Check(search.Value == "bool" && page.Right.Content.Find("Setting_Types_boolean") != null,
                "search filters while being typed through the Unity Update loop");
            search.EditKey(KeyCode.A, true, false);
            T17Button close = body.Find("CloseModSettings").GetComponent<T17Button>();
            RectTransform searchRect = (RectTransform)search.transform, closeRect = (RectTransform)close.transform;
            Vector2 hover = TextFocusCenter(closeRect);
            mouse.ProcessAt(module, hover);
            float delta = (float)AccessTools.Field(typeof(T17StandaloneInputModule), "m_CurrentMouseDelta").GetValue(module);
            File.WriteAllText(Path.Combine(output, "text-focus.txt"), "Screen=" + Screen.width + "x" + Screen.height
                + "\nSearch=" + searchRect.rect + " center=" + TextFocusCenter(searchRect)
                + "\nClose=" + closeRect.rect + " hover=" + hover + " delta=" + delta
                + "\nSelected=" + page.Domain.currentSelectedGameObject + " pending=" + page.Domain.GetPendingSelectedGameObject()
                + " editing=" + search.IsEditing + " selection=" + search.SelectionStart + ":" + search.SelectionEnd + "\n");
            Check(delta > 0 && SameColor(close.targetGraphic.color, page.Palette.Highlight)
                && SameColor(search.targetGraphic.color, page.Palette.Normal), "real mouse movement transfers hover highlighting to Close");
            Check(page.Domain.currentSelectedGameObject == search.gameObject && search.IsEditing && search.HasPending
                && search.SelectionStart == 0 && search.SelectionEnd == 4 && Input.imeCompositionMode == IMECompositionMode.On,
                "moving the pointer away does not deselect or reset search editing");
            typing = TextFocusType("boolean"); while (typing.MoveNext()) yield return typing.Current;
            Check(search.Value == "boolean" && search.Caret == 7 && search.IsEditing
                && page.Right.Content.Find("Setting_Types_boolean") != null && page.Right.Content.Find("Setting_Types_int") == null,
                "typing with the pointer still on Close replaces the selection and updates search");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "text-focus-away.png"));
            yield return new WaitForEndOfFrame(); yield return null;

            RectTransform clear = (RectTransform)body.Find("ClearSearch");
            click = TextFocusClick(module, mouse, clear); while (click.MoveNext()) yield return click.Current;
            Check(search.Value == "" && search.IsEditing && page.Domain.currentSelectedGameObject == search.gameObject,
                "native click on the clear button clears and refocuses search");
            RectTransform row = (RectTransform)page.Right.Content.Find("Setting_Types_int");
            page.Right.Reveal(row); Canvas.ForceUpdateCanvases();
            NativeTextInput number = row.Find("ExactValue").GetComponent<NativeTextInput>();
            click = TextFocusClick(module, mouse, (RectTransform)number.transform); while (click.MoveNext()) yield return click.Current;
            Check(!search.IsEditing && number.IsEditing && page.Domain.currentSelectedGameObject == number.gameObject,
                "clicking another input ends the old edit and focuses the new one");
            number.EditKey(KeyCode.A, true, false);
            typing = TextFocusType("-"); while (typing.MoveNext()) yield return typing.Current;
            mouse.ProcessAt(module, new Vector2(Screen.width * .5f, Screen.height - 1f));
            Check(number.IsEditing && number.Invalid && number.Caret == 1 && Equals(numberEntry.BoxedValue, originalNumber),
                "moving outside the panel keeps an invalid draft without writing it");
            typing = TextFocusType("27"); while (typing.MoveNext()) yield return typing.Current;
            ConfigFile disk = new ConfigFile(fixture.ConfigFilePath, false);
            Check(number.IsEditing && !number.Invalid && number.Value == "-27" && disk.Bind("Types", "int", 0).Value == -27,
                "correcting a number with the pointer outside the panel saves immediately");
            typing = TextFocusType("\n"); while (typing.MoveNext()) yield return typing.Current;
            Check(!number.IsEditing && !number.HasPending && Input.imeCompositionMode == ime, "Enter still ends editing and restores IME mode");
            mouse.ProcessAt(module, hover);
            Check(page.Domain.currentSelectedGameObject == null, "native pointer focus resumes once the input is idle");

            RectTransform color = (RectTransform)page.Right.Content.Find("Setting_Types_color");
            page.Right.Reveal(color); Canvas.ForceUpdateCanvases();
            click = TextFocusClick(module, mouse, (RectTransform)color.Find("PickColor")); while (click.MoveNext()) yield return click.Current;
            ColorPicker picker = page.Overlay.Root.GetComponentInChildren<ColorPicker>();
            Check(picker != null, "native click opens the color picker");
            click = TextFocusClick(module, mouse, (RectTransform)picker.Hex.transform); while (click.MoveNext()) yield return click.Current;
            Check(picker.Hex.IsEditing && page.Domain.currentSelectedGameObject == picker.Hex.gameObject, "color picker Hex owns keyboard focus");
            picker.Hex.EditKey(KeyCode.A, true, false);
            RectTransform pickerClose = (RectTransform)picker.transform.Find("ClosePicker");
            mouse.ProcessAt(module, TextFocusCenter(pickerClose));
            typing = TextFocusType("#33669980"); while (typing.MoveNext()) yield return typing.Current;
            Check(picker.Hex.IsEditing && !picker.Hex.Invalid && SameColor(ActualColor, new Color32(51, 102, 153, 128)),
                "typing Hex while hovering another modal control updates the actual color");
            page.Cancel(); yield return null; yield return null;
            Check(!page.Overlay.IsOpen && Input.imeCompositionMode == ime && page.gameObject.activeInHierarchy,
                "cancelling the modal releases the text edit and leaves the page open");

            click = TextFocusClick(module, mouse, searchRect); while (click.MoveNext()) yield return click.Current;
            Check(search.IsEditing, "search can take keyboard focus again after cancelling the modal");
            click = TextFocusClick(module, mouse, closeRect); while (click.MoveNext()) yield return click.Current;
            Check(Shortcut.ActivePage == null && !search.IsEditing && Input.imeCompositionMode == ime,
                "a real click on Close still closes the page and releases its text edit");
            Shortcut.Open(); yield return new WaitForSecondsRealtime(.35f);
        }
        finally
        {
            inputOverride.SetValue(module, originalInput); windowFocused.SetValue(module, originalFocus);
            Destroy(mouse);
            harmony.Unpatch(textUpdate, HarmonyPatchType.Transpiler, harmony.Id);
            scriptedShortcutFrame = -1; scriptedShortcutText = null;
            numberEntry.BoxedValue = originalNumber; colorEntry.BoxedValue = originalColor;
        }
    }

    private static Vector2 TextFocusCenter(RectTransform rect)
    { return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)); }

    private static IEnumerator TextFocusClick(T17StandaloneInputModule module, TextFocusMouse mouse, RectTransform target)
    {
        Vector2 point = TextFocusCenter(target);
        mouse.ProcessAt(module, point);
        mouse.ProcessAt(module, point, true, false); yield return null;
        mouse.ProcessAt(module, point, false, true); yield return null; yield return null;
    }

    private IEnumerator TextFocusType(string text)
    {
        scriptedShortcutText = text;
        IEnumerator press = PlayShortcutKey(KeyCode.None, false);
        while (press.MoveNext()) yield return press.Current;
        scriptedShortcutText = null;
    }
}

internal sealed class TextFocusMouse : BaseInput
{
    private Vector2 position;
    private bool down, up;
    public override Vector2 mousePosition { get { return position; } }
    public override Vector2 mouseScrollDelta { get { return Vector2.zero; } }
    public override bool GetMouseButtonDown(int button) { return button == 0 && down; }
    public override bool GetMouseButtonUp(int button) { return button == 0 && up; }
    internal void ProcessAt(T17StandaloneInputModule module, Vector2 point) { ProcessAt(module, point, false, false); }
    internal void ProcessAt(T17StandaloneInputModule module, Vector2 point, bool press, bool release)
    {
        EventSystem previous = EventSystem.current;
        bool allowMouse = module.allowMouseInput;
        position = point; down = press; up = release;
        try
        {
            EventSystem.current = module.GetComponent<T17EventSystem>();
            module.allowMouseInput = true; module.Process();
        }
        finally { module.allowMouseInput = allowMouse; EventSystem.current = previous; }
    }
}
