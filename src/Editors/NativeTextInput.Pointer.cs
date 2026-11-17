using UnityEngine;
using UnityEngine.EventSystems;

namespace OC2ModSettings
{
    internal sealed partial class NativeTextInput
    {
        private bool dragging;
        public override void OnPointerDown(PointerEventData data)
        {
            base.OnPointerDown(data);
            if (data.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable()) return;
            bool extend = IsEditing && ShiftHeld;
            Begin(); caret = IndexAt(data);
            if (!extend) anchor = caret;
            dragging = true; RefreshLabel();
        }
        public override void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || !IsActive() || !IsInteractable()) return;
            // The press already placed the caret. A click following a drag must not erase its range.
            Begin(); data.Use();
        }
        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold = false; }
        public void OnBeginDrag(PointerEventData data)
        { if (dragging && data.button == PointerEventData.InputButton.Left) data.Use(); }
        public void OnDrag(PointerEventData data)
        {
            if (!dragging || !IsEditing || data.button != PointerEventData.InputButton.Left) return;
            caret = IndexAt(data); RefreshLabel(); data.Use();
        }
        public void OnEndDrag(PointerEventData data) { dragging = false; }
        private int IndexAt(PointerEventData data)
        {
            Vector2 position;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(label.rectTransform, data.position, data.pressEventCamera, out position)) return caret;
            TextGenerator generator = label.cachedTextGenerator;
            generator.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
            if (generator.lineCount == 0 || generator.characters.Count == 0) return 0;
            float scale = label.pixelsPerUnit;
            int line = 0;
            for (int i = 1; i < generator.lineCount; i++)
                if (position.y * scale < generator.lines[i].topY) line = i;
            int start = generator.lines[line].startCharIdx;
            int end = line + 1 < generator.lineCount ? generator.lines[line + 1].startCharIdx : generator.characterCountVisible;
            int index = end;
            for (int i = start; i < end && i < generator.characters.Count; i++)
            {
                UICharInfo glyph = generator.characters[i];
                if (position.x * scale < glyph.cursorPos.x + glyph.charWidth * .5f) { index = i; break; }
            }
            int composition = Input.compositionString.Length;
            if (index > caret) index = Mathf.Max(caret, index - composition);
            index = Mathf.Clamp(index, 0, draft.Length);
            return index > 0 && index < draft.Length && char.IsLowSurrogate(draft[index]) ? index - 1 : index;
        }
    }
}
