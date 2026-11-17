using System;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        private decimal rangeMin, rangeMax, rangeSpan;
        private T17Text percent;
        private void BuildRange()
        {
            object acceptable = Row.Entry.Description.AcceptableValues;
            if (acceptable == null || !ValueCodec.Numeric(Row.Entry.SettingType)) return;
            PropertyInfo min = acceptable.GetType().GetProperty("MinValue"), max = acceptable.GetType().GetProperty("MaxValue");
            if (min == null || max == null) return;
            try
            {
                rangeMin = Convert.ToDecimal(min.GetValue(acceptable, null), CultureInfo.InvariantCulture);
                rangeMax = Convert.ToDecimal(max.GetValue(acceptable, null), CultureInfo.InvariantCulture);
                if (rangeMax <= rangeMin) return;
                rangeSpan = rangeMax - rangeMin;
            }
            catch (OverflowException) { return; } // Exact text remains available for ranges beyond decimal.
            Height = 154;
            if (Row.Metadata.Percent)
                percent = NativeUi.Text(page.TextTemplate, NativeUi.Rect(Root, "Percent", new Vector2(.82f, 0), new Vector2(1, .12f)), "Text", "", 16);
            slider = PreviewSlider.Create(page, Root, delegate(float position)
            {
                if (refreshing) return;
                preview = FromNormalized(position);
                if (text != null) text.SetValue(ValueCodec.Format(Row, preview));
                RefreshPercent(position);
            });
            RectTransform rect = slider.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(.02f, .02f); rect.anchorMax = new Vector2(.8f, .12f);
            rect.offsetMin = rect.offsetMax = Vector2.zero; Controls.Add(slider);
            slider.Confirmed = delegate { if (preview != null) Commit(preview); };
            slider.Cancelled = delegate { preview = null; Refresh(); };
        }
        private void RefreshPercent(float position)
        { if (percent != null) NativeUi.Label(percent.transform, (position * 100).ToString("F1", CultureInfo.InvariantCulture) + "%"); }
        private float ToNormalized(object value)
        { return rangeSpan <= 0 ? 0 : (float)((Convert.ToDecimal(value, CultureInfo.InvariantCulture) - rangeMin) / rangeSpan); }
        private object FromNormalized(float position)
        {
            decimal value = rangeMin + rangeSpan * (decimal)Mathf.Clamp01(position);
            TypeCode code = Type.GetTypeCode(Row.Entry.SettingType);
            if (code >= TypeCode.SByte && code <= TypeCode.UInt64) value = decimal.Round(value, 0);
            return Convert.ChangeType(value, Row.Entry.SettingType, CultureInfo.InvariantCulture);
        }
    }
}
