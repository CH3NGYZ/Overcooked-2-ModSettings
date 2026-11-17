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
        RectTransform title = backer.Find("Header") as RectTransform;
        Rect titleBounds = BoundsIn(title, backer);
        Check(Mathf.Abs(titleBounds.center.x - backer.rect.center.x) < 1, "main title centered within native blue banner");
        Rect banner = BoundsIn(backer, body);
        Rect search = BoundsIn(body.Find("Search") as RectTransform, body);
        Check(body.Find("CurrentMod") == null && page.Right.Content.Find("Search") == null,
            "duplicate MOD title removed and search moved outside scrolling settings");
        Rect right = BoundsIn(page.Right.Viewport, body);
        Rect left = BoundsIn(page.Left.Viewport, body);
        Rect close = BoundsIn(body.Find("CloseModSettings") as RectTransform, body);
        Rect help = BoundsIn(body.Find("Explanation") as RectTransform, body);
        Check(search.xMin - banner.xMax >= 8, "top-right search sits beside blue banner with eight UI units clearance");
        Check(search.yMin - right.yMax >= 8 && search.height >= 42, "top-right search clears settings viewport and retains usable height");
        Check(banner.yMin - left.yMax >= 8 && banner.yMin - left.yMax <= 24, "settings viewport uses space directly below native banner");
        Check(Mathf.Abs(left.yMax - right.yMax) < 1 && Mathf.Abs(left.yMin - right.yMin) < 1, "independent column viewports share top and bottom edges");
        Rect frame = ((RectTransform)body).rect;
        Check(close.xMin - search.xMax >= 12 && Mathf.Abs(close.height - search.height) < .1f
            && Mathf.Abs(close.center.y - search.center.y) < .1f && close.width >= 140,
            "close sits right of shortened search at equal height with usable width");
        Check(help.xMin >= frame.xMin + frame.width * .20f && help.xMax <= frame.xMax
            && help.yMax < right.yMin - 8, "Description footer clears the native lower notch and viewport");
        File.WriteAllText(Path.Combine(output, "layout-checks.txt"), "Banner=" + banner + "\nTitleCenterDelta=" + (titleBounds.center.x - backer.rect.center.x)
            + "\nFrame=" + frame + "\nSearch=" + search + "\nBannerGap=" + (search.xMin - banner.xMax) + "\nSearchGap=" + (search.yMin - right.yMax)
            + "\nLeft=" + left + "\nRight=" + right + "\nClose=" + close + "\nHelp=" + help);
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
}
