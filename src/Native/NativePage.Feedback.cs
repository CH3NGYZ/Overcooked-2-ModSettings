using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private SettingsRowFeedback hoveredRow, litRow;
        private SettingsControlFeedback hoveredControl, litControl;
        private ShortcutWarning hoveredWarning;
        private NativeTextInput invalidInput;
        private KeyCapture inlineKeyCapture;
        private ConfigEntryBase failedShortcut;
        private string feedbackError;
        private static readonly Color ErrorColor = new Color(.7f, .08f, .05f);
        private static readonly Color WarningColor = new Color32(68, 44, 12, 255);
        internal void InputValidationChanged(NativeTextInput input)
        {
            if (input.ValidationLabel != null)
            {
                NativeUi.Label(input.ValidationLabel.transform, input.ValidationMessage);
                input.ValidationLabel.color = ErrorColor;
            }
            if (input.IsEditing && input.Invalid && input.ValidationLabel == null) invalidInput = input;
            else if (invalidInput == input) invalidInput = null;
            RefreshFeedback();
        }
        internal void PointerFeedback(PointerEventData data, GameObject fallback)
        {
            GameObject hit = data.pointerCurrentRaycast.gameObject;
            if (hit == null) hit = fallback;
            hoveredWarning = hit == null ? null : hit.GetComponentInParent<ShortcutWarning>();
            hoveredControl = hit == null ? null : hit.GetComponentInParent<SettingsControlFeedback>();
            hoveredRow = hit == null ? null : hit.GetComponentInParent<SettingsRowFeedback>();
            if (hoveredControl != null && hoveredControl.Page != this) hoveredControl = null;
            if (hoveredRow != null && hoveredRow.Page != this) hoveredRow = null;
            RefreshFeedback();
        }
        internal void KeyboardFeedback() { hoveredRow = null; hoveredControl = null; hoveredWarning = null; RefreshFeedback(); }
        internal void InlineCaptureChanged(KeyCapture capture)
        {
            inlineKeyCapture = capture;
            if (capture != null) ClearFeedbackError();
            RefreshFeedback();
        }
        internal void ControlSelected(SettingsControlFeedback control)
        {
            if (failedShortcut != null)
            {
                SettingsRowFeedback owner = control.GetComponentInParent<SettingsRowFeedback>();
                SettingRow row = control.ShortcutRow ?? (owner == null ? null : owner.Row);
                if (row == null || row.Entry != failedShortcut) ClearFeedbackError();
            }
            RefreshFeedback();
        }
        internal void ForgetFeedback(Component target)
        {
            if (litRow == target) litRow.Paint(false);
            if (litControl == target) litControl.Paint(false);
            if (hoveredRow == target) hoveredRow = null;
            if (hoveredControl == target) hoveredControl = null;
            if (hoveredWarning == target) hoveredWarning = null;
            if (litRow == target) litRow = null;
            if (litControl == target) litControl = null;
        }
        internal void RefreshFeedback()
        {
            SettingsControlFeedback control = hoveredControl;
            SettingsRowFeedback row = hoveredRow;
            bool byKeyboard = control == null && row == null && hoveredWarning == null;
            if (byKeyboard && Domain != null && Domain.currentSelectedGameObject != null)
            {
                control = Domain.currentSelectedGameObject.GetComponent<SettingsControlFeedback>();
                if (control != null && control.Page != this) control = null;
            }
            if (control != null) row = control.GetComponentInParent<SettingsRowFeedback>();
            if (litControl != null && litControl != control) litControl.Paint(false);
            if (litRow != null) litRow.Paint(false);
            litControl = control; litRow = control == null ? row : null;
            if (litControl != null) litControl.Paint(true);
            if (litRow != null) litRow.Paint(true);
            if (help == null) return;
            if (failedShortcut == null) feedbackError = null;
            if (invalidInput != null && invalidInput.IsEditing && invalidInput.Invalid) SetHelp(invalidInput.ValidationMessage, ErrorColor);
            else if (feedbackError != null) SetHelp(feedbackError, ErrorColor);
            else if (IsRecordingToggleKey || (inlineKeyCapture != null && inlineKeyCapture.isActiveAndEnabled))
                SetHelp(Texts.PressKey + "   ·   Esc: " + Texts.Cancel, Color.white);
            else
            {
                SettingRow shortcut = hoveredWarning != null ? hoveredWarning.Row : byKeyboard && control != null ? control.ShortcutRow : null;
                string detail = ShortcutDetail(shortcut);
                string description = null;
                if (row != null)
                {
                    if (control != null && control.Action != null) description = row.Row.GetActionDescription(control.Action);
                    if (description == null) description = row.Row.DisplayDescription;
                }
                int count = detail == null && string.IsNullOrEmpty(description) && shortcutConflicts != null ? shortcutConflicts.Count(active) : 0;
                if (detail != null) SetHelp(detail, WarningColor);
                else if (count > 0) SetHelp(Texts.ShortcutDuplicateCount(count), WarningColor);
                else SetHelp(description, Color.white);
            }
        }

        private void SetHelp(string message, Color color)
        {
            string text = message ?? "";
            if (help.text != text) NativeUi.Label(help.transform, text);
            if (help.color != color) help.color = color;
        }
        private void ClearFeedbackError() { failedShortcut = null; feedbackError = null; }
        internal void ClearError() { ClearFeedbackError(); RefreshFeedback(); }
        internal void ShowError(string error) { failedShortcut = null; feedbackError = error; SetHelp(error, ErrorColor); }
        internal void ShowCommitError(SettingRow row, string error)
        {
            ShowError(error);
            if (ShortcutConflicts.Tracks(row.Entry)) failedShortcut = row.Entry;
        }
    }
}
