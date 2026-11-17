using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class NativeOverlay
    {
        private readonly NativePage page;
        private readonly Transform parent;
        private GameObject panel;
        private GameObject previous;
        private KeyCapture capture;
        private ColorPicker colorPicker;
        private readonly List<T17Button> buttons = new List<T17Button>();
        internal bool IsOpen { get { return panel != null; } }
        internal Transform Root { get { return panel == null ? null : panel.transform; } }
        internal NativeOverlay(NativePage owner, Transform body) { page = owner; parent = body; }
        internal void Choose(string title, string[] values, Action<int> selected)
        {
            RectTransform root = Begin(title);
            ScrollColumn column = new ScrollColumn(root, "Choices", new Vector2(.04f, .08f), new Vector2(.96f, .86f));
            column.AddScrollbar(page.ScrollbarTemplate); column.Bar.SetEventSystem(page.Domain);
            float width = Mathf.Max(300, root.rect.width * .92f - 30);
            for (int i = 0; i < values.Length; i++)
            {
                int index = i;
                T17Button button = NativeUi.CloneButton(page.ButtonTemplate, column.Content, "Choice_" + i, values[i],
                    delegate { Close(); selected(index); }, page);
                NativeUi.Box(button.GetComponent<RectTransform>(), 2, i * 48, width - 4, 44);
                button.SetEventSystem(page.Domain); buttons.Add(button);
                RowFocus focus = button.gameObject.AddComponent<RowFocus>(); focus.Column = column; focus.Row = button.GetComponent<RectTransform>();
            }
            column.Height(values.Length * 48); BindChoices();
        }
        internal void FocusChoice(int index)
        { if (page.Domain != null && index >= 0 && index < buttons.Count) page.Domain.SetSelectedGameObject(buttons[index].gameObject); }
        internal void Capture(string title, bool shortcut, Action<object> selected)
        {
            RectTransform root = Begin(title + "\n" + Texts.Capture);
            T17Button clear = NativeUi.CloneButton(page.ButtonTemplate, root, "Clear", Texts.Clear,
                delegate { Close(); selected(shortcut ? (object)new KeyboardShortcut(KeyCode.None) : KeyCode.None); }, page);
            NativeUi.Box(clear.GetComponent<RectTransform>(), 30, 160, 260, 48); clear.SetEventSystem(page.Domain);
            capture = root.gameObject.AddComponent<KeyCapture>();
            capture.Initialize(page.Domain, shortcut, delegate(object value) { Close(); selected(value); }, page.CaptureCancelled);
            capture.ClearButton = clear;
        }
        private RectTransform Begin(string title)
        {
            Close(); page.KeyboardFeedback(); previous = page.Domain == null ? null : page.Domain.currentSelectedGameObject;
            RectTransform shield = NativeUi.Rect(parent.GetComponentInParent<Canvas>().transform,
                "ModSettingsOverlay", Vector2.zero, Vector2.one);
            NativeUi.Backer(shield, new Color(.04f, .07f, .12f, .82f)); panel = shield.gameObject;
            RectTransform body = NativeUi.Rect(shield, "Dialog", new Vector2(.14f, .15f), new Vector2(.86f, .88f));
            NativeUi.Backer(body, new Color(.9f, .93f, .94f, 1));
            OverlayDismiss dismiss = shield.gameObject.AddComponent<OverlayDismiss>();
            dismiss.Initialize(body, page.Domain, Close);
            T17Text label = NativeUi.Text(page.TextTemplate, NativeUi.Rect(body, "Title", new Vector2(.03f, .87f), new Vector2(.97f, .99f)), "Text", title, 25);
            label.color = new Color(.1f, .14f, .2f);
            shield.SetAsLastSibling(); return body;
        }
        private void BindChoices()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                Navigation nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp = i > 0 ? buttons[i - 1] : null;
                nav.selectOnDown = i + 1 < buttons.Count ? buttons[i + 1] : null; buttons[i].navigation = nav;
            }
            if (page.Domain != null && buttons.Count > 0) page.Domain.SetSelectedGameObject(buttons[0].gameObject);
        }
        internal void Close()
        {
            if (colorPicker != null) { ColorPicker oldPicker = colorPicker; colorPicker = null; oldPicker.Release(); }
            if (capture != null) { capture.Release(); capture = null; }
            GameObject old = panel; panel = null; buttons.Clear();
            if (old != null) { old.SetActive(false); UnityEngine.Object.Destroy(old); }
            if (previous != null && page.Domain != null && previous.activeInHierarchy) page.Domain.SetSelectedGameObject(previous);
            previous = null;
            page.KeyboardFeedback();
        }
    }

    internal sealed class KeyCapture : MonoBehaviour
    {
        private static readonly KeyCode[] Keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        private static readonly KeyCode[] Modifiers = new KeyCode[] { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftCommand, KeyCode.RightCommand };
        private Suppressor suppressor;
        private T17EventSystem domain;
        private bool shortcut;
        private int startFrame;
        private Action<object> selected;
        private Action cancel;
        private ILogicalButton cancelButton;
        internal T17Button ClearButton;
        internal void Initialize(T17EventSystem system, bool isShortcut, Action<object> result, Action cancelled)
        {
            domain = system; shortcut = isShortcut; selected = result; cancel = cancelled; startFrame = Time.frameCount;
            cancelButton = PlayerInputLookup.GetEngagedButton(PlayerInputLookup.LogicalButtonID.UICancel, PlayerInputLookup.Player.One, PadSide.Both);
            if (domain != null) suppressor = domain.Disable(this);
        }
        private void Update()
        {
            if (Time.frameCount <= startFrame + 1) return;
            if (Input.GetKeyDown(KeyCode.BackQuote)) return;
            if (Input.GetKeyUp(KeyCode.Escape) || (cancelButton != null && cancelButton.JustReleased()))
            { if (cancelButton != null) { cancelButton.ClaimPressEvent(); cancelButton.ClaimReleaseEvent(); } cancel(); return; }
            if (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)
                || (Input.GetMouseButtonUp(0) && ClearButton != null && RectTransformUtility.RectangleContainsScreenPoint(
                    ClearButton.GetComponent<RectTransform>(), Input.mousePosition, EventCamera())))
            { selected(shortcut ? (object)new KeyboardShortcut(KeyCode.None) : KeyCode.None); return; }
            foreach (KeyCode key in Keys)
            {
                if (key == KeyCode.None || key == KeyCode.Escape || (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6) || (shortcut && Array.IndexOf(Modifiers, key) >= 0) || !Input.GetKeyDown(key)) continue;
                List<KeyCode> modifiers = new List<KeyCode>();
                if (shortcut) foreach (KeyCode modifier in Modifiers) if (Input.GetKey(modifier)) modifiers.Add(modifier);
                AcceptKey(key, modifiers.ToArray()); return;
            }
        }
        internal void AcceptKey(KeyCode key, KeyCode[] modifiers)
        {
            if (!enabled || key == KeyCode.None || key == KeyCode.Escape || key == KeyCode.BackQuote) return;
            if (shortcut && Array.IndexOf(Modifiers, key) >= 0) return;
            selected(shortcut ? (object)new KeyboardShortcut(key, modifiers) : key);
        }
        internal void Release()
        { if (suppressor != null) { if (domain != null) domain.ReleaseSuppressor(suppressor); else suppressor.Release(); suppressor = null; } }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
        private Camera EventCamera()
        {
            Canvas canvas = ClearButton.GetComponentInParent<Canvas>();
            GraphicRaycaster raycaster = ClearButton.GetComponentInParent<GraphicRaycaster>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
                : raycaster != null ? raycaster.eventCamera : canvas.worldCamera;
        }
    }
}
