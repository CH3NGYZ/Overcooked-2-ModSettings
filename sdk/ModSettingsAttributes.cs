using System;

namespace OC2ModSettings.SDK
{
    /// <summary>
    /// Display metadata passed through ConfigDescription.Tags, not a CLR attribute.
    /// Compile this source into a consumer; no Configuration Manager or service DLL reference is needed.
    /// Null fields leave the previous metadata value unchanged. Call ModSettingsClient.Refresh
    /// once after changing localized fields. Keep ConfigDefinition section/key identities stable.
    /// </summary>
    public sealed class ModSettingsAttributes
    {
        /// <summary>Localized config row title; does not change the stored key or any attached action caption.</summary>
        public string DispName;
        /// <summary>Localized category label; use CategoryOrder for language-independent ordering.</summary>
        public string Category;
        /// <summary>Localized hover/search description; an empty string explicitly clears it.</summary>
        public string Description;
        /// <summary>Category priority, highest first. Use the same value throughout a category.</summary>
        public int? CategoryOrder;
        /// <summary>Setting priority within its category, highest first.</summary>
        public int? Order;
        /// <summary>False hides this setting.</summary>
        public bool? Browsable;
        /// <summary>True shows this setting only when advanced settings are enabled.</summary>
        public bool? IsAdvanced;
        /// <summary>True prevents edits in the settings page.</summary>
        public bool? ReadOnly;
        /// <summary>Display a ranged numeric value as a percentage.</summary>
        public bool? ShowRangeAsPercent;
        /// <summary>Hide the default-value button.</summary>
        public bool? HideDefaultButton;
        /// <summary>Formats current values and dropdown candidates. May use the MOD's own language; keep stored IDs stable. Custom editable types also need StrToObj.</summary>
        public Func<object, string> ObjToStr;
        /// <summary>Optional parser. Throw on invalid input; do not change config from this callback.</summary>
        public Func<string, object> StrToObj;
    }
}
