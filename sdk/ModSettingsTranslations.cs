using System;

namespace OC2ModSettings.SDK
{
    public static class ModSettingsTranslations
    {
        private static readonly string[] Locales = { "en", "fr", "it", "de", "es", "ru", "pt-BR", "pl", "zh-CN", "ja", "ko", "zh-TW" };

        public const string ModName = "mod";
        public static string Group(string category) { return "group/" + Segment(category); }
        public static string ConfigName(string section, string key) { return "config/" + Segment(section) + "/" + Segment(key); }
        public static string ConfigDescription(string section, string key) { return ConfigName(section, key) + "/description"; }
        public static string ActionTitle(string actionId) { return "action/" + Segment(actionId) + "/title"; }
        public static string ActionLabel(string actionId) { return "action/" + Segment(actionId) + "/label"; }
        public static string ActionDescription(string actionId) { return "action/" + Segment(actionId) + "/description"; }

        private static string Segment(string value)
        {
            if (value == null) throw new ArgumentNullException("value");
            return value.Replace("~", "~0").Replace("/", "~1");
        }

        internal static string LocaleFor(int index) { return index >= 0 && index < Locales.Length ? Locales[index] : Locales[0]; }
        internal static string NormalizeLocale(string locale)
        {
            foreach (string supported in Locales)
                if (string.Equals(locale, supported, StringComparison.OrdinalIgnoreCase)) return supported;
            return null;
        }
    }
}
