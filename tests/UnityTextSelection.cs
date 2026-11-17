using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private PointerEventData TextPointer(NativePage page, NativeTextInput input, float localX)
    { return TextPointer(page, input, localX, 0); }
    private PointerEventData TextPointer(NativePage page, NativeTextInput input, float localX, float localY)
    {
        T17Text label = input.transform.Find("Text").GetComponent<T17Text>();
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, label.rectTransform.TransformPoint(new Vector2(localX, localY)));
        PointerEventData data = new PointerEventData(page.Domain) { position = point, pressPosition = point,
            button = PointerEventData.InputButton.Left };
        List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(data, hits);
        Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerDownHandler>(hits[0].gameObject) == input.gameObject,
            "text pointer reaches input through actual global raycaster");
        data.pointerPressRaycast = data.pointerCurrentRaycast = hits[0]; return data;
    }
    private IEnumerator VerifyTextSelection(NativePage page)
    {
        NativeTextInput input = page.Right.Content.Find("Setting_Types_string/ExactValue").GetComponent<NativeTextInput>();
        string original = (string)fixture["Types", "string"].BoxedValue, clipboard = GUIUtility.systemCopyBuffer;
        fixture["Types", "string"].BoxedValue = "Alpha beta 中文 gamma";
        page.Domain.SetSelectedGameObject(input.gameObject);
        yield return null; yield return null;
        input.SetValue((string)fixture["Types", "string"].BoxedValue); input.Begin();
        Canvas.ForceUpdateCanvases();
        T17Text label = input.transform.Find("Text").GetComponent<T17Text>();
        PointerEventData pointer = TextPointer(page, input, label.rectTransform.rect.xMin + 1);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Check((int)AccessTools.Field(typeof(NativeTextInput), "caret").GetValue(input) == 0,
            "mouse click moves insertion caret from end to beginning");
        File.WriteAllText(Path.Combine(output, "text-selection.txt"), "click-start caret=0\n");
        Check(ExecuteEvents.GetEventHandler<IDragHandler>(input.gameObject) == input.gameObject,
            "input owns drag events instead of ancestor ScrollRect");
        float scroll = page.Right.Content.anchoredPosition.y;
        pointer = GlyphPointer(page, input, 6); ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.initializePotentialDrag);
        Check(!pointer.useDragThreshold, "text selection begins without ScrollRect drag threshold");
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.beginDragHandler);
        pointer.position = GlyphPointer(page, input, 10).position;
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.endDragHandler);
        Check(input.SelectionStart == 6 && input.SelectionEnd == 10, "forward mouse drag selects only beta and release preserves range");
        Check(Mathf.Abs(page.Right.Content.anchoredPosition.y - scroll) < .1f, "text drag leaves parent list scroll unchanged");
        TextSelectionGraphic graphic = input.GetComponentInChildren<TextSelectionGraphic>();
        yield return new WaitForEndOfFrame(); yield return null;
        using (VertexHelper mesh = new VertexHelper())
        {
            AccessTools.Method(typeof(TextSelectionGraphic), "OnPopulateMesh", new Type[] { typeof(VertexHelper) }).Invoke(graphic, new object[] { mesh });
            Check(graphic.enabled && mesh.currentVertCount >= 8 && !graphic.canvasRenderer.cull,
                "actual selection and caret geometry are visible and clipped graphics");
        }
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "text-partial-selection.png")); yield return new WaitForEndOfFrame(); yield return null;
        input.EditKey(KeyCode.C, true, false);
        float copyDeadline = Time.realtimeSinceStartup + 1;
        while (GUIUtility.systemCopyBuffer != "beta" && Time.realtimeSinceStartup < copyDeadline) yield return null;
        Check(GUIUtility.systemCopyBuffer == "beta", "copy copies partial selection only");
        input.InsertText("B"); Check((string)fixture["Types", "string"].BoxedValue == "Alpha B 中文 gamma", "typing replaces selected span and persists immediately");
        pointer = GlyphPointer(page, input, 10); ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        pointer.position = GlyphPointer(page, input, 8).position; ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.endDragHandler);
        Check(input.SelectionStart == 8 && input.SelectionEnd == 10, "reverse drag selects exactly two Chinese characters");
        input.EditKey(KeyCode.X, true, false);
        copyDeadline = Time.realtimeSinceStartup + 1;
        while (GUIUtility.systemCopyBuffer != "中文" && Time.realtimeSinceStartup < copyDeadline) yield return null;
        Check(GUIUtility.systemCopyBuffer == "中文" && (string)fixture["Types", "string"].BoxedValue == "Alpha B  gamma", "cut copies and removes selected CJK text only");
        input.EditKey(KeyCode.V, true, false); Check((string)fixture["Types", "string"].BoxedValue == "Alpha B 中文 gamma", "paste inserts at actual pointer caret");
        input.EditKey(KeyCode.Home, false, false); input.EditKey(KeyCode.RightArrow, false, true); input.EditKey(KeyCode.RightArrow, false, true);
        Check(input.SelectionStart == 0 && input.SelectionEnd == 2, "shift arrows extend a partial keyboard selection");
        input.EditKey(KeyCode.Delete, false, false); Check(input.Value == "pha B 中文 gamma", "Delete removes only partial selection");
        input.EditKey(KeyCode.End, false, false); input.EditKey(KeyCode.Backspace, false, false);
        Check(input.Value == "pha B 中文 gamm", "backspace after moved caret removes one character");
        input.EditKey(KeyCode.A, true, false); input.InsertText("<b>literal</b> 中文");
        Check(label.text == "<b>literal</b> 中文" && !label.supportRichText, "input markup stays literal and caret adds no text glyph");
        input.EditKey(KeyCode.A, true, false); input.InsertText("emoji \uD83D\uDE00");
        input.EditKey(KeyCode.LeftArrow, false, false); Check(input.Caret == 6, "arrow never splits UTF16 surrogate pair");
        input.EditKey(KeyCode.Delete, false, false); Check(input.Value == "emoji ", "delete removes complete surrogate pair");
        Func<string, bool> changed = input.Changed; int changes = 0;
        input.Changed = delegate(string value) { changes++; return changed(value); };
        input.EditKey(KeyCode.A, true, false); input.TypeText("批量中文 \uD83D\uDE00"); input.Changed = changed;
        Check(changes == 1 && input.Value == "批量中文 \uD83D\uDE00", "one inputString/IME batch persists once without splitting surrogate pair");
        input.EditKey(KeyCode.A, true, false); input.InsertText(new string('W', 130));
        Canvas.ForceUpdateCanvases();
        TextGenerator lines = label.cachedTextGenerator; lines.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
        Check(lines.lineCount >= 2 && lines.characterCountVisible > lines.lines[1].startCharIdx + 3, "long text uses rendered wrapped lines for pointer measurement");
        int wrappedEnd = lines.lines[1].startCharIdx + 3;
        pointer = GlyphPointer(page, input, 2); ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        pointer.position = GlyphPointer(page, input, wrappedEnd).position; ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(input.gameObject, pointer, ExecuteEvents.endDragHandler);
        Check(input.SelectionStart == 2 && input.SelectionEnd == wrappedEnd, "drag across wrapped lines uses both glyph coordinates");
        using (VertexHelper mesh = new VertexHelper())
        {
            AccessTools.Method(typeof(TextSelectionGraphic), "OnPopulateMesh", new Type[] { typeof(VertexHelper) }).Invoke(graphic, new object[] { mesh });
            Check(mesh.currentVertCount >= 12, "selection geometry spans both wrapped lines plus caret");
        }
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "text-wrapped-selection.png")); yield return new WaitForEndOfFrame(); yield return null;
        Rect selectionBounds = BoundsIn(input.GetComponent<RectTransform>(), page.Right.Viewport);
        File.AppendAllText(Path.Combine(output, "text-selection.txt"), "input-viewport=" + selectionBounds + "; label=" + label.rectTransform.rect
            + "; selection=6:10; screen=" + Screen.width + "x" + Screen.height + "\n");
        input.Cancel(); fixture["Types", "string"].BoxedValue = original; input.SetValue(original); GUIUtility.systemCopyBuffer = clipboard;
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        page.Domain.SetSelectedGameObject(search.gameObject); yield return null; yield return null;
        search.Begin(); search.ChangeText("find beta word");
        pointer = GlyphPointer(page, search, 5); ExecuteEvents.Execute(search.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        pointer.position = GlyphPointer(page, search, 9).position; ExecuteEvents.Execute(search.gameObject, pointer, ExecuteEvents.dragHandler);
        ExecuteEvents.Execute(search.gameObject, pointer, ExecuteEvents.endDragHandler);
        Check(search.SelectionStart == 5 && search.SelectionEnd == 9, "fixed search field supports the same partial selection");
        search.EditKey(KeyCode.A, true, false); search.EditKey(KeyCode.Backspace, false, false); search.Cancel();
        NativeTextInput number = page.Right.Content.Find("Setting_Types_int/ExactValue").GetComponent<NativeTextInput>();
        page.Domain.SetSelectedGameObject(number.gameObject); yield return null; yield return null;
        number.Begin(); pointer = TextPointer(page, number, number.transform.Find("Text").GetComponent<RectTransform>().rect.xMin + 1);
        ExecuteEvents.Execute(number.gameObject, pointer, ExecuteEvents.pointerDownHandler);
        Check(number.Caret == 0, "numeric exact field places caret by mouse too"); number.Cancel();
        yield return null;
    }
    private PointerEventData GlyphPointer(NativePage page, NativeTextInput input, int index)
    {
        T17Text label = input.transform.Find("Text").GetComponent<T17Text>();
        TextGenerator generator = label.cachedTextGenerator;
        generator.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
        int line = 0;
        for (int i = 1; i < generator.lineCount; i++) if (index >= generator.lines[i].startCharIdx) line = i;
        return TextPointer(page, input, (generator.characters[index].cursorPos.x + generator.characters[index].charWidth * .1f) / label.pixelsPerUnit,
            (generator.lines[line].topY - generator.lines[line].height * .5f) / label.pixelsPerUnit);
    }
}
