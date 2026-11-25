using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        private T17Button rebind;
        private KeyCapture keyCapture;
        private ShortcutWarning shortcutWarning;
        private void BuildKeyCode()
        {
            T17Button choices = Button("KeyList", "", 0, .65f, OpenChoices);
            valueLabel = choices.GetComponentInChildren<T17Text>(true);
            rebind = Button("Rebind", Texts.Rebind, .66f, .80f, BeginKeyCapture);
            shortcutWarning = ShortcutWarning.Create(page, Row, choices.transform, valueLabel, choices, rebind);
        }
        private void BuildKeyboardShortcut()
        {
            T17Button capture = Button("Capture", Texts.Rebind, 0, .80f,
                delegate { page.Overlay.Capture(delegate { return Row.DisplayName; }, true, delegate(object value) { Commit(value); }); });
            RectTransform holder = NativeUi.Rect(Root, "ShortcutValue", Vector2.zero, Vector2.one);
            valueLabel = NativeUi.Text(page.TextTemplate, holder, "Text", "", 22);
            valueLabel.color = Color.white;
            shortcutWarning = ShortcutWarning.Create(page, Row, holder, valueLabel, capture);
        }
        internal void RefreshShortcutWarning()
        { if (shortcutWarning != null) shortcutWarning.Show(page.HasShortcutConflict(Row)); }
        private void BeginKeyCapture()
        {
            page.Overlay.Close(); CancelKeyCapture();
            keyCapture = Root.gameObject.AddComponent<KeyCapture>();
            keyCapture.Initialize(page, KeyCapture.Mode.Single,
                delegate(object value) { CancelKeyCapture(); Commit(value); },
                delegate { CancelKeyCapture(); page.CaptureCancelled(); });
            RefreshKeyCapture(); page.InlineCaptureChanged(keyCapture);
        }
        private void CancelKeyCapture()
        {
            KeyCapture old = keyCapture; keyCapture = null;
            if (old == null) return;
            old.Stop(); RefreshKeyCapture(); page.InlineCaptureChanged(null);
        }
        private void RefreshKeyCapture()
        { if (rebind != null) NativeUi.Label(rebind.transform, keyCapture == null ? Texts.Rebind : Texts.PressKey); }
    }
}
