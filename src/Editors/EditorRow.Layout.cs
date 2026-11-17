using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        // Use the narrowest supported action area for a shared button column on every row.
        private const float ActionUnit = .49f * .24f, ActionGap = .74f * .02f;
        private void Arrange()
        {
            if (custom != null)
            {
                Height = customHeight;
                Place("Name", .01f, .99f, 4, 36);
                RectTransform content = Root.Find("CustomEditor") as RectTransform;
                content.anchorMin = new Vector2(.02f, 0); content.anchorMax = new Vector2(.99f, 1);
                float before = Height;
                ArrangeActions(10, before, 0);
                content.offsetMin = new Vector2(3, 8 + Height - before); content.offsetMax = new Vector2(-3, -46); return;
            }
            float width = Mathf.Max(300, page.Right.Viewport.rect.width - 32);
            T17Text label = Root.Find("Name").GetComponentInChildren<T17Text>(true);
            float name = NameShare((label.preferredWidth + 16) / width);
            float start = name + .02f;
            float gap = ActionGap, button = ActionUnit;
            float end = Root.Find("Default") == null ? 1 : 1 - button - gap;
            int inline = Row.Action == null && actionButtons.Count > 0 && components.Count == 0 && hex == null && slider == null ? 1 : 0;
            if (inline > 0 && end - button - gap - start < 160 / width) inline = 0;
            if (inline > 0) end -= button + gap;
            Height = components.Count > 0 ? 88 : 68;
            TextGenerationSettings settings = label.GetGenerationSettings(new Vector2(name * width - 16, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float nameHeight = label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text, settings) / label.pixelsPerUnit;
            Height = Mathf.Max(Height, Mathf.Ceil(nameHeight) + 12);
            Place("Name", 0, name, 4, Height - 8);
            float top = (Height - 48) / 2;
            // Fixed actions share one unit; flexible fields consume the remaining action space.
            Place("Toggle", end - button, end, top, 48);
            Place("Capture", end - button, end, top, 48);
            Place("ShortcutValue", start, end - button - gap, top, 48);
            Place("KeyList", start, end - button - gap, top, 48);
            Place("Rebind", end - button, end, top, 48);
            Place("Dropdown", start, end, top, 48);
            Place("Value", start, end, 4, Height - 8);
            Place("Default", 1 - button, 1, top, 48);
            Place("ExactValue", start, end, top, 48);
            float primaryHeight = Height;
            ArrangeActions(top, primaryHeight, inline);
            if (slider != null)
            {
                float exact = end - button;
                Place("ExactValue", exact, end, top, 48);
                Place("Range", start, exact - gap, top + (Row.Metadata.Percent ? 28 : 15), 18);
                Place("Percent", start, exact - gap, top - 3, 28);
            }
            if (hex != null)
            {
                float previewStart = end - button - gap - .06f;
                Place("Hex", start, previewStart - gap, top, 48);
                Place("ColorPreview", previewStart, end - button - gap, top, 48);
                Place("PickColor", end - button, end, top, 48); return;
            }
            if (components.Count == 0) return;
            float part = (end - start - gap * (components.Count - 1)) / components.Count;
            top = (primaryHeight - 74) / 2;
            for (int i = 0; i < components.Count; i++)
            {
                float min = start + (part + gap) * i, max = min + part;
                string axis = components[i].name; AxisLabel(axis);
                Place("Axis_" + axis, min, max, top, 25); Place(axis, min, max, top + 27, 46);
            }
            Place("Default", 1 - button, 1, top + 27, 46);
        }
        private static float NameShare(float needed)
        {
            foreach (float share in new float[] { .24f, .32f, .40f }) if (needed <= share) return share;
            return .49f;
        }
        private void AxisLabel(string name)
        {
            T17Text axis = NativeUi.Text(page.TextTemplate,
                NativeUi.Rect(Root, "Axis_" + name, Vector2.zero, Vector2.one), "Label", name, 16);
            axis.color = Color.white; axis.alignment = TextAnchor.MiddleCenter;
        }
        private void Place(string name, float min, float max, float top, float height)
        {
            RectTransform rect = Root.Find(name) as RectTransform;
            if (rect == null) return;
            rect.anchorMin = new Vector2(min, 1); rect.anchorMax = new Vector2(max, 1);
            rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -top);
            rect.sizeDelta = new Vector2(0, height);
        }
    }
}
