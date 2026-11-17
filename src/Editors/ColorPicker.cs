using System;
using System.Globalization;
using UnityEngine;

namespace OC2ModSettings
{
    // The owning overlay calls Release before destroying the panel, including external page disable.
    internal sealed class ColorPicker : MonoBehaviour
    {
        private Func<Color> read;
        private Action<Color> preview;
        private Func<Color, string> commit;
        private Action closed;
        private Color value, observed;
        private float hue, saturation, brightness, alpha;
        private bool updating, released;
        internal ColorPlane Plane;
        internal PreviewSlider Hue, Alpha;
        internal NativeTextInput Hex;
        private NativeTextInput exactAlpha;
        private ColorRamp swatch, alphaRamp;
        private T17Text hueLabel;
        internal void Initialize(NativePage page, Func<Color> current, Action<Color> changed, Func<Color, string> saved, Action cancelled)
        {
            read = current; preview = changed; commit = saved; closed = cancelled;
            Plane = ColorPlane.Create(page, transform);
            Plane.Preview = delegate(float s, float v) { saturation = s; brightness = v; Preview(); };
            Plane.Confirmed = delegate { Save(); }; Plane.Cancelled = Cancel;
            Hue = Slider(page, "Hue", new Vector2(.04f, .17f), new Vector2(.63f, .21f), delegate(float h) { hue = h; Preview(); });
            ColorRamp rainbow = NativeUi.Rect(Hue.transform, "Rainbow", Vector2.zero, Vector2.one).gameObject.AddComponent<ColorRamp>();
            rainbow.Rainbow = true; rainbow.raycastTarget = false; Hue.handleRect.SetAsLastSibling();
            Alpha = Slider(page, "Alpha", new Vector2(.04f, .07f), new Vector2(.63f, .11f), delegate(float a) { alpha = a; Preview(); });
            alphaRamp = NativeUi.Rect(Alpha.transform, "Gradient", Vector2.zero, Vector2.one).gameObject.AddComponent<ColorRamp>();
            alphaRamp.raycastTarget = false; Alpha.handleRect.SetAsLastSibling();
            hueLabel = Label(page, "HueLabel", Texts.Pick("色相", "Hue"), .215f);
            Label(page, "AlphaLabel", Texts.Pick("透明度 A", "Opacity A"), .115f);
            Label(page, "HexLabel", "Hex · #RRGGBBAA", .69f, .68f, .96f);
            Label(page, "ControlsHint", Texts.Pick("拖动选色，松手保存\n方向键：确认后调色，再确认保存", "Drag to preview; release to save\nPad: confirm, adjust, confirm to save"), .35f, .68f, .96f, .12f);
            swatch = NativeUi.Rect(transform, "ColorPreview", new Vector2(.70f, .78f), new Vector2(.94f, .85f)).gameObject.AddComponent<ColorRamp>();
            swatch.raycastTarget = false;
            Hex = NativeTextInput.Create(page, transform, "Hex", "", "#RRGGBBAA");
            Stretch(Hex.GetComponent<RectTransform>(), new Vector2(.68f, .58f), new Vector2(.96f, .67f));
            Hex.ValidationLabel = Label(page, "HexValidation", "", .53f, .68f, .96f, .045f);
            Hex.Changed = delegate(string raw)
            {
                Color next;
                if (!TryHex(raw, out next)) return false;
                ResetPending(); SetColor(next); Hex.ValidationError = Save(); return Hex.ValidationError == null;
            };
            exactAlpha = NativeTextInput.Create(page, transform, "ExactAlpha", "", "0–1");
            Stretch(exactAlpha.GetComponent<RectTransform>(), new Vector2(.68f, .04f), new Vector2(.96f, .13f));
            exactAlpha.ValidationLabel = Label(page, "AlphaValidation", "", 0, .68f, .96f, .035f);
            exactAlpha.Changed = delegate(string raw)
            {
                float a;
                if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out a) || float.IsNaN(a) || a < 0 || a > 1)
                { return false; }
                ResetPending(); alpha = a; Preview(); exactAlpha.ValidationError = Save(); return exactAlpha.ValidationError == null;
            };
            SetColor(read()); observed = read();
        }
        private PreviewSlider Slider(NativePage page, string name, Vector2 min, Vector2 max, Action<float> changed)
        {
            PreviewSlider slider = PreviewSlider.Create(page, transform, delegate(float v) { if (!updating) changed(v); });
            slider.name = name; Stretch(slider.GetComponent<RectTransform>(), min, max);
            slider.Confirmed = delegate { Save(); }; slider.Cancelled = Cancel; return slider;
        }
        private T17Text Label(NativePage page, string name, string text, float bottom)
        { return Label(page, name, text, bottom, .04f, .63f, .045f); }
        private T17Text Label(NativePage page, string name, string text, float bottom, float left, float right)
        { return Label(page, name, text, bottom, left, right, .07f); }
        private T17Text Label(NativePage page, string name, string text, float bottom, float left, float right, float height)
        {
            T17Text label = NativeUi.Text(page.TextTemplate, NativeUi.Rect(transform, name, new Vector2(left, bottom), new Vector2(right, bottom + height)), "Text", text, 20);
            label.color = new Color(.1f, .2f, .27f); return label;
        }
        internal static bool TryHex(string raw, out Color color)
        {
            string digits = (raw ?? "").Trim().TrimStart('#'); color = Color.clear;
            if (digits.Length != 6 && digits.Length != 8) return false;
            uint value;
            return uint.TryParse(digits, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                && ColorUtility.TryParseHtmlString("#" + digits, out color);
        }
        private void Preview()
        {
            if (updating || released) return;
            value = Color.HSVToRGB(hue, saturation, brightness); value.a = alpha;
            RefreshControls(); preview(value);
        }
        private void SetColor(Color color)
        {
            value = color; float h, s, v; Color.RGBToHSV(color, out h, out s, out v);
            // Grayscale and black retain the last meaningful hue so they can be made colorful again.
            if (s > .00001f && v > .00001f) hue = h;
            saturation = s; brightness = v; alpha = color.a; RefreshControls();
        }
        private void RefreshControls()
        {
            updating = true;
            Plane.SetColor(hue, saturation, brightness);
            bool huePending = Hue.Pending, alphaPending = Alpha.Pending;
            Hue.value = hue; Hue.Pending = huePending; Alpha.value = alpha; Alpha.Pending = alphaPending;
            Hex.SetValue("#" + ColorUtility.ToHtmlStringRGBA(value)); exactAlpha.SetValue(alpha.ToString("0.###", CultureInfo.InvariantCulture));
            swatch.SetColors(value, value); Color low = value, high = value; low.a = 0; high.a = 1; alphaRamp.SetColors(low, high);
            NativeUi.Label(hueLabel.transform, Texts.Pick("色相", "Hue") + "  " + (hue * 360).ToString("0", CultureInfo.InvariantCulture) + "°");
            updating = false;
        }
        private void ResetPending() { Plane.Pending = Hue.Pending = Alpha.Pending = false; }
        private string Save()
        {
            if (released) return null;
            string error = commit(value); observed = read(); SetColor(observed); return error;
        }
        private void Cancel()
        { ResetPending(); observed = read(); SetColor(observed); preview(observed); }
        private void LateUpdate()
        {
            if (released || Plane.Pending || Hue.Pending || Alpha.Pending) return;
            Color actual = read();
            if (!actual.Equals(observed)) { observed = actual; SetColor(actual); preview(actual); }
        }
        internal void Release()
        {
            if (released) return; released = true; ResetPending();
            Hex.Cancel(); exactAlpha.Cancel(); closed();
            read = null; preview = null; commit = null; closed = null;
        }
        private void OnDisable() { Release(); }
        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    }
}
