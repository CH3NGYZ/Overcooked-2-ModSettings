using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace OC2ModSettings
{
    // Values and their display names are sampled together. Translated labels never become config values.
    internal sealed class ChoiceItems
    {
        internal string Title;
        internal object[] Values;
        internal string[] Names;
        private static readonly Dictionary<Type, PropertyInfo> properties = new Dictionary<Type, PropertyInfo>();
        internal static object[] ValuesFor(ConfigEntryBase entry)
        {
            AcceptableValueBase acceptable = entry.Description.AcceptableValues;
            Array values = null;
            if (entry.SettingType == typeof(KeyCode)) values = Enum.GetValues(typeof(KeyCode));
            else if (acceptable != null)
            {
                PropertyInfo property;
                if (!properties.TryGetValue(acceptable.GetType(), out property))
                { property = acceptable.GetType().GetProperty("AcceptableValues"); properties[acceptable.GetType()] = property; }
                if (property != null) values = property.GetValue(acceptable, null) as Array;
            }
            if (values == null && entry.SettingType.IsEnum) values = Enum.GetValues(entry.SettingType);
            if (values == null) return null;
            List<object> result = new List<object>();
            foreach (object value in values)
            {
                if (entry.SettingType == typeof(KeyCode) && (result.Contains(value) || (acceptable != null && !acceptable.IsValid(value)))) continue;
                result.Add(value);
            }
            return result.ToArray();
        }
        internal static ChoiceItems Read(SettingRow row)
        {
            object[] values = ValuesFor(row.Entry);
            if (values == null) return null;
            ChoiceItems result = new ChoiceItems { Title = row.DisplayName, Values = values, Names = new string[values.Length] };
            bool flags = row.Entry.SettingType.IsEnum && row.Entry.SettingType.IsDefined(typeof(FlagsAttribute), false);
            for (int i = 0; i < values.Length; i++)
                result.Names[i] = (flags && EnumBits.Contains(row.Entry.BoxedValue, values[i]) ? "✓ " : "") + ValueCodec.Format(row, values[i]);
            return result;
        }
        internal bool SameValuesAs(ChoiceItems other)
        {
            if (other == null || Values.Length != other.Values.Length) return false;
            for (int i = 0; i < Values.Length; i++)
                if (!Equals(Values[i], other.Values[i])) return false;
            return true;
        }
    }
}
