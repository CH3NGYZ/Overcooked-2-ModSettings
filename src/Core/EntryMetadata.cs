using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;

namespace OC2ModSettings
{
    internal sealed class EntryMetadata
    {
        internal string Name, Category, Description;
        internal bool Hidden, Advanced, ReadOnly, Percent, HideDefault;
        internal int Order;
        internal int? CategoryOrder;
        internal Func<object, string> Format;
        internal Func<string, object> Parse;
        internal bool SameAs(EntryMetadata other)
        {
            return Name == other.Name && Category == other.Category && Description == other.Description
                && Hidden == other.Hidden && Advanced == other.Advanced && ReadOnly == other.ReadOnly
                && Percent == other.Percent && HideDefault == other.HideDefault && Order == other.Order && CategoryOrder == other.CategoryOrder
                && Equals(Format, other.Format) && Equals(Parse, other.Parse);
        }

        internal static EntryMetadata Read(ConfigEntryBase entry, IDictionary<string, object> overrides)
        {
            EntryMetadata m = new EntryMetadata { Name = entry.Definition.Key,
                Category = entry.Definition.Section, Description = entry.Description.Description };
            // Source SDK types may live in any consumer assembly. Never require their CLR identity.
            ApplyTags(m, entry.Description.Tags, "ConfigurationManagerAttributes");
            ApplyTags(m, entry.Description.Tags, "ModSettingsAttributes");
            if (overrides != null) foreach (KeyValuePair<string, object> pair in overrides) m.Apply(pair.Key, pair.Value);
            return m;
        }
        private static void ApplyTags(EntryMetadata metadata, object[] tags, string name)
        {
            foreach (object tag in tags)
            {
                if (tag == null || tag.GetType().Name != name) continue;
                foreach (FieldInfo field in tag.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
                    metadata.Apply(field.Name, field.GetValue(tag));
            }
        }
        private void Apply(string key, object value)
        {
            if (value == null) return;
            switch (key)
            {
                case "DispName": Name = (string)value; break;
                case "Category": Category = (string)value; break;
                case "Description": Description = (string)value; break;
                case "Browsable": Hidden = !(bool)value; break;
                case "IsAdvanced": Advanced = (bool)value; break;
                case "ReadOnly": ReadOnly = (bool)value; break;
                case "ShowRangeAsPercent": Percent = (bool)value; break;
                case "HideDefaultButton": HideDefault = (bool)value; break;
                case "Order": Order = (int)value; break;
                case "CategoryOrder": CategoryOrder = (int)value; break;
                case "ObjToStr": Format = value as Func<object, string>; break;
                case "StrToObj": Parse = value as Func<string, object>; break;
            }
        }
    }

    internal sealed class SettingRow
    {
        internal string Owner;
        internal ConfigEntryBase Entry;
        internal EntryMetadata Metadata;
        internal RegisteredAction Action;
        internal readonly List<RegisteredAction> Actions = new List<RegisteredAction>();
        internal string Identity { get { return Entry == null ? "@Action\n" + Action.Id : Entry.Definition.Section + "\n" + Entry.Definition.Key; } }
        internal bool Matches(string query, bool advanced)
        {
            if (Metadata.Hidden || (!advanced && Metadata.Advanced)) return false;
            if (string.IsNullOrEmpty(query) || (Metadata.Name + "\n" + Metadata.Category + "\n" + Metadata.Description)
                .IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) return true;
            foreach (RegisteredAction action in Actions)
                if ((action.Name + "\n" + action.Description).IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0) return true;
            return false;
        }
    }

    internal sealed class ModConfig
    {
        internal string Guid, Name, Description;
        internal readonly List<SettingRow> Rows = new List<SettingRow>();
    }
}
