using UnityEngine;

namespace OC2ModSettings
{
    internal static class NativeTextureReadback
    {
        // UI bundle textures can be non-readable. Copy only the requested region to the CPU.
        internal static Texture2D Read(Texture source, int width, int height, Vector2 scale, Vector2 offset)
        {
            RenderTexture previous = RenderTexture.active;
            RenderTexture staging = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            Texture2D copy = null;
            try
            {
                Graphics.Blit(source, staging, scale, offset); RenderTexture.active = staging;
                copy = new Texture2D(width, height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0, 0, width, height), 0, 0, false); return copy;
            }
            catch { if (copy != null) Object.Destroy(copy); throw; }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(staging); }
        }
    }
}
