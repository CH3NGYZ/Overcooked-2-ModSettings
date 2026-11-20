using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private T17Button shortcutButton;
        private KeyCapture shortcutCapture;
        private string shortcutCaption;
        internal bool IsRecordingToggleKey { get { return shortcutCapture != null && shortcutCapture.isActiveAndEnabled; } }
        internal bool IsEditingText
        {
            get
            {
                GameObject selected = Domain.currentSelectedGameObject;
                NativeTextInput input = selected == null ? null : selected.GetComponent<NativeTextInput>();
                return input != null && input.IsEditing;
            }
        }

        private void BuildShortcut(Transform body)
        {
            shortcutButton = NativeUi.CloneButton(ButtonTemplate, body, "ToggleShortcut", "", BeginShortcutCapture, this);
            RectTransform rect = (RectTransform)shortcutButton.transform;
            // Align with the visible list buttons and clear the highest point of the native notch.
            rect.anchorMin = rect.anchorMax = new Vector2(ContentLeftAnchor, .15f);
            rect.pivot = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(168, 40); rect.anchoredPosition = new Vector2(ModTabInset, 0);
            RowFocus focus = shortcutButton.gameObject.AddComponent<RowFocus>();
            focus.Selected = delegate { onRight = false; RefreshPins(); };
            RefreshShortcutKey();
        }
        private void RefreshShortcutKey()
        {
            string caption = IsRecordingToggleKey ? Texts.PressKey : ValueCodec.Format(Shortcut.Setting, Shortcut.Key);
            if (shortcutCaption == caption) return;
            NativeUi.Label(shortcutButton.transform, caption); shortcutCaption = caption;
        }
        private void BeginShortcutCapture()
        {
            if (!ownedState || rebuilding || IsRecordingToggleKey) return;
            CancelEdits(false);
            shortcutButton.Select();
            shortcutCapture = shortcutButton.gameObject.AddComponent<KeyCapture>();
            shortcutCapture.Initialize(this, KeyCapture.Mode.GlobalToggle,
                delegate(object value)
                {
                    CommitResult result = ConfigCommit.Apply(Shortcut.Setting, value);
                    EndShortcutCapture();
                    if (!result.Success) ShowError(result.Error);
                },
                delegate { EndShortcutCapture(); CaptureCancelled(); });
            RefreshShortcutKey(); RefreshFeedback();
        }
        private bool EndShortcutCapture()
        {
            KeyCapture old = shortcutCapture; shortcutCapture = null;
            if (old == null) return false;
            // Unity's script order is not fixed. Tick may see the newly saved key later this frame.
            Shortcut.ConsumeCurrentFrame(); old.Stop();
            RefreshShortcutKey(); RefreshFeedback(); return true;
        }
        private void WireShortcut(Selectable right)
        {
            shortcutButton.navigation = new Navigation { mode = Navigation.Mode.Explicit,
                selectOnUp = tabs.Count == 0 ? null : tabs[tabs.Count - 1],
                selectOnDown = tabs.Count == 0 ? null : tabs[0], selectOnRight = right };
        }
    }
}
