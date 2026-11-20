using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OC2ModSettings
{
    // DDOL does not protect bundle-owned assets from Unload(true). Copy just the UI assets
    // reachable from the retained template, once per identity, and rebind every reference.
    internal sealed class NativeTemplateAssets : IDisposable
    {
        private readonly Dictionary<Object, Object> copies = new Dictionary<Object, Object>();
        private readonly List<Object> owned = new List<Object>();
        private readonly NativeFontLease fonts = new NativeFontLease();
        internal int Count { get { return owned.Count; } }
        internal void Capture(GameObject template)
        {
            foreach (Graphic graphic in template.GetComponentsInChildren<Graphic>(true))
            {
                Text text = graphic as Text;
                Material original = graphic.material;
                if (text != null && text.font != null)
                {
                    text.font = CopyFont(text.font);
                    // Text.mainTexture supplies the live font atlas. The UI material supports
                    // RectMask2D; font.material uses GUI/Text Shader and ignores partial clips.
                    if (text.font.dynamic) { text.material = null; continue; }
                }
                graphic.material = CopyMaterial(original);
                Image image = graphic as Image;
                if (image != null)
                {
                    Sprite sprite = image.sprite, alternate = image.overrideSprite;
                    image.sprite = CopySprite(sprite); image.overrideSprite = CopySprite(alternate);
                }
            }
            foreach (Selectable selectable in template.GetComponentsInChildren<Selectable>(true))
            {
                SpriteState state = selectable.spriteState;
                state.highlightedSprite = CopySprite(state.highlightedSprite);
                state.pressedSprite = CopySprite(state.pressedSprite);
                state.disabledSprite = CopySprite(state.disabledSprite); selectable.spriteState = state;
            }
        }
        private T Retain<T>(Object source, T copy) where T : Object
        { copy.name = source.name + " (ModSettings cache)"; copies.Add(source, copy); owned.Add(copy); return copy; }
        private Font CopyFont(Font source)
        {
            if (source.dynamic) return fonts.Keep(source);
            Object found; if (copies.TryGetValue(source, out found)) return (Font)found;
            Font copy = Retain(source, Object.Instantiate(source));
            copy.material = CopyMaterial(source.material);
            return copy;
        }
        private Material CopyMaterial(Material source)
        {
            if (source == null) return null;
            Object found; if (copies.TryGetValue(source, out found)) return (Material)found;
            Material copy = Retain(source, new Material(source));
            foreach (string property in new string[] { "_MainTex", "_AlphaTex" })
                if (source.HasProperty(property))
                {
                    Texture texture = source.GetTexture(property);
                    if (texture is Texture2D) copy.SetTexture(property, CopyTexture((Texture2D)texture));
                }
            return copy;
        }
        private Sprite CopySprite(Sprite source)
        {
            if (source == null) return null;
            Object found; if (copies.TryGetValue(source, out found)) return (Sprite)found;
            Rect rect = source.rect;
            Sprite copy = Sprite.Create(CopyTexture(source.texture), rect,
                new Vector2(source.pivot.x / rect.width, source.pivot.y / rect.height),
                source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
            return Retain(source, copy);
        }
        private Texture2D CopyTexture(Texture2D source)
        {
            Object found; if (copies.TryGetValue(source, out found)) return (Texture2D)found;
            Texture2D copy = NativeTextureReadback.Read(source, source.width, source.height, Vector2.one, Vector2.zero);
            try
            {
                copy.Apply(false, true);
                copy.filterMode = source.filterMode; copy.wrapMode = source.wrapMode;
                return Retain(source, copy);
            }
            catch { Object.Destroy(copy); throw; }
        }
        public void Dispose()
        {
            for (int i = owned.Count - 1; i >= 0; i--) if (owned[i] != null) Object.Destroy(owned[i]);
            owned.Clear(); copies.Clear();
            fonts.Dispose();
        }
    }
}
