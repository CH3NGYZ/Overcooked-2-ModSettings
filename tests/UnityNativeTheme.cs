using System;
using System.IO;
using AssetBundles;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;

public sealed partial class ModSettingsUnityTests
{
    private static bool themeFixture, themeInjected;
    private static readonly Color32 FixtureNormal = new Color32(176, 82, 133, 255);
    private static readonly Color32 FixtureHighlight = new Color32(52, 126, 204, 255);
    private void InstallThemeFixture()
    {
        themeFixture = Array.IndexOf(Environment.GetCommandLineArgs(), "-modSettingsThemeFixture") >= 0;
        if (themeFixture)
            harmony.Patch(AccessTools.Method(typeof(NativePageTemplate), "Capture"),
                new HarmonyMethod(typeof(ModSettingsUnityTests), "RecolorLoadedMenu"));
    }
    private static void RecolorLoadedMenu(FrontendRootMenu root)
    {
        if (themeInjected || root == null || NativePageTemplate.Source != null) return;
        string error;
        LoadedAssetBundle loaded = AssetBundleManager.GetLoadedAssetBundle("bundle24", out error);
        if (loaded == null) throw new InvalidOperationException("Native test bundle unavailable: " + error);
        // The original animator and sprite references survive. Only the two runtime textures receive
        // new pixels; the installed bundle is untouched. This exercises the stock animation path.
        RecolorTexture(loaded.m_AssetBundle.LoadAsset<Sprite>("assets/gui/menu/t_ui_menubar_01.png"), FixtureNormal);
        RecolorTexture(loaded.m_AssetBundle.LoadAsset<Sprite>("assets/gui/menu/t_ui_menubar_highlight_01.png"), FixtureHighlight);
        themeInjected = true;
    }
    private static void RecolorTexture(Sprite sprite, Color32 color)
    {
        if (sprite == null) throw new InvalidOperationException("Expected a stock menu state sprite in bundle24.");
        Texture2D destination = sprite.texture;
        Texture2D source = new Texture2D(destination.width, destination.height, destination.format, false);
        try
        {
            Color32[] pixels = new Color32[source.width * source.height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            source.SetPixels32(pixels); source.Apply(false, true);
            Graphics.CopyTexture(source, destination);
            File.AppendAllText(Path.Combine(output, "theme-source.txt"), sprite.name + " " + destination.width + "x" + destination.height
                + " " + destination.format + " => #" + ColorUtility.ToHtmlStringRGB(color) + Environment.NewLine);
        }
        finally { UnityEngine.Object.Destroy(source); }
    }
}
