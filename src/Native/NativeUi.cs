using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal static class NativeUi
    {
        internal static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform; rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
        internal static void ClearChildren(Transform parent)
        { for (int i = parent.childCount - 1; i >= 0; i--) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject); }
        internal static void Strip(GameObject go)
        {
            foreach (MonoBehaviour component in go.GetComponentsInChildren<MonoBehaviour>(true))
                if (!(component is Graphic) && !(component is Selectable) && !(component is LayoutElement))
                    UnityEngine.Object.DestroyImmediate(component);
            foreach (Animator animator in go.GetComponentsInChildren<Animator>(true)) UnityEngine.Object.DestroyImmediate(animator);
            foreach (Selectable selectable in go.GetComponentsInChildren<Selectable>(true))
            {
                T17Button button = selectable as T17Button;
                if (button == null) continue;
                button.OnButtonSelect = null; button.OnButtonDeselect = null; button.OnButtonMove = null;
                button.OnButtonPointerEnter = null; button.OnButtonPointerExit = null;
                button.onClick = new Button.ButtonClickedEvent(); button.m_bShowTooltip = false;
                button.transition = Selectable.Transition.ColorTint;
            }
        }
        internal static void Label(Transform owner, string value)
        {
            foreach (T17Text text in owner.GetComponentsInChildren<T17Text>(true))
            { text.m_NonLocalizedKeyboardText = string.Empty; text.SetNonLocalizedText(value); }
        }
        internal static T17Button CloneButton(T17Button source, Transform parent, string name, string label, Action click)
        { return CloneButton(source, parent, name, label, click, null); }
        internal static T17Button CloneButton(T17Button source, Transform parent, string name, string label, Action click, NativePage page)
        {
            GameObject go = UnityEngine.Object.Instantiate(source.gameObject, parent, false); go.name = name;
            Strip(go); T17Button button = go.GetComponent<T17Button>();
            RectTransform root = go.GetComponent<RectTransform>(); root.localScale = Vector3.one; root.localRotation = Quaternion.identity;
            Graphic hit = go.GetComponent<Graphic>();
            if (hit == null) { Image image = go.AddComponent<Image>(); image.color = Color.clear; hit = image; }
            hit.raycastTarget = true;
            foreach (T17Text text in go.GetComponentsInChildren<T17Text>(true))
            {
                RectTransform rect = text.rectTransform; rect.localScale = Vector3.one; rect.localRotation = Quaternion.identity;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, .5f);
                rect.offsetMin = new Vector2(10, 3); rect.offsetMax = new Vector2(-10, -3);
                text.alignment = TextAnchor.MiddleCenter; text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Truncate; text.raycastTarget = false;
                text.resizeTextForBestFit = true; text.resizeTextMinSize = 14; text.resizeTextMaxSize = 23;
                text.gameObject.SetActive(true);
            }
            button.onClick.AddListener(delegate { click(); }); button.interactable = true; Label(go.transform, label); go.SetActive(true);
            if (page != null) ThemeControl(button, page);
            return button;
        }
        internal static void ThemeControl(Selectable control, NativePage page)
        {
            Image backer = control.GetComponent<Image>(); if (backer == null) backer = control.gameObject.AddComponent<Image>();
            backer.sprite = null; backer.overrideSprite = null; backer.material = null; backer.type = Image.Type.Simple; backer.raycastTarget = true;
            foreach (Graphic graphic in control.GetComponentsInChildren<Graphic>(true))
                if (graphic != backer && !(graphic is Text)) { graphic.enabled = false; graphic.raycastTarget = false; }
            control.targetGraphic = backer; SettingsControlFeedback.Attach(control, backer, page, null);
        }
        internal static T17Text Text(T17Text source, Transform parent, string name, string value, int size)
        {
            GameObject go = UnityEngine.Object.Instantiate(source.gameObject, parent, false); go.name = name;
            Strip(go); T17Text text = go.GetComponent<T17Text>();
            text.m_NonLocalizedKeyboardText = string.Empty; text.SetNonLocalizedText(value);
            text.fontSize = size; text.resizeTextForBestFit = false; text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.alignment = TextAnchor.MiddleLeft; text.rectTransform.localScale = Vector3.one; text.rectTransform.localRotation = Quaternion.identity;
            RectTransform rect = text.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8, 2); rect.offsetMax = new Vector2(-8, -2);
            go.SetActive(true); return text;
        }
        internal static Image Backer(RectTransform rect, Color color)
        { Image image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
        internal static void Box(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        }
    }
}
