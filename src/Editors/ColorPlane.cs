using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class ColorPlane : T17Button, IDragHandler, IBeginDragHandler, IEndDragHandler
    {
        internal bool Pending;
        internal Action<float, float> Preview;
        internal Action Confirmed, Cancelled;
        private bool editing;
        private float saturation, brightness;
        private RectTransform cursor;
        internal ColorSquare Square;
        internal static ColorPlane Create(NativePage page, Transform parent)
        {
            RectTransform rect = NativeUi.Rect(parent, "SaturationValue", new Vector2(.04f, .27f), new Vector2(.63f, .85f));
            ColorSquare square = rect.gameObject.AddComponent<ColorSquare>();
            ColorPlane plane = rect.gameObject.AddComponent<ColorPlane>(); plane.Square = square;
            // Do not tint the actual color surface when focused.
            plane.transition = Transition.None; plane.targetGraphic = square; plane.SetEventSystem(page.Domain);
            RectTransform cursor = NativeUi.Rect(rect, "Cursor", Vector2.zero, Vector2.zero);
            cursor.sizeDelta = new Vector2(20, 20); plane.cursor = cursor;
            Image image = NativeUi.Backer(cursor, Color.clear); image.raycastTarget = false;
            Outline outline = cursor.gameObject.AddComponent<Outline>(); outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2); image.color = Color.white;
            RectTransform center = NativeUi.Rect(cursor, "Center", new Vector2(.25f, .25f), new Vector2(.75f, .75f));
            NativeUi.Backer(center, Color.black).raycastTarget = false;
            return plane;
        }
        internal void SetColor(float hue, float s, float v)
        {
            saturation = s; brightness = v; Square.SetHue(hue);
            cursor.anchorMin = cursor.anchorMax = new Vector2(s, v); cursor.anchoredPosition = Vector2.zero;
        }
        private void Change(float s, float v)
        {
            saturation = Mathf.Clamp01(s); brightness = Mathf.Clamp01(v); Pending = true;
            if (Preview != null) Preview(saturation, brightness);
        }
        private void Pointer(PointerEventData data)
        {
            Vector2 point; RectTransform rect = (RectTransform)transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, data.position, data.pressEventCamera, out point))
                Change((point.x - rect.rect.xMin) / rect.rect.width, (point.y - rect.rect.yMin) / rect.rect.height);
        }
        public override void OnPointerDown(PointerEventData data)
        { if (data.button != PointerEventData.InputButton.Left) return; base.OnPointerDown(data); Pointer(data); }
        public void OnBeginDrag(PointerEventData data) { }
        public void OnDrag(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Pointer(data); }
        public void OnEndDrag(PointerEventData data) { }
        public override void OnPointerUp(PointerEventData data)
        { base.OnPointerUp(data); if (data.button == PointerEventData.InputButton.Left) Commit(); }
        public override void OnSubmit(BaseEventData data)
        { if (editing) { editing = false; Commit(); } else editing = true; data.Use(); }
        public override void OnMove(AxisEventData data)
        {
            if (!editing) { base.OnMove(data); return; }
            Change(saturation + (data.moveDir == MoveDirection.Right ? .02f : data.moveDir == MoveDirection.Left ? -.02f : 0),
                brightness + (data.moveDir == MoveDirection.Up ? .02f : data.moveDir == MoveDirection.Down ? -.02f : 0)); data.Use();
        }
        public override void OnDeselect(BaseEventData data) { editing = false; Cancel(); base.OnDeselect(data); }
        internal void Commit() { if (!Pending) return; Pending = false; if (Confirmed != null) Confirmed(); }
        internal void Cancel() { if (!Pending) return; Pending = false; if (Cancelled != null) Cancelled(); }
    }

    internal sealed class ColorSquare : MaskableGraphic
    {
        private float hue = -1;
        internal void SetHue(float value) { if (hue == value) return; hue = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); Rect r = rectTransform.rect;
            for (int x = 0; x < 16; x++) for (int y = 0; y < 16; y++)
            {
                int i = mesh.currentVertCount;
                Add(mesh, r, x / 16f, y / 16f); Add(mesh, r, x / 16f, (y + 1) / 16f);
                Add(mesh, r, (x + 1) / 16f, (y + 1) / 16f); Add(mesh, r, (x + 1) / 16f, y / 16f);
                mesh.AddTriangle(i, i + 1, i + 2); mesh.AddTriangle(i, i + 2, i + 3);
            }
        }
        private void Add(VertexHelper mesh, Rect r, float s, float v)
        { mesh.AddVert(new Vector3(r.xMin + s * r.width, r.yMin + v * r.height), Color.HSVToRGB(hue, s, v), Vector2.zero); }
    }
}
