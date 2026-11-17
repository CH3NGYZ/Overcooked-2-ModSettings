using System;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal static class NativePageTemplate
    {
        private static GameObject storage;
        private static NativeTemplateAssets assets;
        internal static Transform Source { get; private set; }
        internal static Rect VisibleBody { get; private set; }
        internal static void Capture(FrontendRootMenu root)
        {
            if (Source != null || root == null) return;
            Transform source = root.transform.Find("FixedAspectRootCanvas/ScreenSpaceCanvas/GameOptions");
            if (source == null) throw new MissingMemberException("Native GameOptions frame unavailable.");
            Sprite background = source.Find("SettingsBody").GetComponent<Image>().sprite;
            Vector2 min = Vector2.one, max = Vector2.zero;
            foreach (Vector2 vertex in background.vertices)
            {
                Vector2 pixel = vertex * background.pixelsPerUnit + background.pivot;
                Vector2 point = new Vector2(pixel.x / background.rect.width, pixel.y / background.rect.height);
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            VisibleBody = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            storage = new GameObject("ModSettingsNativeTemplate", typeof(RectTransform));
            storage.SetActive(false); UnityEngine.Object.DontDestroyOnLoad(storage);
            try
            {
                GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, storage.transform, false);
                copy.SetActive(false); NativeUi.Strip(copy); NativeTemplateComponents.Rebuild(copy);
                assets = new NativeTemplateAssets(); assets.Capture(copy); Source = copy.transform;
            }
            catch { Dispose(); throw; }
        }
        internal static void Dispose()
        {
            if (storage != null) UnityEngine.Object.Destroy(storage); storage = null; Source = null;
            if (assets != null) assets.Dispose(); assets = null;
        }
    }
}
