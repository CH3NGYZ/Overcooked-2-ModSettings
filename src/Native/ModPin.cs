using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class ModPin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal T17Button Owner, Button;
        internal string Guid;
        private NativePage page;
        private CanvasGroup visibility;
        private Image icon;
        internal static ModPin Create(NativePage page, T17Button owner, string guid)
        {
            ModPin pin = owner.gameObject.AddComponent<ModPin>(); pin.page = page; pin.Owner = owner; pin.Guid = guid;
            if (PinnedMods.IsFixed(guid)) return pin;
            RectTransform root = NativeUi.Rect(owner.transform, "Pin", new Vector2(1, .5f), new Vector2(1, .5f));
            root.sizeDelta = new Vector2(42, 42); root.anchoredPosition = new Vector2(-27, 0);
            Image hit = NativeUi.Backer(root, Color.white);
            pin.Button = root.gameObject.AddComponent<T17Button>(); pin.Button.targetGraphic = hit;
            pin.Button.SetEventSystem(page.Domain); pin.Button.transition = Selectable.Transition.ColorTint;
            SettingsControlFeedback.Attach(pin.Button, hit, page, null);
            pin.visibility = root.gameObject.AddComponent<CanvasGroup>();
            pin.visibility.interactable = true;
            RectTransform image = NativeUi.Rect(root, "Icon", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            image.sizeDelta = new Vector2(28, 28); pin.icon = image.gameObject.AddComponent<Image>();
            pin.icon.raycastTarget = false; pin.icon.preserveAspect = true;
            pin.Button.onClick.AddListener(delegate { page.TogglePin(guid); });
            pin.Button.OnButtonSelect += delegate { page.RefreshPins(); };
            owner.OnButtonSelect += delegate { page.RefreshPins(); };
            RowFocus focus = root.gameObject.AddComponent<RowFocus>(); focus.Row = owner.GetComponent<RectTransform>(); focus.Column = page.Left;
            focus.Selected = page.PinSelected;
            return pin;
        }
        internal void Show(bool visible, bool pinned)
        {
            if (Button == null) return;
            visibility.alpha = visible ? 1 : 0; visibility.blocksRaycasts = visible;
            icon.sprite = NativeIcons.Get(pinned ? "PinFilled" : "PinOutline"); icon.color = pinned ? new Color(1, .83f, .28f) : Color.white;
        }
        public void OnPointerEnter(PointerEventData data) { page.HoverPin(this); }
        public void OnPointerExit(PointerEventData data)
        {
            GameObject hit = data.pointerCurrentRaycast.gameObject;
            if (hit == null || !hit.transform.IsChildOf(transform)) page.LeavePin(this);
        }
    }
}
