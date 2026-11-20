using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OC2ModSettings
{
    internal sealed class NativePalette
    {
        private static readonly Color DefaultNormal = new Color32(90, 150, 190, 255);
        private static readonly Color DefaultHighlight = new Color32(255, 163, 57, 255);
        internal readonly Color Normal, Highlight, Row, Category;

        private NativePalette(Color normal, Color highlight)
        {
            Normal = normal; Highlight = highlight;
            Row = Color.Lerp(normal, Color.white, .2f);
            Category = Color.Lerp(normal, Color.black, .12f);
        }

        internal static NativePalette Capture(T17Button button, Image alternateHighlight, Transform staging)
        {
            Color? normal = null, highlighted = null;
            try { ReadButton(button, staging, ref normal, ref highlighted); }
            catch (System.Exception error) { Report(error); }
            Color fill = normal ?? DefaultNormal;
            if (!highlighted.HasValue || highlighted.Value == fill)
            {
                try { highlighted = Read(alternateHighlight); }
                catch (System.Exception error) { Report(error); }
            }
            Color fallback = fill == DefaultHighlight ? DefaultNormal : DefaultHighlight;
            return new NativePalette(fill, !highlighted.HasValue || highlighted.Value == fill ? fallback : highlighted.Value);
        }

        private static void ReadButton(T17Button button, Transform staging, ref Color? normal, ref Color? highlighted)
        {
            Image image = button == null ? null : button.targetGraphic as Image;
            if (image == null) return;
            switch (button.transition)
            {
                case Selectable.Transition.Animation:
                    ReadAnimation(button, staging, ref normal, ref highlighted);
                    if (!normal.HasValue) normal = Read(image);
                    break;
                case Selectable.Transition.SpriteSwap:
                    normal = Read(image.sprite, image.color);
                    if (button.spriteState.highlightedSprite != null) highlighted = Read(button.spriteState.highlightedSprite, image.color);
                    break;
                case Selectable.Transition.ColorTint:
                    ColorBlock colors = button.colors;
                    normal = Read(image.overrideSprite, image.color * colors.normalColor * colors.colorMultiplier);
                    highlighted = Read(image.overrideSprite, image.color * colors.highlightedColor * colors.colorMultiplier);
                    break;
                default:
                    normal = Read(image);
                    break;
            }
        }

        private static void ReadAnimation(T17Button button, Transform staging, ref Color? normal, ref Color? highlighted)
        {
            Animator animator = button.GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            // GameOptions uses animation to replace m_Sprite, not its serialized ColorBlock.
            AnimationClip idle = null, focus = null;
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                if (clip.name == button.animationTriggers.normalTrigger) idle = clip;
                if (clip.name == button.animationTriggers.highlightedTrigger) focus = clip;
            }
            if (idle == null && focus == null) return;
            GameObject sample = Object.Instantiate(button.gameObject, staging, false);
            try
            {
                // This object stays inactive under staging. Unity 2017 still needs the
                // Animator component to sample a non-legacy clip on it in the player.
                Image image = sample.GetComponent<T17Button>().targetGraphic as Image;
                if (image == null) return;
                image.overrideSprite = null;
                if (idle != null) idle.SampleAnimation(sample, idle.length);
                normal = Read(image);
                if (focus != null) { focus.SampleAnimation(sample, focus.length); highlighted = Read(image); }
            }
            finally { Object.DestroyImmediate(sample); }
        }

        private static void Report(System.Exception error)
        { if (ModSettingsPlugin.Instance != null) ModSettingsPlugin.Instance.Report("Native palette capture", error); }

        private static Color? Read(Image image)
        { return image == null ? null : Read(image.overrideSprite, image.color); }

        private static Color? Read(Sprite sprite, Color tint)
        {
            if (tint.a <= .05f) return null;
            if (sprite != null)
            {
                // UV bounds work both for the game's tight sprites and packed sprite regions.
                Vector2 min = Vector2.one, max = Vector2.zero;
                foreach (Vector2 uv in sprite.uv) { min = Vector2.Min(min, uv); max = Vector2.Max(max, uv); }
                Vector2 span = max - min;
                if (span.x <= 0 || span.y <= 0) return null;
                // The central surface excludes the decorative edges. Ignore transparent samples.
                Texture2D sample = NativeTextureReadback.Read(sprite.texture, 8, 8, span * .5f, min + span * .25f);
                try
                {
                    Vector3 rgb = Vector3.zero; int count = 0;
                    foreach (Color32 pixel in sample.GetPixels32())
                        if (pixel.a >= 128) { rgb += new Vector3(pixel.r, pixel.g, pixel.b); count++; }
                    if (count == 0) return null;
                    rgb /= count * 255f; tint.r *= rgb.x; tint.g *= rgb.y; tint.b *= rgb.z;
                }
                finally { Object.Destroy(sample); }
            }
            return new Color(Mathf.Clamp01(tint.r), Mathf.Clamp01(tint.g), Mathf.Clamp01(tint.b), 1);
        }
    }
}
