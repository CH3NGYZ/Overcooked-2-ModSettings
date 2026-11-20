using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // T17 owns selection. Valid changes persist immediately; invalid intermediate text stays editable.
    internal sealed partial class NativeTextInput : T17Button, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        internal string Value;
        internal bool IsEditing;
        internal bool HasPending;
        internal Func<string, bool> Changed;
        internal bool Invalid;
        internal string ValidationError;
        internal T17Text ValidationLabel;
        internal string ValidationMessage { get { return IsEditing && Invalid ? ValidationError ?? Texts.InvalidValue : ""; } }
        private NativePage page;
        private int started;
        private string draft, placeholder;
        private T17Text label;
        private int caret, anchor;
        private TextSelectionGraphic selection;
        private string observed;
        private IMECompositionMode previousIme;
        private static readonly KeyCode[] ClipboardKeys = { KeyCode.A, KeyCode.C, KeyCode.X, KeyCode.V };
        private static readonly KeyCode[] EditKeys = { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Home, KeyCode.End, KeyCode.Delete };
        internal static NativeTextInput Create(NativePage page, Transform parent, string name, string value, string hint)
        {
            // Reuse the same button hierarchy and theme for fields and the fixed search input.
            T17Button template = NativeUi.CloneButton(page.ButtonTemplate, parent, name, "", delegate { });
            RectTransform rect = template.GetComponent<RectTransform>();
            Graphic graphic = template.targetGraphic; ColorBlock colors = template.colors;
            T17Text text = template.GetComponentInChildren<T17Text>(true);
            UnityEngine.Object.DestroyImmediate(template);
            NativeTextInput input = rect.gameObject.AddComponent<NativeTextInput>();
            input.targetGraphic = graphic; input.colors = colors; input.transition = Transition.ColorTint;
            input.Value = value; input.placeholder = hint; input.page = page;
            input.label = text == null ? NativeUi.Text(page.TextTemplate, rect, "Text", "", 22) : text;
            input.label.name = "Text"; input.label.alignment = TextAnchor.MiddleLeft;
            input.label.resizeTextForBestFit = true; input.label.resizeTextMinSize = 12; input.label.resizeTextMaxSize = 22;
            input.label.color = Color.white;
            input.label.supportRichText = false;
            input.SetEventSystem(page.Domain); input.onClick.AddListener(input.Begin);
            NativeUi.ThemeControl(input, page);
            RectTransform selectionRect = NativeUi.Rect(input.label.transform, "Selection", Vector2.zero, Vector2.one);
            input.selection = selectionRect.gameObject.AddComponent<TextSelectionGraphic>();
            input.selection.Owner = input; input.selection.Label = input.label; input.selection.raycastTarget = false;
            input.RefreshLabel(); return input;
        }
        internal void SetValue(string value) { Value = value; if (!HasPending) RefreshLabel(); }
        internal void SetPlaceholder(string value) { placeholder = value; RefreshLabel(); }
        internal void RefreshLanguage() { RefreshLabel(); if (Invalid) page.InputValidationChanged(this); }
        internal void Begin()
        {
            if (IsEditing) return;
            IsEditing = HasPending = true; started = Time.frameCount; draft = observed = Value ?? ""; caret = anchor = draft.Length;
            previousIme = Input.imeCompositionMode; Input.imeCompositionMode = IMECompositionMode.On; RefreshLabel();
        }
        internal void Confirm()
        {
            if (!HasPending) return;
            PublishChange(); if (Invalid) return;
            EndInput(); HasPending = false; RefreshLabel();
        }
        internal void Cancel() { EndInput(); HasPending = Invalid = false; draft = null; RefreshLabel(); }
        internal void ChangeText(string value)
        { draft = value ?? ""; caret = Mathf.Min(caret, draft.Length); anchor = Mathf.Min(anchor, draft.Length); PublishChange(); RefreshLabel(); }
        private void PublishChange()
        {
            if (Changed == null || draft == observed) return;
            observed = draft; ValidationError = null; Invalid = !Changed(draft);
            if (page != null) page.InputValidationChanged(this);
        }
        private void EndInput()
        {
            if (IsEditing) Input.imeCompositionMode = previousIme;
            IsEditing = false; dragging = false;
            if (page != null) page.InputValidationChanged(this);
        }
        protected override void OnDisable() { EndInput(); base.OnDisable(); }
        private void Update()
        {
            if (!IsEditing || Time.frameCount <= started) return;
            if (page.Shortcut.Pressed) return;
            // The global page consumes cancel on release through its ordered Cancel handler.
            // Cancelling here on key-down would consume the draft twice across two frames.
            if (Input.GetKeyDown(KeyCode.Escape)) return;
            bool command = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            if (command)
            {
                foreach (KeyCode key in ClipboardKeys)
                    if (Input.GetKeyDown(key)) EditKey(key, true, ShiftHeld);
            }
            foreach (KeyCode key in EditKeys)
                if (Input.GetKeyDown(key)) EditKey(key, command, ShiftHeld);
            if (command) return;
            TypeText(Input.inputString);
            RefreshLabel();
        }
        internal void TypeText(string chars)
        {
            for (int i = 0; i < chars.Length; )
            {
                char c = chars[i++];
                if (c == '\b') EditKey(KeyCode.Backspace, false, false);
                else if (c == '\r' || c == '\n') { Confirm(); return; }
                else if (!char.IsControl(c))
                {
                    int start = i - 1;
                    while (i < chars.Length && !char.IsControl(chars[i])) i++;
                    // A committed IME/inputString batch is one edit, including UTF16 pairs.
                    InsertText(chars.Substring(start, i - start));
                }
            }
        }
        internal int SelectionStart { get { return Mathf.Min(caret, anchor); } }
        internal int SelectionEnd { get { return Mathf.Max(caret, anchor); } }
        internal int Caret { get { return caret; } }
        private static bool ShiftHeld { get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); } }
        internal void InsertText(string value)
        {
            int start = SelectionStart;
            draft = draft.Remove(start, SelectionEnd - start).Insert(start, value);
            caret = anchor = start + value.Length; PublishChange(); RefreshLabel();
        }
        internal void EditKey(KeyCode key, bool command, bool shift)
        {
            if (!IsEditing) return;
            if (command && key == KeyCode.A) { anchor = 0; caret = draft.Length; }
            else if (command && (key == KeyCode.C || key == KeyCode.X))
            {
                if (SelectionEnd > SelectionStart)
                {
                    GUIUtility.systemCopyBuffer = draft.Substring(SelectionStart, SelectionEnd - SelectionStart);
                    if (key == KeyCode.X) InsertText("");
                }
            }
            else if (command && key == KeyCode.V) InsertText(GUIUtility.systemCopyBuffer ?? "");
            else if (key == KeyCode.Backspace || key == KeyCode.Delete)
            {
                if (caret == anchor) anchor = key == KeyCode.Backspace ? Previous(caret) : Next(caret);
                InsertText("");
            }
            else
            {
                int next = caret;
                if (key == KeyCode.Home) next = 0;
                else if (key == KeyCode.End) next = draft.Length;
                else if (key == KeyCode.LeftArrow) next = !shift && caret != anchor ? SelectionStart : Previous(caret);
                else if (key == KeyCode.RightArrow) next = !shift && caret != anchor ? SelectionEnd : Next(caret);
                caret = next; if (!shift) anchor = caret;
            }
            RefreshLabel();
        }
        private int Previous(int index)
        { return index > 1 && char.IsLowSurrogate(draft[index - 1]) && char.IsHighSurrogate(draft[index - 2]) ? index - 2 : Mathf.Max(0, index - 1); }
        private int Next(int index)
        { return index + 1 < draft.Length && char.IsHighSurrogate(draft[index]) && char.IsLowSurrogate(draft[index + 1]) ? index + 2 : Mathf.Min(draft.Length, index + 1); }
        public override void OnDeselect(BaseEventData data)
        { Cancel(); base.OnDeselect(data); }
        public override void OnSubmit(BaseEventData data)
        {
            // OC2 maps keyboard Space to native UI select. Spaces inside a draft are text.
            if (IsEditing && Input.GetKey(KeyCode.Space)) { data.Use(); return; }
            if (IsEditing) Confirm(); else base.OnSubmit(data);
        }
        public override void OnMove(AxisEventData data) { if (IsEditing) { data.Use(); return; } base.OnMove(data); }
        private void RefreshLabel()
        {
            if (label == null) return;
            string value = IsEditing ? draft.Insert(caret, Input.compositionString) : (string.IsNullOrEmpty(Value) ? placeholder : Value);
            label.color = Invalid ? new Color(1, .55f, .46f) : new Color(.94f, .97f, 1);
            label.m_NonLocalizedKeyboardText = ""; label.SetNonLocalizedText(value ?? "");
            if (selection != null) selection.SetVerticesDirty();
        }
    }
}
