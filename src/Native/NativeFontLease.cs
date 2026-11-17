using System;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;

namespace OC2ModSettings
{
    // Unity 2017 Font.Instantiate shares native atlas state even with a separate Material.
    // Keep the game's font bundle (and its CJK/Thai dependencies), not the frontend scene.
    internal sealed class NativeFontLease : IDisposable
    {
        private const string Bundle = "bundle22";
        private bool retained;
        private readonly HashSet<Font> kept = new HashSet<Font>();
        internal Font Keep(Font source)
        {
            if (kept.Contains(source)) return source;
            string path = source.name == "Header_Font" ? "assets/gui/fonts/header_font.otf"
                : source.name == "Body_Font" ? "assets/gui/fonts/body_font.ttf" : null;
            string error;
            LoadedAssetBundle loaded = AssetBundleManager.GetLoadedAssetBundle(Bundle, out error);
            if (path == null || loaded == null || loaded.m_AssetBundle.LoadAsset<Font>(path) != source)
                throw new InvalidOperationException("Native UI font bundle identity changed: " + source.name + "; " + error);
            if (!retained)
            {
                // This public path acquires one balanced reference including fallback bundles.
                AssetBundleManager.LoadAssetAsync(Bundle, path, typeof(Font)); retained = true;
            }
            kept.Add(source); return source;
        }
        public void Dispose()
        {
            if (!retained) return;
            retained = false; kept.Clear(); AssetBundleManager.UnloadAssetBundle(Bundle);
        }
    }
}
