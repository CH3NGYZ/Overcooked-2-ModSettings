using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class ShortcutWarning : MaskableGraphic, IPointerEnterHandler, IPointerExitHandler
    {
        internal SettingRow Row { get; private set; }
        private NativePage page;
        private T17Text label;
        private float labelRight;

        internal static ShortcutWarning Create(NativePage page, SettingRow row, Transform holder, T17Text label, params Selectable[] controls)
        {
            Vector2 right = new Vector2(1, .5f);
            RectTransform rect = NativeUi.Rect(holder, "ShortcutWarning", right, right);
            rect.sizeDelta = new Vector2(22, 22); rect.anchoredPosition = new Vector2(-18, 0);
            ShortcutWarning warning = rect.gameObject.AddComponent<ShortcutWarning>();
            warning.page = page; warning.Row = row; warning.label = label;
            warning.labelRight = label.rectTransform.offsetMax.x;
            warning.color = new Color32(255, 210, 96, 255);
            foreach (Selectable control in controls)
            {
                SettingsControlFeedback feedback = control.GetComponent<SettingsControlFeedback>();
                if (feedback != null) feedback.ShortcutRow = row;
            }
            rect.gameObject.SetActive(false);
            return warning;
        }

        internal void Show(bool visible)
        {
            if (gameObject.activeSelf == visible) return;
            Vector2 offset = label.rectTransform.offsetMax;
            offset.x = visible ? Mathf.Min(labelRight, -34) : labelRight;
            label.rectTransform.offsetMax = offset;
            gameObject.SetActive(visible);
        }

        public void OnPointerEnter(PointerEventData data) { page.PointerFeedback(data, gameObject); }
        public void OnPointerExit(PointerEventData data) { page.PointerFeedback(data, null); }
        protected override void OnDisable() { if (page != null) page.ForgetFeedback(this); base.OnDisable(); }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Color32 ink = new Color32(37, 34, 29, 255);
            Triangle(mesh, new Vector2(0, 11), new Vector2(11, -9), new Vector2(-11, -9), ink);
            Triangle(mesh, new Vector2(0, 7.5f), new Vector2(8.5f, -7.5f), new Vector2(-8.5f, -7.5f), color);
            Quad(mesh, -1.2f, -1, 1.2f, 3.8f, ink);
            Quad(mesh, -1.2f, -5.6f, 1.2f, -3.3f, ink);
        }
        private static void Triangle(VertexHelper mesh, Vector2 top, Vector2 right, Vector2 left, Color32 tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(top, tint, Vector2.zero); mesh.AddVert(right, tint, Vector2.zero); mesh.AddVert(left, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
        }
        private static void Quad(VertexHelper mesh, float left, float bottom, float right, float top, Color32 tint)
        {
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector2(left, bottom), tint, Vector2.zero); mesh.AddVert(new Vector2(left, top), tint, Vector2.zero);
            mesh.AddVert(new Vector2(right, top), tint, Vector2.zero); mesh.AddVert(new Vector2(right, bottom), tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2); mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
