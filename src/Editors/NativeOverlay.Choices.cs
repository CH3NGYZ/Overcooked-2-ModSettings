using System;
using BepInEx.Configuration;
using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class NativeOverlay
    {
        internal sealed class ChoiceState
        {
            internal ConfigEntryBase Entry;
            internal object Focus;
            internal float Scroll;
        }
        private SettingRow choiceRow;
        private ChoiceItems choiceItems;
        private Action<object> choiceSelected;
        private ScrollColumn choiceColumn;
        private void CreateChoices(RectTransform root)
        {
            choiceColumn = new ScrollColumn(root, "Choices", new Vector2(.04f, .08f), new Vector2(.96f, .86f));
            choiceColumn.AddScrollbar(page.ScrollbarTemplate, page.Palette.Normal); choiceColumn.Bar.SetEventSystem(page.Domain);
        }
        private void PopulateChoices(string[] names, Action<int> selected)
        {
            buttons.Clear(); NativeUi.ClearChildren(choiceColumn.Content);
            float width = Mathf.Max(300, choiceColumn.Viewport.rect.width);
            for (int i = 0; i < names.Length; i++)
            {
                int index = i;
                T17Button button = NativeUi.CloneButton(page.ButtonTemplate, choiceColumn.Content, "Choice_" + i, names[i],
                    delegate { Close(); selected(index); }, page);
                NativeUi.Box(button.GetComponent<RectTransform>(), 2, i * 48, width - 4, 44);
                button.SetEventSystem(page.Domain); buttons.Add(button);
                RowFocus focus = button.gameObject.AddComponent<RowFocus>(); focus.Column = choiceColumn; focus.Row = button.GetComponent<RectTransform>();
            }
            choiceColumn.Height(names.Length * 48); BindChoices();
        }
        internal void Choose(SettingRow row, GameObject invoker, Action<object> selected)
        {
            ChoiceItems items = ChoiceItems.Read(row); if (items == null) return;
            RectTransform root = Begin(items.Title);
            previous = invoker;
            choiceRow = row; choiceSelected = selected; CreateChoices(root); ApplyChoices(items);
            // Key lists have always opened at the current key. Other dropdowns keep their first item focus.
            if (row.Entry.SettingType == typeof(KeyCode)) FocusChoice(Array.IndexOf(items.Values, row.Entry.BoxedValue));
        }
        private void ApplyChoices(ChoiceItems items)
        {
            Action<object> selected = choiceSelected;
            NativeUi.Label(titleLabel.transform, items.Title); choiceItems = items;
            // Capture this snapshot, never an index into a later translated/reordered collection.
            PopulateChoices(items.Names, delegate(int index) { selected(items.Values[index]); });
        }
        internal ChoiceState RememberChoice()
        {
            if (!IsOpen || choiceRow == null) return null;
            object focus = choiceRow.Entry.BoxedValue;
            for (int i = 0; i < buttons.Count; i++)
                if (page.Domain.currentSelectedGameObject == buttons[i].gameObject) { focus = choiceItems.Values[i]; break; }
            return new ChoiceState { Entry = choiceRow.Entry, Focus = focus, Scroll = choiceColumn.Content.anchoredPosition.y };
        }
        internal void RestoreChoice(ChoiceState state)
        {
            if (state == null || choiceItems == null) return;
            FocusChoice(Array.IndexOf(choiceItems.Values, state.Focus));
            choiceColumn.Content.anchoredPosition = new Vector2(0, Mathf.Clamp(state.Scroll, 0,
                Mathf.Max(0, choiceColumn.Content.rect.height - choiceColumn.Viewport.rect.height)));
        }
        internal void RefreshChoices()
        {
            if (choiceRow == null) return;
            ChoiceItems items;
            try { items = ChoiceItems.Read(choiceRow); }
            catch (Exception error) { Close(); page.ShowError(error.Message); return; }
            if (items == null) { Close(); return; }
            if (items.SameValuesAs(choiceItems))
            {
                if (items.Title != choiceItems.Title) NativeUi.Label(titleLabel.transform, items.Title);
                for (int index = 0; index < items.Names.Length; index++)
                    if (items.Names[index] != choiceItems.Names[index]) NativeUi.Label(buttons[index].transform, items.Names[index]);
                choiceItems = items;
                return;
            }
            ChoiceState state = RememberChoice(); ApplyChoices(items); RestoreChoice(state);
        }
        internal void ChooseDefaults(Action<int> selected)
        {
            Choose(Texts.Defaults, new string[] { Texts.Confirm }, selected);
            localizedTitle = delegate { return Texts.Defaults; };
        }
    }
}
