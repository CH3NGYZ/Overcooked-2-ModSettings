using System.Collections;
using System.Collections.Generic;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private static bool SameColor(Color a, Color b)
    { return Mathf.Abs(a.r - b.r) < .005f && Mathf.Abs(a.g - b.g) < .005f && Mathf.Abs(a.b - b.b) < .005f && Mathf.Abs(a.a - b.a) < .005f; }
    private PointerEventData HoverSettings(NativePage page, RectTransform rect, Vector2 fraction)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        GraphicRaycaster raycaster = rect.GetComponentInParent<GraphicRaycaster>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster.eventCamera;
        PointerEventData pointer = new PointerEventData(page.Domain) { position = RectTransformUtility.WorldToScreenPoint(camera,
            rect.TransformPoint(new Vector3(rect.rect.xMin + rect.rect.width * fraction.x, rect.rect.yMin + rect.rect.height * fraction.y, 0))) };
        List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(pointer, hits);
        if (hits.Count > 0) pointer.pointerCurrentRaycast = hits[0];
        Check(pointer.pointerCurrentRaycast.gameObject != null, "feedback pointer reaches rendered UI at " + rect.name);
        for (Transform target = pointer.pointerCurrentRaycast.gameObject.transform; target != null; target = target.parent)
            ExecuteEvents.Execute(target.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        return pointer;
    }
    private IEnumerator VerifyFeedback(NativePage page)
    {
        Color normal = themeFixture ? FixtureNormal : new Color32(90, 150, 190, 255);
        Color highlight = themeFixture ? FixtureHighlight : new Color32(255, 157, 62, 255);
        Color rowNormal = page.Palette.Row;
        File.WriteAllText(Path.Combine(output, "palette.txt"), "Normal=#" + ColorUtility.ToHtmlStringRGB(page.Palette.Normal)
            + "\nHighlight=#" + ColorUtility.ToHtmlStringRGB(page.Palette.Highlight) + "\nFixture=" + themeInjected);
        Check(!themeFixture || themeInjected, "recolored textures were installed in the live test process before template capture");
        Check(SameColor(page.Palette.Normal, normal) && SameColor(page.Palette.Highlight, highlight),
            "cached colors match the independent expectations of the native button's two sprite states");
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        Check(SameColor(search.targetGraphic.color, normal) && SameColor(page.Right.Bar.GetComponent<Image>().color, normal),
            "search and scrollbar track use the native normal color");
        T17Text help = page.transform.Find("SettingsBody/Explanation/Help").GetComponent<T17Text>();
        foreach (Transform child in page.Right.Content)
            if (child.name == "Category")
                Check(SameColor(child.GetComponent<Image>().color, page.Palette.Category)
                    && SameColor(child.Find("Label").GetComponent<T17Text>().color, Color.white),
                    "category heading uses the same game white as setting titles");
        foreach (SettingsRowFeedback candidate in page.Right.Content.GetComponentsInChildren<SettingsRowFeedback>())
            Check(candidate.transform.Find("Name").GetComponentInChildren<T17Text>().color == Color.white,
                "setting title uses game white: " + candidate.name);
        RectTransform boolean = page.Right.Content.Find("Setting_Types_boolean") as RectTransform;
        T17Button toggle = boolean.Find("Toggle").GetComponent<T17Button>();
        T17Button defaults = boolean.Find("Default").GetComponent<T17Button>();
        page.Right.Reveal(boolean); page.Domain.SetSelectedGameObject(toggle.gameObject); yield return null; yield return null;
        HoverSettings(page, boolean, new Vector2(.1f, .5f));
        Check(SameColor(boolean.GetComponent<Image>().color, highlight) && SameColor(toggle.targetGraphic.color, normal)
            && SameColor(defaults.targetGraphic.color, normal) && help.text == fixture["Types", "boolean"].Description.Description,
            "row whitespace highlights only the row and displays its exact Description");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "feedback-row.png")); yield return new WaitForEndOfFrame(); yield return null;
        HoverSettings(page, toggle.GetComponent<RectTransform>(), new Vector2(.75f, .5f));
        Check(SameColor(toggle.targetGraphic.color, highlight) && SameColor(boolean.GetComponent<Image>().color, rowNormal)
            && SameColor(defaults.targetGraphic.color, normal) && help.text == fixture["Types", "boolean"].Description.Description,
            "hovering the toggle removes row highlight and highlights only that child");
        Check((toggle.transform.Find("StateIcon") as RectTransform).sizeDelta == new Vector2(64, 64), "theme retains the 64 by 64 toggle icon");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "feedback-control.png")); yield return new WaitForEndOfFrame(); yield return null;
        RectTransform textRow = page.Right.Content.Find("Setting_Types_string") as RectTransform;
        page.Right.Reveal(textRow); yield return null; yield return null;
        NativeTextInput text = textRow.Find("ExactValue").GetComponent<NativeTextInput>();
        HoverSettings(page, text.GetComponent<RectTransform>(), new Vector2(.5f, .5f));
        Check(SameColor(text.targetGraphic.color, highlight) && SameColor(toggle.targetGraphic.color, normal)
            && help.text == fixture["Types", "string"].Description.Description, "hover changes Description and clears the previous child highlight");
        page.KeyboardFeedback(); page.Domain.SetSelectedGameObject(text.gameObject); yield return null; yield return null;
        Check(SameColor(text.targetGraphic.color, highlight) && help.text == fixture["Types", "string"].Description.Description,
            "native selection highlights input and shows Description without a mouse");
        HoverSettings(page, search.GetComponent<RectTransform>(), new Vector2(.5f, .5f));
        Check(SameColor(search.targetGraphic.color, highlight) && SameColor(text.targetGraphic.color, normal) && help.text == "",
            "search uses the shared theme and clears unrelated Description without the removed hint");
        RectTransform range = page.Right.Content.Find("Setting_Types_range") as RectTransform;
        page.Right.Reveal(range); yield return null; yield return null;
        PreviewSlider slider = range.Find("Range").GetComponent<PreviewSlider>();
        HoverSettings(page, slider.GetComponent<RectTransform>(), new Vector2(.3f, .5f));
        Check(SameColor(slider.GetComponent<Image>().color, highlight) && SameColor(range.GetComponent<Image>().color, rowNormal)
            && help.text == fixture["Types", "range"].Description.Description, "slider highlights its rail without highlighting the enclosing row");
        Graphic handle = slider.handleRect.GetComponent<Graphic>();
        Check(SameColor(handle.color, Color.white) && SameColor(handle.canvasRenderer.GetColor(), Color.white),
            "focused slider retains a white handle and renderer tint over the themed rail");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "feedback-slider.png")); yield return new WaitForEndOfFrame(); yield return null;
        RectTransform choice = page.Right.Content.Find("Setting_Types_choice") as RectTransform;
        page.Right.Reveal(choice); yield return null; yield return null;
        T17Button dropdown = choice.Find("Dropdown").GetComponent<T17Button>();
        HoverSettings(page, dropdown.GetComponent<RectTransform>(), new Vector2(.5f, .5f));
        Check(SameColor(dropdown.targetGraphic.color, highlight) && SameColor(slider.GetComponent<Image>().color, normal),
            "dropdown applies exact shared hover colors and releases previous slider highlight");
        Submit(dropdown, page); yield return null; yield return null;
        T17Button option = page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_1").GetComponent<T17Button>();
        HoverSettings(page, option.GetComponent<RectTransform>(), new Vector2(.5f, .5f));
        Check(SameColor(option.targetGraphic.color, highlight) && SameColor(dropdown.targetGraphic.color, normal),
            "dropdown overlay option uses the shared focus color without leaving its owner highlighted");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "feedback-dropdown.png")); yield return new WaitForEndOfFrame(); yield return null;
        page.Overlay.Close(); yield return null; yield return null;
        page.KeyboardFeedback();
        File.WriteAllText(Path.Combine(output, "feedback-geometry.txt"), "Normal=" + normal + "\nHighlight=" + highlight
            + "\nRow=" + boolean.rect + "\nToggle=" + toggle.GetComponent<RectTransform>().rect + "\nIcon="
            + (toggle.transform.Find("StateIcon") as RectTransform).rect + "\nTitle=" + boolean.Find("Name").GetComponentInChildren<T17Text>().color
            + "\nFooter=" + help.text + "\nScreen=" + Screen.width + "x" + Screen.height);
        page.Right.Content.anchoredPosition = Vector2.zero;
    }
}
