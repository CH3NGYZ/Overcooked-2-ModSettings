using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OC2ModSettings
{
    internal static class NativeIcons
    {
        private static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        internal static Sprite Get(string name)
        {
            Sprite sprite;
            if (sprites.TryGetValue(name, out sprite)) return sprite;
            byte[] data;
            using (Stream stream = typeof(NativeIcons).Assembly.GetManifestResourceStream("OC2ModSettings.Assets." + name + ".png"))
            {
                if (stream == null) throw new InvalidOperationException("Missing settings icon: " + name);
                using (BinaryReader reader = new BinaryReader(stream)) data = reader.ReadBytes((int)stream.Length);
            }
            Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
            if (!ImageConversion.LoadImage(texture, data, true))
            { UnityEngine.Object.Destroy(texture); throw new InvalidOperationException("Invalid settings icon: " + name); }
            texture.name = "ModSettings_" + name; texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            UnityEngine.Object.DontDestroyOnLoad(texture); UnityEngine.Object.DontDestroyOnLoad(sprite);
            sprites.Add(name, sprite); return sprite;
        }
        internal static void Dispose()
        {
            foreach (Sprite sprite in sprites.Values)
                if (sprite != null) { UnityEngine.Object.Destroy(sprite.texture); UnityEngine.Object.Destroy(sprite); }
            sprites.Clear();
        }
    }
}
