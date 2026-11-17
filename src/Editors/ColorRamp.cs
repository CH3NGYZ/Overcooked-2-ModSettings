using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // Mesh colors avoid per-drag texture allocations. Alpha is shown over a checkerboard.
    internal sealed class ColorRamp : MaskableGraphic
    {
        private Color low, high;
        internal bool Rainbow;
        internal void SetColors(Color from, Color to)
        { low = from; high = to; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect bounds = rectTransform.rect;
            for (int x = 0; x < 32; x++)
            {
                float start = x / 32f, end = (x + 1) / 32f;
                for (int y = 0; y < 2; y++)
                {
                    Color checker = (x / 2 + y) % 2 == 0 ? new Color(.80f, .83f, .85f) : new Color(.37f, .43f, .47f);
                    Rect cell = new Rect(bounds.xMin + bounds.width * start, bounds.yMin + bounds.height * y / 2, bounds.width / 32, bounds.height / 2);
                    Quad(mesh, cell, checker, checker);
                    Quad(mesh, cell, Rainbow ? Color.HSVToRGB(start, 1, 1) : Color.Lerp(low, high, start),
                        Rainbow ? Color.HSVToRGB(end, 1, 1) : Color.Lerp(low, high, end));
                }
            }
        }
        private static void Quad(VertexHelper mesh, Rect rect, Color left, Color right)
        {
            int i = mesh.currentVertCount;
            mesh.AddVert(new Vector3(rect.xMin, rect.yMin), left, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMin, rect.yMax), left, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMax), right, Vector2.zero);
            mesh.AddVert(new Vector3(rect.xMax, rect.yMin), right, Vector2.zero);
            mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
