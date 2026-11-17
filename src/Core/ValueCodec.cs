using System;
using System.Globalization;
using BepInEx.Configuration;
using UnityEngine;

namespace OC2ModSettings
{
    internal static class ValueCodec
    {
        internal static bool Numeric(Type type)
        {
            TypeCode c = Type.GetTypeCode(type);
            return !type.IsEnum && c >= TypeCode.SByte && c <= TypeCode.Decimal;
        }
        internal static string Format(SettingRow row, object value)
        {
            if (row.Metadata.Format != null) return row.Metadata.Format(value);
            if (value is string) return (string)value;
            if (value is float) return ((float)value).ToString("R", CultureInfo.InvariantCulture);
            if (value is double) return ((double)value).ToString("R", CultureInfo.InvariantCulture);
            if (Numeric(row.Entry.SettingType)) return Convert.ToString(value, CultureInfo.InvariantCulture);
            return TomlTypeConverter.CanConvert(row.Entry.SettingType)
                ? TomlTypeConverter.ConvertToString(value, row.Entry.SettingType) : Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        internal static bool Editable(SettingRow row)
        {
            return row.Entry != null && !row.Metadata.ReadOnly && ((row.Metadata.Parse != null && row.Metadata.Format != null)
                || TomlTypeConverter.CanConvert(row.Entry.SettingType));
        }
        internal static object Parse(SettingRow row, string text)
        {
            Type type = row.Entry.SettingType;
            object value;
            if (row.Metadata.Parse != null) value = row.Metadata.Parse(text);
            else if (type == typeof(string)) value = text;
            else if (Numeric(type)) value = Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
            else if (type.IsEnum) value = Enum.Parse(type, text, true);
            else value = TomlTypeConverter.ConvertToValue(text, type);
            if (value == null || !type.IsInstanceOfType(value)) throw new FormatException("Converter returned an incompatible value.");
            if ((value is float && (float.IsNaN((float)value) || float.IsInfinity((float)value)))
                || (value is double && (double.IsNaN((double)value) || double.IsInfinity((double)value))))
                throw new FormatException("A finite number is required.");
            AcceptableValueBase limits = row.Entry.Description.AcceptableValues;
            if (limits != null && !limits.IsValid(value)) throw new ArgumentOutOfRangeException("text", "Value is outside the allowed range or list.");
            return value;
        }
    }

    internal sealed class CommitResult
    {
        internal bool Success;
        internal object ActualValue;
        internal string Error;
    }

    internal static class ConfigCommit
    {
        internal static CommitResult Apply(SettingRow row, object value)
        {
            CommitResult result = new CommitResult();
            try
            {
                if (row.Entry == null || row.Metadata.ReadOnly) throw new InvalidOperationException("This row is not an editable setting.");
                if (value == null || !row.Entry.SettingType.IsInstanceOfType(value)) throw new ArgumentException("Incompatible value.");
                if ((value is float && (float.IsNaN((float)value) || float.IsInfinity((float)value)))
                    || (value is double && (double.IsNaN((double)value) || double.IsInfinity((double)value))))
                    throw new ArgumentException("A finite number is required.");
                AcceptableValueBase acceptable = row.Entry.Description.AcceptableValues;
                if (acceptable != null && !acceptable.IsValid(value)) throw new ArgumentOutOfRangeException("value");
                bool changed = !Equals(row.Entry.BoxedValue, value);
                row.Entry.BoxedValue = value;
                if (!row.Entry.ConfigFile.SaveOnConfigSet || !changed) row.Entry.ConfigFile.Save();
                result.Success = true;
            }
            catch (Exception error) { result.Error = error.Message; }
            // An autosave failure may follow mutation. Never claim that the old value was restored.
            try { result.ActualValue = row.Entry == null ? null : row.Entry.BoxedValue; }
            catch (Exception error) { result.Success = false; result.Error = error.Message; }
            return result;
        }
    }
}
