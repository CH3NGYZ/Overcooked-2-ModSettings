using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyResponsiveRows(NativePage page)
    {
        RectTransform shortRow = page.Right.Content.Find("Setting_Types_boolean") as RectTransform;
        float width = shortRow.rect.width, gap = width * .74f * .02f;
        Rect name = BoundsIn(shortRow.Find("Name") as RectTransform, shortRow);
        Rect toggle = BoundsIn(shortRow.Find("Toggle") as RectTransform, shortRow);
        Rect reset = BoundsIn(shortRow.Find("Default") as RectTransform, shortRow);
        float unit = toggle.width;
        Check(Mathf.Abs(name.width / width - .24f) < .001f, "short names use the 24 percent tier");
        Check(Mathf.Abs(toggle.width - unit) < 1 && Mathf.Abs(reset.width - unit) < 1
            && Mathf.Abs(reset.xMin - toggle.xMax - gap) < 1, "toggle and defaults share a unit with a two-percent action gap");
        Check((shortRow.Find("Toggle/StateIcon") as RectTransform).sizeDelta == new Vector2(64, 64),
            "toggle icon retains its original 64 by 64 UI size");
        foreach (string key in new string[] { "string", "choice", "enum", "shortcut", "range" })
        {
            RectTransform row = page.Right.Content.Find("Setting_Types_" + key) as RectTransform;
            string control = key == "choice" ? "Dropdown" : key == "enum" ? "KeyList" : key == "shortcut" ? "ShortcutValue" : "ExactValue";
            Rect field = BoundsIn(row.Find(control) as RectTransform, row);
            Rect defaults = BoundsIn(row.Find("Default") as RectTransform, row);
            Rect label = BoundsIn(row.Find("Name") as RectTransform, row);
            Check(Mathf.Abs(defaults.width - unit) < 1, key + ": defaults uses the common action width");
            if (key == "range")
            {
                Rect rail = BoundsIn(row.Find("Range") as RectTransform, row);
                Check(Mathf.Abs(field.width - unit) < 1 && Mathf.Abs(field.xMin - rail.xMax - gap) < 1
                    && Mathf.Abs(rail.xMin - label.xMax - width * .02f) < 1, "range fills its remainder and exact input matches the toggle unit");
            }
            else
            {
                Check(Mathf.Abs(field.xMin - label.xMax - width * .02f) < 1, key + ": action area starts two percent after its label");
                if (key == "enum" || key == "shortcut")
                {
                    Transform button = row.Find(key == "enum" ? "Rebind" : "Capture");
                    Rect buttonBounds = BoundsIn(button as RectTransform, row);
                    Check(Mathf.Abs(buttonBounds.width - unit) < 1 && Mathf.Abs(buttonBounds.xMin - field.xMax - gap) < 1
                        && button.GetComponentInChildren<T17Text>().text == Texts.Rebind, key + ": rebind uses the common width and corrected label");
                }
                else Check(Mathf.Abs(defaults.xMin - field.xMax - gap) < 1, key + ": flexible field consumes the remaining width");
            }
        }
        RectTransform vector = page.Right.Content.Find("Setting_Types_vector4") as RectTransform;
        Rect previous = BoundsIn(vector.Find("X") as RectTransform, vector);
        foreach (string axis in new string[] { "Y", "Z", "W" })
        {
            Rect next = BoundsIn(vector.Find(axis) as RectTransform, vector);
            Check(Mathf.Abs(next.width - previous.width) < 1 && Mathf.Abs(next.xMin - previous.xMax - gap) < 1,
                axis + ": component fields divide the remaining width with the common action gap"); previous = next;
        }
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        Image[] searchImages = search.GetComponentsInChildren<Image>(true);
        Image[] buttonImages = shortRow.Find("Default").GetComponentsInChildren<Image>(true);
        bool sameSkin = searchImages.Length == buttonImages.Length;
        for (int i = 0; sameSkin && i < searchImages.Length; i++)
            sameSkin &= searchImages[i].sprite == buttonImages[i].sprite && searchImages[i].type == buttonImages[i].type;
        Check(sameSkin && searchImages.Length > 0, "search and buttons retain the same themed background hierarchy");
        Check(search.transform.Find("Text").GetComponent<T17Text>().color.r > .9f,
            "search uses bright readable text over its shared blue backer");
        NativeTextInput exactInput = page.Right.Content.Find("Setting_Types_string/ExactValue").GetComponent<NativeTextInput>();
        Check(exactInput.GetComponents<T17Button>().Length == 1, "skinned input retains one input button and no duplicate submit listener");
        T17Text measure = shortRow.Find("Name").GetComponentInChildren<T17Text>();
        string[] keys = { "string", "choice", "range", "enum", "boolean", "color", "shortcut" };
        float[] tiers = { .32f, .40f, .49f, .49f, .49f, .49f, .40f };
        List<IDisposable> overrides = new List<IDisposable>();
        for (int i = 0; i < keys.Length; i++)
        {
            float needed = i == 0 ? .28f : i == 1 || i == 6 ? .36f : i == 3 ? 1.6f : .45f;
            string title = "";
            while (true)
            {
                // Add one glyph so different Canvas widths cannot skip a target tier.
                title += "长配置名称"[title.Length % 5];
                float preferred = measure.cachedTextGeneratorForLayout.GetPreferredWidth(title, measure.GetGenerationSettings(Vector2.zero)) / measure.pixelsPerUnit;
                if ((preferred + 16) / width >= needed) break;
            }
            overrides.Add(ModSettingsPlugin.Instance.ConfigureEntryV1(fixture["Types", keys[i]],
                new Dictionary<string, object> { { "DispName", title } }));
        }
        yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        bool aligned = true;
        for (int i = 0; i < keys.Length; i++)
        {
            RectTransform row = page.Right.Content.Find("Setting_Types_" + keys[i]) as RectTransform;
            Rect label = BoundsIn(row.Find("Name") as RectTransform, row);
            T17Text text = row.Find("Name").GetComponentInChildren<T17Text>();
            File.AppendAllText(Path.Combine(output, "responsive-probe.txt"), keys[i] + " rowWidth=" + row.rect.width
                + " preferredWidth=" + text.preferredWidth + " pixelsPerUnit=" + text.pixelsPerUnit
                + " nameShare=" + label.width / row.rect.width + " expected=" + tiers[i] + Environment.NewLine);
            Check(Mathf.Abs(label.width / row.rect.width - tiers[i]) < .001f, keys[i] + ": actual font selects tier " + tiers[i]);
            Rect defaults = BoundsIn(row.Find("Default") as RectTransform, row);
            aligned &= Mathf.Abs(defaults.xMin - reset.xMin) < 1 && Mathf.Abs(defaults.width - unit) < 1;
            string actionName = keys[i] == "range" ? "ExactValue" : keys[i] == "enum" ? "Rebind"
                : keys[i] == "boolean" ? "Toggle" : keys[i] == "color" ? "PickColor" : keys[i] == "shortcut" ? "Capture" : null;
            Rect actionBounds = actionName == null ? defaults : BoundsIn(row.Find(actionName) as RectTransform, row);
            if (actionName != null) aligned &= Mathf.Abs(actionBounds.xMin - toggle.xMin) < 1 && Mathf.Abs(actionBounds.width - unit) < 1;
            File.AppendAllText(Path.Combine(output, "responsive-rows.txt"), keys[i] + " width=" + row.rect.width + " height=" + row.rect.height
                + " name=" + label + " default=" + defaults + " action=" + actionBounds + " preferredHeight=" + text.preferredHeight + Environment.NewLine);
        }
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "responsive-columns.png")); yield return new WaitForEndOfFrame(); yield return null;
        Check(aligned, "24/32/40/49-percent names preserve identical default and toggle/rebind/exact-input columns");
        RectTransform color = page.Right.Content.Find("Setting_Types_color") as RectTransform;
        Rect longHex = BoundsIn(color.Find("Hex") as RectTransform, color);
        Rect longPreview = BoundsIn(color.Find("ColorPreview") as RectTransform, color);
        Check(longHex.width >= 120 && longHex.xMax < longPreview.xMin
            && longPreview.xMax < BoundsIn(color.Find("PickColor") as RectTransform, color).xMin,
            "49-percent color name retains usable Hex, preview and aligned picker without overlap");
        Check(BoundsIn(page.Right.Content.Find("Setting_Types_range/Range") as RectTransform,
            page.Right.Content.Find("Setting_Types_range")).width >= 120, "49-percent range retains a usable slider rail");
        RectTransform longKey = page.Right.Content.Find("Setting_Types_enum") as RectTransform;
        page.Right.Reveal(longKey); yield return null; yield return null;
        T17Text wrapped = longKey.Find("Name").GetComponentInChildren<T17Text>();
        Check(longKey.rect.height > 68 && !wrapped.resizeTextForBestFit && wrapped.cachedTextGenerator.lineCount >= 3,
            "visible name beyond 49 percent wraps and expands its own row without shrinking the label");
        CheckRaycast(longKey.Find("Rebind").GetComponent<T17Button>(), page.Domain, "long wrapped row rebind remains reachable");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "responsive-long-rows.png")); yield return new WaitForEndOfFrame(); yield return null;
        foreach (IDisposable metadata in overrides) metadata.Dispose();
        yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        page.Right.Content.anchoredPosition = Vector2.zero;
    }
}
