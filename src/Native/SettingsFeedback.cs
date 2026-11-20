using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // The page chooses one feedback target, so row and child highlights cannot overlap.
    internal sealed class SettingsControlFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IMoveHandler
    {
        internal NativePage Page;
        private Selectable control;
        private Graphic backer, indicator;
        private bool lit;
        internal static void Attach(Selectable control, Graphic backer, NativePage page, Graphic indicator)
        {
            SettingsControlFeedback feedback = control.gameObject.AddComponent<SettingsControlFeedback>();
            feedback.Page = page; feedback.control = control; feedback.backer = backer; feedback.indicator = indicator;
            control.transition = Selectable.Transition.None; feedback.Paint(false);
        }
        internal void Paint(bool active)
        {
            lit = active;
            Color color = active && control.IsInteractable() ? Page.Palette.Highlight : Page.Palette.Normal;
            if (!control.IsInteractable()) color.a = .45f;
            // A cloned Selectable may still have a native tint tween running on its Graphic.
            // Setting transition=None and the current tint alone does not stop that coroutine.
            backer.CrossFadeColor(Color.white, 0, true, true);
            backer.color = color; backer.canvasRenderer.SetColor(Color.white);
            if (indicator != null)
            {
                indicator.CrossFadeColor(Color.white, 0, true, true);
                indicator.color = Color.white; indicator.canvasRenderer.SetColor(Color.white);
            }
        }
        internal void RefreshState() { Paint(lit); }
        public void OnPointerEnter(PointerEventData data) { Page.PointerFeedback(data, gameObject); }
        public void OnPointerExit(PointerEventData data) { Page.PointerFeedback(data, null); }
        public void OnSelect(BaseEventData data) { Page.RefreshFeedback(); }
        public void OnDeselect(BaseEventData data) { Page.RefreshFeedback(); }
        public void OnMove(AxisEventData data) { Page.KeyboardFeedback(); }
        private void OnDisable() { if (Page != null) Page.ForgetFeedback(this); }
    }

    internal sealed class SettingsRowFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        internal NativePage Page;
        internal SettingRow Row;
        private Image backer;
        internal void Initialize(NativePage page, SettingRow row)
        { Page = page; Row = row; backer = GetComponent<Image>(); backer.raycastTarget = true; Paint(false); }
        internal void Paint(bool active)
        { backer.color = active ? Page.Palette.Highlight : Page.Palette.Row; }
        public void OnPointerEnter(PointerEventData data) { Page.PointerFeedback(data, gameObject); }
        public void OnPointerExit(PointerEventData data) { Page.PointerFeedback(data, null); }
        private void OnDisable() { if (Page != null) Page.ForgetFeedback(this); }
    }
}
