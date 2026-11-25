using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow : IDisposable
    {
        internal SettingRow Row;
        internal RectTransform Root;
        internal float Height = 68;
        internal readonly List<Selectable> Controls = new List<Selectable>();
        internal Selectable First { get { return Controls.Count == 0 ? null : Controls[0]; } }
        private NativePage page;
        private T17Text nameLabel, valueLabel;
        private Image toggleIcon;
        private NativeTextInput text;
        private PreviewSlider slider;
        private IDisposable custom;
        private IDictionary<string, object> renderer;
        private readonly List<NativeTextInput> components = new List<NativeTextInput>();
        private bool refreshing;
        private object preview;

        internal static EditorRow Create(NativePage page, SettingRow row, float width)
        {
            EditorRow editor = new EditorRow { Row = row, page = page };
            editor.Root = NativeUi.Rect(page.Right.Content, "Setting_" + row.Identity.Replace('\n', '_'), Vector2.zero, Vector2.one);
            NativeUi.Backer(editor.Root, new Color(1, 1, 1, .08f)).raycastTarget = false;
            RectTransform name = NativeUi.Rect(editor.Root, "Name", new Vector2(.01f, .08f), new Vector2(.32f, .92f));
            editor.nameLabel = NativeUi.Text(page.TextTemplate, name, "Text", row.DisplayName, 22);
            editor.nameLabel.color = Color.white;
            editor.Root.gameObject.AddComponent<SettingsRowFeedback>().Initialize(page, row);
            if (row.Entry != null)
            {
                if (page.Catalog.Renderers.TryGet(row.Entry, out editor.renderer) && !row.Metadata.ReadOnly) editor.BuildCustom();
                else if (!ValueCodec.Editable(row)) editor.ReadOnly();
                else editor.BuildBuiltin(width);
            }
            if (editor.shortcutWarning == null && ShortcutConflicts.Tracks(row.Entry))
            {
                T17Text keyLabel = editor.valueLabel ?? editor.nameLabel;
                editor.shortcutWarning = ShortcutWarning.Create(page, row, keyLabel.transform.parent, keyLabel, editor.Controls.ToArray());
            }
            editor.BuildActions();
            editor.Arrange();
            foreach (Selectable control in editor.Controls)
            {
                RowFocus focus = control.gameObject.AddComponent<RowFocus>(); focus.Row = editor.Root; focus.Column = page.Right;
            }
            editor.RefreshShortcutWarning(); editor.Refresh(); return editor;
        }
        private T17Button Button(string name, string label, float min, float max, Action click)
        {
            T17Button button = NativeUi.CloneButton(page.ButtonTemplate, Root, name, label, click, page);
            RectTransform rect = button.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(min, .10f); rect.anchorMax = new Vector2(max, .62f);
            rect.offsetMin = new Vector2(3, 2); rect.offsetMax = new Vector2(-3, -2);
            button.SetEventSystem(page.Domain); Controls.Add(button);
            foreach (T17Text labelText in button.GetComponentsInChildren<T17Text>(true))
            { labelText.resizeTextForBestFit = true; labelText.resizeTextMinSize = 12; labelText.resizeTextMaxSize = 22; }
            return button;
        }
        private void ReadOnly()
        {
            valueLabel = NativeUi.Text(page.TextTemplate, NativeUi.Rect(Root, "Value", new Vector2(0, 0), new Vector2(1, .62f)), "Text",
                Convert.ToString(Row.Entry.BoxedValue, CultureInfo.InvariantCulture) + "  " + (Row.Metadata.ReadOnly ? Texts.Locked : Texts.ReadOnly), 18);
        }
        private void BuildBuiltin(float width)
        {
            Type type = Row.Entry.SettingType;
            if (type == typeof(bool))
            {
                T17Button toggle = Button("Toggle", "", 0, .80f, delegate { Commit(!(bool)Row.Entry.BoxedValue); });
                valueLabel = toggle.GetComponentInChildren<T17Text>(true);
                valueLabel.rectTransform.offsetMin = new Vector2(70, 3);
                RectTransform icon = NativeUi.Rect(toggle.transform, "StateIcon", new Vector2(0, .5f), new Vector2(0, .5f));
                icon.sizeDelta = new Vector2(64, 64); icon.anchoredPosition = new Vector2(36, 0);
                toggleIcon = icon.gameObject.AddComponent<Image>(); toggleIcon.preserveAspect = true; toggleIcon.raycastTarget = false;
            }
            else if (type == typeof(KeyCode)) BuildKeyCode();
            else if (type == typeof(KeyboardShortcut)) BuildKeyboardShortcut();
            else if (type.IsEnum || Choices() != null)
            {
                T17Button choice = Button("Dropdown", "", 0, .80f, OpenChoices); valueLabel = choice.GetComponentInChildren<T17Text>(true);
            }
            else if (ComponentEditor.Supports(type)) BuildComponents(width);
            else
            {
                text = NativeTextInput.Create(page, Root, "ExactValue", "", "");
                text.Changed = delegate(string input)
                {
                    object value;
                    try { value = ValueCodec.Parse(Row, input); }
                    catch (Exception) { return false; }
                    text.ValidationError = Commit(value); return text.ValidationError == null;
                };
                RectTransform rect = text.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, .12f); rect.anchorMax = new Vector2(.66f, .62f);
                rect.offsetMin = new Vector2(4, 2); rect.offsetMax = new Vector2(-4, -2); Controls.Add(text);
                BuildRange();
            }
            if (!Row.Metadata.HideDefault) Button("Default", Texts.Defaults, .82f, 1, delegate { CancelPending(); Commit(Row.Entry.DefaultValue); });
        }
        private string Commit(object value)
        {
            CommitResult result = page.CommitSetting(Row, value);
            preview = null; Refresh();
            string error = result.Success ? null : Row.DisplayName + ": " + result.Error;
            if (!result.Success) page.ShowCommitError(Row, error);
            else page.ClearError();
            return error;
        }
        internal void Refresh()
        {
            if (refreshing) return; refreshing = true;
            try
            {
                RefreshActions();
                if (Row.Entry == null) return;
                object value = Row.Entry.BoxedValue;
                RefreshKeyCapture();
                if (toggleIcon != null)
                {
                    toggleIcon.sprite = NativeIcons.Get((bool)value ? "ToggleOn" : "ToggleOff");
                    toggleIcon.color = (bool)value ? new Color(.43f, .94f, .69f) : new Color(.76f, .83f, .87f);
                }
                if (text != null) text.SetValue(ValueCodec.Format(Row, value));
                if (valueLabel != null) NativeUi.Label(valueLabel.transform, (Row.Entry.SettingType == typeof(bool) ? Texts.Boolean((bool)value) : ValueCodec.Format(Row, value))
                    + (!ValueCodec.Editable(Row) ? "  " + (Row.Metadata.ReadOnly ? Texts.Locked : Texts.ReadOnly)
                        : (Row.Entry.SettingType.IsEnum || Choices() != null ? " ▾" : "")));
                if (slider != null && !slider.Pending) { slider.value = ToNormalized(value); slider.Pending = false; RefreshPercent(slider.value); }
                RefreshComponents(value);
                Delegate action; if (customCallbacks.TryGetValue("Refresh", out action)) ((Action)action)();
            }
            catch (Exception e) { page.ShowError(Row.DisplayName + ": " + e.Message); }
            finally { refreshing = false; }
        }
        internal bool RefreshLocalizedText()
        {
            for (int index = 0; index < actionButtons.Count; index++)
                NativeUi.Label(actionButtons[index].transform, Row.GetActionCaption(Row.Action ?? Row.Actions[index]));
            string displayName = Row.DisplayName;
            if (nameLabel.text == displayName) return false;
            NativeUi.Label(nameLabel.transform, displayName);
            Rearrange();
            return true;
        }
        internal void RefreshLanguage()
        {
            foreach (Selectable control in Controls)
            {
                if (control.name == "Default") NativeUi.Label(control.transform, Texts.Defaults);
                else if (control.name == "Capture") NativeUi.Label(control.transform, Texts.Rebind);
                else if (control.name == "PickColor") NativeUi.Label(control.transform, Texts.PickColor);
            }
            Refresh();
        }
        internal bool Owns(GameObject go) { return go != null && (go.transform == Root || go.transform.IsChildOf(Root)); }
        internal bool CancelPending()
        {
            bool pending = false;
            if (keyCapture != null) { CancelKeyCapture(); pending = true; }
            if (text != null && text.HasPending) { text.Cancel(); pending = true; }
            foreach (NativeTextInput component in components) if (component.HasPending) { component.Cancel(); pending = true; }
            if (hex != null && hex.HasPending) { hex.Cancel(); pending = true; }
            if (slider != null && slider.Pending) { slider.Cancel(); pending = true; }
            Delegate cancel;
            try
            {
                Delegate hasPending;
                if (customCallbacks.TryGetValue("HasPending", out hasPending)) pending |= ((Func<bool>)hasPending)();
                if (customCallbacks.TryGetValue("Cancel", out cancel)) ((Action)cancel)();
                pending |= captures.Count > 0;
            }
            catch (Exception error) { page.ShowError(error.Message); }
            foreach (IDisposable capture in captures.ToArray()) capture.Dispose(); captures.Clear();
            return pending;
        }
        public void Dispose()
        {
            try { CancelPending(); if (custom != null) custom.Dispose(); }
            catch (Exception error) { page.ShowError(error.Message); }
            finally { custom = null; customCallbacks.Clear(); foreach (IDisposable capture in captures.ToArray()) capture.Dispose(); captures.Clear(); }
        }
        private object[] Choices()
        {
            return ChoiceItems.ValuesFor(Row.Entry);
        }
        internal void OpenChoices()
        {
            Type type = Row.Entry.SettingType;
            Transform invoker = Root.Find(type == typeof(KeyCode) ? "KeyList" : "Dropdown");
            if (Row.Metadata.ReadOnly || invoker == null || Choices() == null) return;
            bool flags = type.IsEnum && type.IsDefined(typeof(FlagsAttribute), false);
            page.Overlay.Choose(Row, invoker.gameObject, delegate(object value)
            {
                Commit(flags ? EnumBits.Toggle(type, Row.Entry.BoxedValue, value) : value);
                if (flags) OpenChoices();
            });
        }
    }

    internal static class EnumBits
    {
        private static ulong Bits(object value)
        {
            Type underlying = Enum.GetUnderlyingType(value.GetType());
            return underlying == typeof(ulong) ? Convert.ToUInt64(value) : unchecked((ulong)Convert.ToInt64(value));
        }
        internal static bool Contains(object value, object flag) { ulong v = Bits(value), f = Bits(flag); return f == 0 ? v == 0 : (v & f) == f; }
        internal static object Toggle(Type type, object value, object flag)
        { ulong f = Bits(flag); return Enum.ToObject(type, f == 0 ? 0UL : Bits(value) ^ f); }
    }
}
