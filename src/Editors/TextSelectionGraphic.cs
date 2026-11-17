using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // The selection is geometry, not inserted text: hit positions use the actual rendered glyphs.
    internal sealed class TextSelectionGraphic : MaskableGraphic
    {
        internal NativeTextInput Owner;
        internal T17Text Label;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Owner == null || !Owner.IsEditing || Label == null) return;
            TextGenerator generator = Label.cachedTextGenerator;
            generator.Populate(Label.text, Label.GetGenerationSettings(Label.rectTransform.rect.size));
            float scale = Label.pixelsPerUnit;
            for (int line = 0; line < generator.lineCount; line++)
            {
                UILineInfo info = generator.lines[line];
                int end = line + 1 < generator.lineCount ? generator.lines[line + 1].startCharIdx : generator.characterCountVisible;
                float top = info.topY / scale, bottom = top - info.height / scale;
                int start = Mathf.Max(info.startCharIdx, Owner.SelectionStart), stop = Mathf.Min(end, Owner.SelectionEnd);
                if (start < stop)
                {
                    // At a wrapped boundary the next glyph belongs to the next line's left edge.
                    float right = stop == end && line + 1 < generator.lineCount
                        ? (generator.characters[stop - 1].cursorPos.x + generator.characters[stop - 1].charWidth) / scale : Position(generator, stop);
                    Quad(mesh, Position(generator, start), bottom, right, top, new Color(.65f, .87f, 1, .42f));
                }
                if (Owner.Caret >= info.startCharIdx && (Owner.Caret < end || line == generator.lineCount - 1))
                {
                    float x = Position(generator, Owner.Caret + Input.compositionString.Length);
                    Quad(mesh, x, bottom, x + 1.5f, top, Color.white);
                }
            }
        }
        private float Position(TextGenerator generator, int index)
        {
            if (generator.characters.Count == 0) return Label.rectTransform.rect.xMin;
            if (index < generator.characters.Count) return generator.characters[index].cursorPos.x / Label.pixelsPerUnit;
            UICharInfo last = generator.characters[generator.characters.Count - 1];
            return (last.cursorPos.x + last.charWidth) / Label.pixelsPerUnit;
        }
        private void Quad(VertexHelper mesh, float left, float bottom, float right, float top, Color color)
        {
            Rect bounds = rectTransform.rect;
            left = Mathf.Max(bounds.xMin, left); right = Mathf.Min(bounds.xMax, right);
            bottom = Mathf.Max(bounds.yMin, bottom); top = Mathf.Min(bounds.yMax, top);
            if (left >= right || bottom >= top) return;
            int index = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero); mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero); mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index + 2, index + 3, index);
        }
    }
}
