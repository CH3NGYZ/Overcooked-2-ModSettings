using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private Color ActualColor { get { return (Color)fixture["Types", "color"].BoxedValue; } }
    private IEnumerator VerifyColorPicker(NativePage page)
    {
        RectTransform row = page.Right.Content.Find("Setting_Types_color") as RectTransform;
        Check(row.rect.height == 68 && row.Find("RSlider") == null && row.GetComponentsInChildren<NativeTextInput>().Length == 1,
            "color row is compact without legacy four-channel controls");
        Rect hexBounds = BoundsIn(row.Find("Hex") as RectTransform, row);
        Rect swatchBounds = BoundsIn(row.Find("ColorPreview") as RectTransform, row);
        Rect pickBounds = BoundsIn(row.Find("PickColor") as RectTransform, row);
        Check(hexBounds.width > 180 && hexBounds.xMax < swatchBounds.xMin && swatchBounds.xMax < pickBounds.xMin,
            "compact color row has nonoverlapping Hex, checker preview and picker action");
        page.Right.Reveal(row); yield return null; yield return null;
        CheckRaycast(row.Find("PickColor").GetComponent<T17Button>(), page.Domain, "color picker action receives real canvas raycast");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "compact-color-row.png")); yield return new WaitForEndOfFrame(); yield return null;
        Submit(row.Find("PickColor"), page); yield return null; yield return null;
        ColorPicker picker = page.Overlay.Root.GetComponentInChildren<ColorPicker>();
        Check(picker != null && !page.Domain.IsDisabled() && page.Domain.currentSelectedGameObject == picker.Plane.gameObject,
            "color overlay owns focus while its native controls remain enabled");
        CheckRaycast(picker.Plane, page.Domain, "saturation and brightness plane receives real canvas raycast");
        Color before = ActualColor;
        PlanePointer(picker.Plane, page, .75f, .8f, false);
        Check(picker.Plane.Pending && ActualColor.Equals(before), "color plane drag previews without saving before release");
        string draft = picker.Hex.Value;
        Check(draft == row.Find("Hex").GetComponent<NativeTextInput>().Value && draft != "#" + ColorUtility.ToHtmlStringRGBA(before),
            "plane preview synchronizes modal and row Hex fields");
        PlanePointer(picker.Plane, page, .75f, .8f, true);
        float h, s, v; Color.RGBToHSV(ActualColor, out h, out s, out v);
        Check(!picker.Plane.Pending && Mathf.Abs(s - .75f) < .01f && Mathf.Abs(v - .8f) < .01f,
            "actual pointer release commits saturation and brightness");
        before = ActualColor;
        SliderPointer(picker.Hue, page, .38f, false);
        Check(picker.Hue.Pending && ActualColor.Equals(before), "rainbow pointer drag remains an unsaved preview");
        SliderPointer(picker.Hue, page, .38f, true);
        Color.RGBToHSV(ActualColor, out h, out s, out v);
        Check(!picker.Hue.Pending && Mathf.Abs(h - picker.Hue.value) < .01f, "rainbow pointer release commits actual RGB color");
        picker.Alpha.value = .35f;
        Check(Mathf.Abs(ActualColor.a - before.a) < .001f && picker.Hex.Value.EndsWith("59"),
            "opacity preview changes Hex alpha while preserving saved opacity");
        picker.Alpha.OnPointerUp(new PointerEventData(page.Domain));
        Check(Mathf.Abs(ActualColor.a - .35f) < .001f && picker.Hex.Value == "#" + ColorUtility.ToHtmlStringRGBA(ActualColor),
            "opacity pointer release saves and synchronizes Hex");
        before = ActualColor; picker.Hue.value = .9f;
        Color external = before; external.b = .24f; fixture["Types", "color"].BoxedValue = external;
        string visible = picker.Hex.Value;
        yield return new WaitForSecondsRealtime(.35f);
        File.AppendAllText(Path.Combine(output, "color-preview-probe.txt"), "expected=" + visible
            + " modal=" + picker.Hex.Value + " row=" + row.Find("Hex").GetComponent<NativeTextInput>().Value
            + " huePending=" + picker.Hue.Pending + " planePending=" + picker.Plane.Pending
            + " selected=" + page.Domain.currentSelectedGameObject + Environment.NewLine);
        Check(picker.Hex.Value == visible && row.Find("Hex").GetComponent<NativeTextInput>().Value == visible,
            "external config notifications preserve held color preview in both surfaces");
        picker.Hue.Cancel();
        Check(ActualColor.Equals(external) && picker.Hex.Value == "#" + ColorUtility.ToHtmlStringRGBA(external),
            "cancel discards drag and reads the latest external color without reverse writing");
        picker.Hex.Begin(); picker.Hex.EditKey(KeyCode.A, true, false); picker.Hex.TypeText("#33669980");
        Check(Mathf.Abs(ActualColor.r - .2f) < .001f && Mathf.Abs(picker.Alpha.value - 128f / 255) < .001f,
            "modal valid Hex saves before Enter and updates opacity and plane");
        before = ActualColor; picker.Hex.ChangeText("#12GG");
        Check(picker.Hex.Invalid && ActualColor.Equals(before), "invalid modal Hex preserves the saved color");
        Check(picker.Hex.ValidationLabel.text == "数值有误" && picker.Hex.ValidationLabel.gameObject.activeInHierarchy,
            "invalid modal Hex displays its validation message inside the dialog");
        page.KeyboardFeedback(); yield return null; yield return null;
        Rect modalInput = BoundsIn(picker.Hex.GetComponent<RectTransform>(), picker.GetComponent<RectTransform>());
        Rect modalError = BoundsIn(picker.Hex.ValidationLabel.rectTransform.parent as RectTransform, picker.GetComponent<RectTransform>());
        File.WriteAllText(Path.Combine(output, "modal-validation.txt"), "input=" + modalInput + "\nerror=" + modalError + "\n");
        Check(modalError.yMax + 2 < modalInput.yMin && picker.Hex.ValidationLabel.text == "数值有误",
            "modal validation stays below Hex with visible clearance after feedback refresh");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "invalid-modal-input.png")); yield return new WaitForEndOfFrame(); yield return null;
        picker.Hex.Cancel();
        Check(picker.Hex.ValidationLabel.text == "", "ending modal Hex editing clears its validation message");
        Enter(picker.Hex, "FFFFFF");
        Check(ActualColor == Color.white && picker.Plane != null, "six-digit Hex supports white and defaults to opaque");
        Enter(picker.Hex, "#00000080");
        picker.Hue.value = .63f; picker.Hue.Commit();
        PlanePointer(picker.Plane, page, .8f, .85f, true);
        Color.RGBToHSV(ActualColor, out h, out s, out v);
        Check(Mathf.Abs(h - .63f) < .01f && Mathf.Abs(ActualColor.a - 128f / 255) < .001f,
            "black can be recolored through retained hue and plane without losing opacity");
        NativeTextInput alpha = picker.transform.Find("ExactAlpha").GetComponent<NativeTextInput>();
        alpha.Begin(); alpha.EditKey(KeyCode.A, true, false); alpha.TypeText("0.42");
        Check(Mathf.Abs(ActualColor.a - .42f) < .001f && alpha.IsEditing, "exact opacity input saves before Enter");
        before = ActualColor; alpha.ChangeText("1.2");
        Check(alpha.Invalid && ActualColor.Equals(before), "opacity rejects values outside zero to one");
        Check(alpha.ValidationLabel.text == "数值有误" && alpha.ValidationLabel.gameObject.activeInHierarchy,
            "invalid exact opacity displays its validation message inside the dialog"); alpha.Cancel();
        page.Domain.SetSelectedGameObject(picker.Plane.gameObject);
        Submit(picker.Plane.transform, page);
        ExecuteEvents.Execute(picker.Plane.gameObject, new AxisEventData(page.Domain) { moveDir = MoveDirection.Left }, ExecuteEvents.moveHandler);
        Check(picker.Plane.Pending && ActualColor.Equals(before), "native direction editing previews the color plane after confirm");
        Submit(picker.Plane.transform, page);
        Check(!picker.Plane.Pending && !ActualColor.Equals(before), "second native confirm saves directional plane adjustment");
        Check(picker.Alpha.navigation.selectOnDown == picker.Hex && picker.Hex.navigation.selectOnRight.transform.IsChildOf(picker.transform),
            "modal explicit navigation remains within the picker");
        yield return new WaitForSecondsRealtime(.35f);
        Check(SameColor(picker.Hex.targetGraphic.color, page.Palette.Normal)
            && SameColor(alpha.targetGraphic.color, page.Palette.Normal)
            && SameColor(picker.Hex.targetGraphic.canvasRenderer.GetColor(), Color.white)
            && SameColor(alpha.targetGraphic.canvasRenderer.GetColor(), Color.white),
            "modal inputs retain exact normal blue after the native tint tween duration");
        HoverSettings(page, picker.Hex.GetComponent<RectTransform>(), new Vector2(.5f, .5f));
        Check(SameColor(picker.Hex.targetGraphic.color, page.Palette.Highlight)
            && SameColor(alpha.targetGraphic.color, page.Palette.Normal)
            && SameColor(picker.Hex.targetGraphic.canvasRenderer.GetColor(), Color.white),
            "modal Hex highlights only its own input without a leftover native tint");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "color-picker-focus.png")); yield return new WaitForEndOfFrame(); yield return null;
        page.KeyboardFeedback(); page.Domain.SetSelectedGameObject(picker.Plane.gameObject); yield return null;
        File.WriteAllText(Path.Combine(output, "picker-theme.txt"), "Hex=" + picker.Hex.targetGraphic.color
            + " canvas=" + picker.Hex.targetGraphic.canvasRenderer.GetColor() + "\nAlpha=" + alpha.targetGraphic.color
            + " canvas=" + alpha.targetGraphic.canvasRenderer.GetColor());
        File.AppendAllText(Path.Combine(output, "color-picker-geometry.txt"), "Screen=" + Screen.width + "x" + Screen.height + " row=" + row.rect
            + " hex=" + hexBounds + " preview=" + swatchBounds + " pick=" + pickBounds + "\n");
        foreach (string name in new string[] { "SaturationValue", "Hue", "Alpha", "Hex", "ColorPreview", "ExactAlpha", "ClosePicker" })
            File.AppendAllText(Path.Combine(output, "color-picker-geometry.txt"), name + "=" + BoundsIn(picker.transform.Find(name) as RectTransform, picker.GetComponent<RectTransform>()) + "\n");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "color-picker.png")); yield return new WaitForEndOfFrame(); yield return null;
        before = ActualColor; picker.Alpha.value = .1f; page.Cancel(); yield return null;
        Check(!page.Overlay.IsOpen && ActualColor.Equals(before) && !page.Domain.IsDisabled(), "cancel closes picker and discards unsaved opacity");
        Check(row.Find("Hex").GetComponent<NativeTextInput>().Value == "#" + ColorUtility.ToHtmlStringRGBA(before),
            "cancelled picker removes the row preview");
        Submit(row.Find("PickColor"), page); yield return null;
        picker = page.Overlay.Root.GetComponentInChildren<ColorPicker>(); picker.Hue.value = .2f;
        ExecuteEvents.Execute(page.Overlay.Root.gameObject, OutsideOverlay(page), ExecuteEvents.pointerClickHandler); yield return null;
        Check(!page.Overlay.IsOpen && ActualColor.Equals(before), "outside click discards picker draft without changing config");
        Submit(row.Find("PickColor"), page); yield return null;
        picker = page.Overlay.Root.GetComponentInChildren<ColorPicker>(); picker.Hex.Begin(); picker.Hex.ChangeText("bad");
        IMECompositionMode modalIme = Input.imeCompositionMode;
        T17EventSystem originalDomain = page.Domain;
        page.gameObject.SetActive(false); yield return null;
        Check(!page.Overlay.IsOpen && !picker.Hex.IsEditing && ActualColor.Equals(before), "external disable releases picker, draft and native input");
        Check(modalIme == IMECompositionMode.On && !originalDomain.IsDisabled(), "disabled color modal releases input ownership");
        FrontendRootMenu root = T17FrontendFlow.Instance.m_Rootmenu;
        page.Open();
        yield return new WaitForSecondsRealtime(.35f);
        Check(page.gameObject.activeInHierarchy && !page.Overlay.IsOpen, "page reopens after external color-modal disable");
        page.Right.Content.anchoredPosition = Vector2.zero;
    }
    private PointerEventData ColorPointer(Selectable control, NativePage page, float x, float y)
    {
        RectTransform rect = control.GetComponent<RectTransform>(); Canvas canvas = control.GetComponentInParent<Canvas>();
        GraphicRaycaster raycaster = control.GetComponentInParent<GraphicRaycaster>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster.eventCamera;
        return new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * x, rect.rect.yMin + rect.rect.height * y, 0))),
            pointerPressRaycast = new RaycastResult { module = raycaster } };
    }
    private void PlanePointer(ColorPlane plane, NativePage page, float x, float y, bool release)
    {
        PointerEventData pointer = ColorPointer(plane, page, x, y);
        ExecuteEvents.Execute(plane.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(plane.gameObject, pointer, ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(plane.gameObject, pointer, ExecuteEvents.dragHandler);
        if (release) { ExecuteEvents.Execute(plane.gameObject, pointer, ExecuteEvents.endDragHandler); ExecuteEvents.Execute(plane.gameObject, pointer, ExecuteEvents.pointerUpHandler); }
    }
    private void SliderPointer(PreviewSlider slider, NativePage page, float x, bool release)
    {
        PointerEventData pointer = ColorPointer(slider, page, x, .5f);
        ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.initializePotentialDrag);
        ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.beginDragHandler);
        ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.dragHandler);
        if (release) { ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.endDragHandler); ExecuteEvents.Execute(slider.gameObject, pointer, ExecuteEvents.pointerUpHandler); }
    }
}
