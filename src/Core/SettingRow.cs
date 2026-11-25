using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using OC2ModSettings.SDK;

namespace OC2ModSettings
{
    internal sealed class SettingRow
    {
        internal string Owner;
        internal ConfigEntryBase Entry;
        internal EntryMetadata Metadata;
        internal RegisteredAction Action;
        internal TranslationCatalog Translations;
        internal readonly List<RegisteredAction> Actions = new List<RegisteredAction>();
        internal string Identity { get { return Entry == null ? "@Action\n" + Action.Id : Entry.Definition.Section + "\n" + Entry.Definition.Key; } }
        internal string DisplayName
        {
            get { return Translations == null ? Metadata.Name : Action != null ? GetActionName(Action)
                : Resolve(ModSettingsTranslations.ConfigName(Entry.Definition.Section, Entry.Definition.Key), Metadata.Name, false); }
        }
        internal string DisplayCategory
        {
            get { return Translations == null || Metadata.Category == null ? Metadata.Category
                : Resolve(ModSettingsTranslations.Group(Metadata.Category), Metadata.Category, false); }
        }
        internal string DisplayDescription
        {
            get { return Translations == null ? Metadata.Description : Action != null ? GetActionDescription(Action)
                : Resolve(ModSettingsTranslations.ConfigDescription(Entry.Definition.Section, Entry.Definition.Key), Metadata.Description, true); }
        }
        internal string GetActionName(RegisteredAction action)
        { return Resolve(ModSettingsTranslations.ActionTitle(action.Id), action.Name, false); }
        internal string GetActionCaption(RegisteredAction action)
        {
            return Resolve(ModSettingsTranslations.ActionLabel(action.Id), null, false)
                ?? (string.IsNullOrEmpty(action.ButtonLabel) ? GetActionName(action) : action.ButtonLabel);
        }
        internal string GetActionDescription(RegisteredAction action)
        { return Resolve(ModSettingsTranslations.ActionDescription(action.Id), action.Description, true); }
        private string Resolve(string key, string fallback, bool allowEmpty)
        { return Translations == null ? fallback : Translations.Resolve(Owner, key, fallback, allowEmpty); }
        internal bool Matches(string query, bool advanced)
        {
            if (Metadata.Hidden || (!advanced && Metadata.Advanced)) return false;
            if (string.IsNullOrEmpty(query) || (DisplayName + "\n" + DisplayCategory + "\n" + DisplayDescription)
                .IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) return true;
            foreach (RegisteredAction action in Actions)
                if ((GetActionName(action) + "\n" + GetActionDescription(action)).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) return true;
            return false;
        }
    }

    internal sealed class ModConfig
    {
        internal string Guid, Name, Description;
        internal readonly List<SettingRow> Rows = new List<SettingRow>();
    }
}
