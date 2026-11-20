using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class ScrollColumn
    {
        internal readonly ScrollRect Scroll;
        internal readonly RectTransform Viewport, Content;
        internal T17Scrollbar Bar { get; private set; }
        internal ScrollColumn(Transform parent, string name, Vector2 min, Vector2 max)
        {
            RectTransform root = NativeUi.Rect(parent, name, min, max);
            Viewport = NativeUi.Rect(root, "Viewport", Vector2.zero, Vector2.one);
            NativeUi.Backer(Viewport, new Color(.13f, .17f, .22f, .10f));
            Viewport.gameObject.AddComponent<RectMask2D>();
            Content = NativeUi.Rect(Viewport, "Content", new Vector2(0, 1), Vector2.one);
            Content.pivot = new Vector2(0, 1);
            Scroll = root.gameObject.AddComponent<ScrollRect>(); Scroll.viewport = Viewport; Scroll.content = Content;
            Scroll.horizontal = false; Scroll.vertical = true; Scroll.inertia = false;
            Scroll.movementType = ScrollRect.MovementType.Clamped; Scroll.scrollSensitivity = 42;
        }
        internal void AddScrollbar(T17Scrollbar template, Color normal)
        {
            if (template == null) throw new System.InvalidOperationException("Native scrollbar template component missing.");
            GameObject copy = UnityEngine.Object.Instantiate(template.gameObject, Scroll.transform, false);
            copy.name = "VerticalScrollbar"; NativeUi.Strip(copy);
            Bar = copy.GetComponent<T17Scrollbar>();
            if (Bar == null || Bar.handleRect == null) throw new System.InvalidOperationException("Native scrollbar clone handle missing; template handle=" + template.handleRect);
            Graphic track = copy.GetComponent<Graphic>();
            if (track == null) track = copy.AddComponent<Image>();
            track.color = normal;
            track.raycastTarget = true;
            foreach (Graphic graphic in Bar.handleRect.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = true;
            Bar.onValueChanged = new Scrollbar.ScrollEvent();
            Bar.navigation = new Navigation { mode = Navigation.Mode.None };
            Bar.direction = Scrollbar.Direction.BottomToTop; Bar.numberOfSteps = 0;
            RectTransform rect = copy.GetComponent<RectTransform>();
            rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
            rect.anchorMin = new Vector2(1, 0); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = new Vector2(-22, 4); rect.offsetMax = new Vector2(-2, -4);
            Viewport.offsetMax = new Vector2(-30, 0);
            Scroll.verticalScrollbar = Bar; Scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            copy.SetActive(true);
        }
        internal void Height(float height) { Content.sizeDelta = new Vector2(0, height); }
        internal void Reveal(RectTransform row)
        {
            if (row == null) return;
            float top = -row.anchoredPosition.y, bottom = top + row.rect.height;
            float offset = Content.anchoredPosition.y, view = Viewport.rect.height;
            if (top < offset) offset = top;
            else if (bottom > offset + view) offset = bottom - view;
            Content.anchoredPosition = new Vector2(0, Mathf.Clamp(offset, 0, Mathf.Max(0, Content.rect.height - view)));
        }
    }

    internal sealed class RowFocus : MonoBehaviour, ISelectHandler
    {
        internal ScrollColumn Column;
        internal RectTransform Row;
        internal System.Action Selected;
        public void OnSelect(BaseEventData data) { if (Column != null) Column.Reveal(Row); if (Selected != null) Selected(); }
    }
}
