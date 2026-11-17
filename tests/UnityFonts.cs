using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OC2ModSettings;
using UnityEngine;

public sealed partial class ModSettingsUnityTests
{
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
