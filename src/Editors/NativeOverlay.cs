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
        private T17Button colorClose;
        private T17Text titleLabel;
        private Func<string> localizedTitle;
        private readonly List<T17Button> buttons = new List<T17Button>();
        internal bool IsOpen { get { return panel != null; } }
        internal Transform Root { get { return panel == null ? null : panel.transform; } }
        internal NativeOverlay(NativePage owner, Transform body) { page = owner; parent = body; }
        internal void Choose(string title, string[] values, Action<int> selected)
        {
            RectTransform root = Begin(title);
            CreateChoices(root); PopulateChoices(values, selected);
        }
        internal void FocusChoice(int index)
        { if (page.Domain != null && index >= 0 && index < buttons.Count) page.Domain.SetSelectedGameObject(buttons[index].gameObject); }
        internal void Capture(Func<string> title, bool shortcut, Action<object> selected)
        {
            Func<string> caption = delegate { return title() + "\n" + Texts.Capture; };
            RectTransform root = Begin(caption());
            localizedTitle = caption;
            T17Button clear = NativeUi.CloneButton(page.ButtonTemplate, root, "Clear", Texts.Clear,
                delegate { Close(); selected(shortcut ? (object)new KeyboardShortcut(KeyCode.None) : KeyCode.None); }, page);
            NativeUi.Box(clear.GetComponent<RectTransform>(), 30, 160, 260, 48); clear.SetEventSystem(page.Domain);
            capture = root.gameObject.AddComponent<KeyCapture>();
            capture.Initialize(page, shortcut ? KeyCapture.Mode.Combination : KeyCapture.Mode.Single,
                delegate(object value) { Close(); selected(value); }, page.CaptureCancelled);
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
            titleLabel = NativeUi.Text(page.TextTemplate, NativeUi.Rect(body, "Title", new Vector2(.03f, .87f), new Vector2(.97f, .99f)), "Text", title, 25);
            titleLabel.color = new Color(.1f, .14f, .2f);
            titleLabel.resizeTextForBestFit = true; titleLabel.resizeTextMinSize = 14; titleLabel.resizeTextMaxSize = 25;
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
            choiceRow = null; choiceItems = null; choiceSelected = null; choiceColumn = null; colorClose = null;
            localizedTitle = null; titleLabel = null;
            if (colorPicker != null) { ColorPicker oldPicker = colorPicker; colorPicker = null; oldPicker.Release(); }
            if (capture != null) { capture.Stop(); capture = null; }
            GameObject old = panel; panel = null; buttons.Clear();
            if (old != null) { old.SetActive(false); UnityEngine.Object.Destroy(old); }
            if (previous != null && page.Domain != null && previous.activeInHierarchy) page.Domain.SetSelectedGameObject(previous);
            previous = null;
            page.KeyboardFeedback();
        }
        internal void RefreshLocalizedText()
        {
            if (localizedTitle == null || titleLabel == null) return;
            string title = localizedTitle();
            if (titleLabel.text != title) NativeUi.Label(titleLabel.transform, title);
        }
        internal void RefreshLanguage()
        {
            if (!IsOpen) return;
            RefreshLocalizedText();
            if (choiceRow == null && choiceColumn != null && localizedTitle != null && buttons.Count == 1)
                NativeUi.Label(buttons[0].transform, Texts.Confirm);
            if (capture != null) NativeUi.Label(capture.ClearButton.transform, Texts.Clear);
            if (colorPicker != null)
            {
                colorPicker.RefreshLanguage();
                if (colorClose != null) NativeUi.Label(colorClose.transform, Texts.Close);
            }
            RefreshChoices();
        }
    }
}
