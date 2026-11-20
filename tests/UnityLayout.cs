using System;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private static Rect BoundsIn(RectTransform item, Transform space)
    {
        Vector3[] corners = new Vector3[4]; item.GetWorldCorners(corners);
        Vector3 min = space.InverseTransformPoint(corners[0]), max = min;
        foreach (Vector3 point in corners)
        { Vector3 local = space.InverseTransformPoint(point); min = Vector3.Min(min, local); max = Vector3.Max(max, local); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }
    private void VerifyLayout(NativePage page)
    {
        Transform body = page.transform.Find("SettingsBody");
        RectTransform backer = body.Find("HeaderBacker") as RectTransform;
        VerifyBanner(page);
        Rect banner = BoundsIn(backer, body);
        Rect search = BoundsIn(body.Find("Search") as RectTransform, body);
        Check(body.Find("CurrentMod") == null && page.Right.Content.Find("Search") == null,
            "duplicate MOD title removed and search moved outside scrolling settings");
        Rect right = BoundsIn(page.Right.Viewport, body);
        Rect left = BoundsIn(page.Left.Viewport, body);
        Rect close = BoundsIn(body.Find("CloseModSettings") as RectTransform, body);
        Rect clear = BoundsIn(body.Find("ClearSearch") as RectTransform, body);
        Rect help = BoundsIn(body.Find("Explanation") as RectTransform, body);
        Check(search.xMin - banner.xMax >= 8, "top-right search sits beside blue banner with eight UI units clearance");
        float gap = search.yMin - right.yMax;
        Check(gap >= 8 && gap <= 16 && search.height >= 42 && search.width >= 400,
            "settings viewport is raised to the header controls while leaving room to type");
        Check(Mathf.Abs(left.yMax - right.yMax) < 1 && Mathf.Abs(left.yMin - right.yMin) < 1, "independent column viewports share top and bottom edges");
        Rect frame = ((RectTransform)body).rect;
        Check(clear.xMin - search.xMax >= 8 && close.xMin - clear.xMax >= 8 && clear.width >= 40 && close.width >= 140
            && Mathf.Abs(close.height - search.height) < .1f && Mathf.Abs(clear.height - search.height) < .1f
            && Mathf.Abs(close.center.y - search.center.y) < .1f && Mathf.Abs(clear.center.y - search.center.y) < .1f,
            "search, clear and close are equally tall and have separate hit areas");
        Check(help.xMin >= frame.xMin + frame.width * .20f && help.xMax <= frame.xMax
            && help.yMax < right.yMin - 8, "Description footer clears the native lower notch and viewport");
        File.WriteAllText(Path.Combine(output, "layout-checks.txt"), "Banner=" + banner + "\nFrame=" + frame + "\nSearch=" + search
            + "\nBannerGap=" + (search.xMin - banner.xMax) + "\nSearchGap=" + gap
            + "\nLeft=" + left + "\nRight=" + right + "\nClear=" + clear + "\nClose=" + close + "\nHelp=" + help);
        Transform category = page.Right.Content.Find("Category");
        Check(category != null && category.Find("Label").GetComponent<T17Text>().alignment == TextAnchor.MiddleCenter
            && category.Find("Divider") != null, "category header is centered above indented rows with a separator");
        string[] inline = { "boolean", "byte", "string", "range", "choice", "shortcut", "enum" };
        foreach (string key in inline)
        {
            RectTransform row = page.Right.Content.Find("Setting_Types_" + key) as RectTransform;
            if (row == null) continue;
            Rect label = BoundsIn(row.Find("Name") as RectTransform, row);
            string path = key == "boolean" ? "Toggle" : key == "choice" ? "Dropdown" : key == "shortcut" ? "Capture" : key == "enum" ? "KeyList" : "ExactValue";
            Rect editor = BoundsIn(row.Find(key == "shortcut" ? "ShortcutValue" : path) as RectTransform, row);
            Check(row.rect.height <= 72 && editor.xMin - label.xMax >= 8
                && Mathf.Abs(editor.center.y - label.center.y) <= 3, key + ": compact name and editor share one row without overlap");
            Check(row.Find("Confirm") == null && row.Find("ComponentConfirm") == null && row.Find("CustomConfirm") == null,
                key + ": no confirmation button or hidden placeholder exists");
        }
        Transform toggle = page.Right.Content.Find("Setting_Types_boolean/Toggle");
        if (toggle != null)
        {
            string translated;
            bool value = (bool)fixture["Types", "boolean"].BoxedValue;
            Check(Localization.Get(value ? "OptionValue.On" : "OptionValue.Off", out translated)
                && toggle.GetComponentInChildren<T17Text>().text == translated, "boolean uses game's current-language ON/OFF translation");
        }
    }
    private void VerifyBanner(NativePage page)
    {
        RectTransform banner = (RectTransform)page.transform.Find("SettingsBody/HeaderBacker");
        T17Text title = banner.Find("Header").GetComponent<T17Text>();
        T17Text version = banner.Find("PluginVersion").GetComponent<T17Text>();
        Rect t = BoundsIn(title.rectTransform, banner), v = BoundsIn(version.rectTransform, banner);
        float versionInset = banner.rect.xMax - v.xMax;
        SupportedLanguages language = Localization.GetLanguage();
        Check(title.text == Texts.PanelTitle && version.text == "v" + expectedVersion
            && (language != SupportedLanguages.Chinese || title.text == "MOD 设置面板"), "banner shows the requested title and loaded plugin version in " + language);
        Check(Mathf.Abs(t.center.x - banner.rect.center.x) < 1 && t.xMax < v.xMin && versionInset >= 40
            && v.yMin > banner.rect.yMin && v.center.y < t.center.y && !version.raycastTarget,
            "the centered title and inset version have separate space inside the banner");
        File.AppendAllText(Path.Combine(output, "banner-layout.txt"), language + " version right inset=" + versionInset + Environment.NewLine);
        foreach (T17Text label in new T17Text[] { title, version })
        {
            TextGenerator glyphs = label.cachedTextGenerator;
            bool rendered = glyphs.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
            float left = float.MaxValue, right = float.MinValue;
            for (int i = 0; i + 7 < glyphs.verts.Count; i += 4)
            {
                float x = glyphs.verts[i].position.x, end = glyphs.verts[i + 1].position.x;
                if (end <= x) continue;
                left = Mathf.Min(left, x / label.pixelsPerUnit); right = Mathf.Max(right, end / label.pixelsPerUnit);
            }
            File.AppendAllText(Path.Combine(output, "banner-layout.txt"), language + " " + label.name + " " + label.text
                + " rect=" + BoundsIn(label.rectTransform, banner) + " font=" + label.font.name + " lines=" + glyphs.lineCount
                + " glyphs=" + glyphs.characterCountVisible + " x=" + left + ":" + right + Environment.NewLine);
            Check(label.isActiveAndEnabled && rendered && glyphs.lineCount == 1 && glyphs.characterCountVisible >= label.text.Length
                && right > left && left >= label.rectTransform.rect.xMin - 4 && right <= label.rectTransform.rect.xMax + 4,
                label.name + " renders fully on one line in " + language);
        }
    }
}
