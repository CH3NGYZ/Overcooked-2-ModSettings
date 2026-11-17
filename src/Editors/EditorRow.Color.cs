using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        private ColorRamp colorPreview;
        private NativeTextInput hex;
        private Color? colorDraft;
        private void BuildColor()
        {
            hex = NativeTextInput.Create(page, Root, "Hex", "", Texts.HexHint);
            hex.Changed = delegate(string raw)
            {
                Color value;
                if (!ColorPicker.TryHex(raw, out value))
                { return false; }
                colorDraft = null; hex.ValidationError = Commit(value); return hex.ValidationError == null;
            };
            Controls.Add(hex);
            colorPreview = NativeUi.Rect(Root, "ColorPreview", Vector2.zero, Vector2.one).gameObject.AddComponent<ColorRamp>();
            colorPreview.raycastTarget = false;
            Button("PickColor", Texts.PickColor, 0, 1, delegate
            {
                page.Overlay.PickColor(Row.Metadata.Name, delegate { return (Color)Row.Entry.BoxedValue; },
                    delegate(Color value) { colorDraft = value; RefreshColor(value); },
                    delegate(Color value) { colorDraft = null; return Commit(value); },
                    delegate { colorDraft = null; Refresh(); });
            });
        }
        private void RefreshColor(Color value)
        {
            Color actual = colorDraft.HasValue ? colorDraft.Value : value;
            hex.SetValue("#" + ColorUtility.ToHtmlStringRGBA(actual)); colorPreview.SetColors(actual, actual);
        }
    }
}
