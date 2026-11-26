using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OC2ModSettings;
using UnityEngine;

public sealed partial class ModSettingsUnityTests
{
    private void VerifySettingsTypography(NativePage page, T17Text category, T17Text setting, T17Text action)
    {
        T17Text refresh = page.Right.Content.GetChild(2).GetComponentInChildren<T17Text>();
        T17Text banner = page.transform.Find("SettingsBody/HeaderBacker/Header").GetComponent<T17Text>();
        StringBuilder report = new StringBuilder(Localization.GetLanguage() + " " + Screen.width + "x" + Screen.height + "\n");
        report.AppendLine("Refresh=" + refresh.font.name + " " + refresh.fontStyle + " Banner=" + banner.font.name);
        bool sameFace = true, fits = true;
        foreach (T17Text label in new T17Text[] { category, setting, action })
        {
            Rect bounds = BoundsIn(label.rectTransform, page.Right.Viewport);
            TextGenerator glyphs = label.cachedTextGenerator;
            sameFace &= label.font == refresh.font && label.fontStyle == refresh.fontStyle;
            fits &= label.isActiveAndEnabled && !label.canvasRenderer.cull && glyphs.lineCount == 1
                && glyphs.characterCountVisible >= label.text.Replace(" ", "").Length
                && label.preferredWidth <= bounds.width + 1 && label.preferredHeight <= bounds.height + 1;
            report.AppendLine(label.text + " font=" + label.font.name + " " + label.fontStyle + " size=" + label.fontSize
                + " bounds=" + bounds + " preferred=" + label.preferredWidth + "x" + label.preferredHeight
                + " lines=" + glyphs.lineCount + " visible=" + glyphs.characterCountVisible);
        }
        File.AppendAllText(Path.Combine(output, "settings-typography.txt"), report.ToString());
        Check(refresh.text == Texts.Refresh && banner.font == page.TextTemplate.font && banner.font != refresh.font,
            "toolbar and panel banner keep their distinct native font assets");
        Check(sameFace, "category, config and standalone action titles match the Refresh button font and style");
        Check(fits, "visible category, config and action titles fit at their existing label sizes in " + Localization.GetLanguage());
    }

    private void VerifyFontAssets(NativePage page, string stage)
    {
        StringBuilder report = new StringBuilder(stage + "\n");
        List<Font> fonts = new List<Font>();
        foreach (T17Text text in page.GetComponentsInChildren<T17Text>(true))
            if (text.font != null && !fonts.Contains(text.font)) fonts.Add(text.font);
        bool isolated = true;
        foreach (Font font in fonts)
        {
            Material material = font.material;
            report.AppendLine(font.name + " id=" + font.GetInstanceID() + " dynamic=" + font.dynamic
                + " material=" + (material == null ? "null" : material.GetInstanceID().ToString())
                + " texture=" + (material == null || material.mainTexture == null ? "null" : material.mainTexture.GetInstanceID().ToString()));
            if (font.name.Contains("ModSettings cache")) isolated = false;
            foreach (Font other in Resources.FindObjectsOfTypeAll<Font>())
                if (other != font && material != null && other.material == material)
                {
                    isolated = false;
                    report.AppendLine("SHARED MATERIAL: " + other.name + " id=" + other.GetInstanceID());
                }
        }
        File.AppendAllText(Path.Combine(output, "font-assets.txt"), report.ToString());
        Check(isolated, "retained fonts use one native atlas per font without cloned atlas state or cross-font materials: " + stage);
    }
}
