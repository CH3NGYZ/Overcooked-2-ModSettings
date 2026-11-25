using System;
using System.Collections.Generic;
using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        private readonly List<T17Button> actionButtons = new List<T17Button>();
        private readonly HashSet<RegisteredAction> actionErrors = new HashSet<RegisteredAction>();
        private void BuildActions()
        {
            foreach (RegisteredAction action in Row.Actions) BuildAction(action);
            if (Row.Action != null) BuildAction(Row.Action);
        }
        private void BuildAction(RegisteredAction action)
        {
            T17Button button = Button("Action_" + action.Id, Row.GetActionCaption(action), 0, 1, delegate { RunAction(action); });
            button.GetComponent<SettingsControlFeedback>().Action = action;
            actionButtons.Add(button);
        }
        private bool ActionEnabled(RegisteredAction action)
        {
            RegisteredAction current;
            return page.Catalog.Actions.TryGet(action.Key, out current) && ReferenceEquals(current, action)
                && (action.CanExecute == null || action.CanExecute());
        }
        internal void RefreshActions()
        {
            for (int i = 0; i < actionButtons.Count; i++)
                try
                {
                    RegisteredAction action = Row.Action ?? Row.Actions[i];
                    actionButtons[i].interactable = ActionEnabled(action); actionErrors.Remove(action);
                }
                catch (Exception error)
                {
                    actionButtons[i].interactable = false;
                    RegisteredAction action = Row.Action ?? Row.Actions[i];
                    if (actionErrors.Add(action)) { page.ShowError(Row.GetActionName(action) + ": " + error.Message); ModSettingsPlugin.Instance.Report("Action.CanExecute", error); }
                }
                finally
                {
                    SettingsControlFeedback feedback = actionButtons[i].GetComponent<SettingsControlFeedback>();
                    if (feedback != null) feedback.RefreshState();
                }
        }
        private void ArrangeActions(float top, float baseHeight, int inline)
        {
            float gap = ActionGap, unit = ActionUnit;
            Transform defaultButton = Root.Find("Default");
            if (actionButtons.Count > 0 && defaultButton != null)
            {
                Controls.Remove(actionButtons[0]);
                Controls.Insert(Controls.IndexOf(defaultButton.GetComponent<T17Button>()) + (inline > 0 ? 0 : 1), actionButtons[0]);
            }
            for (int i = 0; i < actionButtons.Count; i++)
            {
                float x = Row.Action != null ? 1 - unit : i < inline
                    ? 1 - (defaultButton == null ? 0 : unit + gap) - unit
                    : 1 - unit - (2 - (i - inline) % 3) * (unit + gap);
                float y = Row.Action != null || i < inline ? top : baseHeight + ((i - inline) / 3) * 54;
                Place(actionButtons[i].name, x, x + unit, y, 48);
            }
            if (Row.Action == null && actionButtons.Count > inline)
                Height = baseHeight + ((actionButtons.Count - inline + 2) / 3) * 54;
        }
        private void RunAction(RegisteredAction action)
        {
            try
            {
                if (!ActionEnabled(action)) { RefreshActions(); return; }
                if (action.ClosePage) page.Hide(true, false);
                action.Execute();
            }
            catch (Exception error)
            { page.ShowError(Row.GetActionName(action) + ": " + error.Message); ModSettingsPlugin.Instance.Report("Action " + action.Key, error); }
        }
    }
}
