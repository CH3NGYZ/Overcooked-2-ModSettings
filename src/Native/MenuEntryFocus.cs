using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // CustomChef animates this native child; the cloned entry owns equivalent selection explicitly.
    internal sealed class MenuEntryFocus : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        private T17Button button;
        private GameObject highlight;
        internal void Initialize(T17Button owner)
        {
            button = owner;
            Transform child = transform.Find("Highlight");
            highlight = child == null ? null : child.gameObject;
            if (highlight != null)
            {
                button.targetGraphic = highlight.GetComponent<Graphic>();
                if (button.targetGraphic != null) button.targetGraphic.raycastTarget = false;
            }
            button.transition = Selectable.Transition.None;
            button.OnButtonPointerEnter = delegate(T17Button hovered) { hovered.Select(); };
            Show(false);
        }
        public void OnSelect(BaseEventData data) { Show(true); }
        public void OnDeselect(BaseEventData data) { Show(false); }
        private void Show(bool selected)
        {
            if (highlight != null) highlight.SetActive(selected);
            foreach (T17Text text in button.GetComponentsInChildren<T17Text>(true))
                text.color = selected ? Color.white : new Color(.28f, .53f, .67f);
        }
    }
}
