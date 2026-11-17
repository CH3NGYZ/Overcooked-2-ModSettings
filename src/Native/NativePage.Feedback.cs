using UnityEngine;
using UnityEngine.EventSystems;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private SettingsRowFeedback hoveredRow, litRow;
        private SettingsControlFeedback hoveredControl, litControl;
        private NativeTextInput invalidInput;
        internal void InputValidationChanged(NativeTextInput input)
        {
            if (input.ValidationLabel != null)
            {
                NativeUi.Label(input.ValidationLabel.transform, input.ValidationMessage);
                input.ValidationLabel.color = new Color(.7f, .08f, .05f);
            }
            if (input.IsEditing && input.Invalid && input.ValidationLabel == null) invalidInput = input;
            else if (invalidInput == input) invalidInput = null;
            RefreshFeedback();
        }
        internal void PointerFeedback(PointerEventData data, GameObject fallback)
        {
            GameObject hit = data.pointerCurrentRaycast.gameObject;
            if (hit == null) hit = fallback;
            hoveredControl = hit == null ? null : hit.GetComponentInParent<SettingsControlFeedback>();
            hoveredRow = hit == null ? null : hit.GetComponentInParent<SettingsRowFeedback>();
            if (hoveredControl != null && hoveredControl.Page != this) hoveredControl = null;
            if (hoveredRow != null && hoveredRow.Page != this) hoveredRow = null;
            RefreshFeedback();
        }
        internal void KeyboardFeedback() { hoveredRow = null; hoveredControl = null; RefreshFeedback(); }
        internal void ForgetFeedback(Component target)
        {
            if (litRow == target) litRow.Paint(false);
            if (litControl == target) litControl.Paint(false);
            if (hoveredRow == target) hoveredRow = null;
            if (hoveredControl == target) hoveredControl = null;
            if (litRow == target) litRow = null;
            if (litControl == target) litControl = null;
        }
        internal void RefreshFeedback()
        {
            SettingsControlFeedback control = hoveredControl;
            SettingsRowFeedback row = hoveredRow;
            if (control == null && row == null && Domain != null && Domain.currentSelectedGameObject != null)
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
            if (help != null) ShowHelp(row == null ? null : row.Row.Metadata.Description);
        }
    }
}
