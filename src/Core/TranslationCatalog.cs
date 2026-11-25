using System;
using System.Collections.Generic;
using OC2ModSettings.SDK;

namespace OC2ModSettings
{
    internal sealed class TranslationCatalog
    {
        private const string English = "en";
        private readonly LeaseTable<string, IDictionary<string, IDictionary<string, string>>> registrations;
        private readonly Action changed;
        private TranslationFiles files;
        private string locale = English;

        internal TranslationCatalog(Action changed)
        {
            this.changed = changed;
            registrations = new LeaseTable<string, IDictionary<string, IDictionary<string, string>>>(changed);
        }

        internal void LoadFiles(string directory, Action<string> warn)
        { files = TranslationFiles.Load(directory, warn); changed(); }

        internal void SetLanguage(SupportedLanguages language)
        {
            string current = ModSettingsTranslations.LocaleFor((int)language);
            if (locale == current) return;
            locale = current; changed();
        }

        internal IDisposable Register(string ownerGuid, IDictionary<string, IDictionary<string, string>> translations)
        {
            if (string.IsNullOrEmpty(ownerGuid) || ownerGuid.Trim().Length == 0)
                throw new ArgumentException("An owner GUID is required.", "ownerGuid");
            if (translations == null) throw new ArgumentNullException("translations");
            Dictionary<string, IDictionary<string, string>> snapshot = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, IDictionary<string, string>> table in translations)
            {
                string supported = ModSettingsTranslations.NormalizeLocale(table.Key);
                if (supported == null || table.Value == null) continue;
                if (snapshot.ContainsKey(supported)) throw new ArgumentException("Duplicate locale: " + supported, "translations");
                snapshot.Add(supported, new Dictionary<string, string>(table.Value, StringComparer.Ordinal));
            }
            return registrations.Set(ownerGuid, snapshot);
        }

        internal string Resolve(string ownerGuid, string key, string fallback, bool allowEmpty)
        {
            IDictionary<string, IDictionary<string, string>> api;
            registrations.TryGet(ownerGuid, out api);
            string current = locale;
            return Lookup(api, ownerGuid, key, current, allowEmpty)
                ?? (current == English ? null : Lookup(api, ownerGuid, key, English, allowEmpty)) ?? fallback;
        }

        private string Lookup(IDictionary<string, IDictionary<string, string>> api, string ownerGuid, string key, string language, bool allowEmpty)
        {
            IDictionary<string, string> table;
            string value;
            if (api != null && api.TryGetValue(language, out table) && table.TryGetValue(key, out value) && HasText(value, allowEmpty))
                return value;
            value = files == null ? null : files.Get(ownerGuid, key, language);
            return HasText(value, allowEmpty) ? value : null;
        }

        private static bool HasText(string value, bool allowEmpty)
        { return value != null && (allowEmpty || value.Trim().Length > 0); }
    }
}
