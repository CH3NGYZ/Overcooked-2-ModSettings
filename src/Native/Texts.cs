using System.Collections.Generic;

namespace OC2ModSettings
{
    internal enum UiText
    {
        Title, PanelTitle, Search, Advanced, Defaults, Empty, Confirm, Cancel, Clear, Close, Refresh, Loading,
        Rebind, PressKey, InvalidValue, ReadOnly, Locked, Capture, PickColor, Hue, Opacity, ColorHint,
        ShortcutDuplicate, ShortcutDuplicateCount, ShortcutDuplicateMore, TogglePanel
    }

    internal static partial class Texts
    {
        private static readonly Dictionary<UiText, string[]> translations = CreateTranslations();
        private static Dictionary<UiText, string[]> CreateTranslations()
        {
            Dictionary<UiText, string[]> result = new Dictionary<UiText, string[]>();
            AddCommon(result); AddEditing(result); AddColor(result); AddShortcutConflicts(result); return result;
        }
        // Shipped SupportedLanguages: English .. ChineseTraditional (0 .. 11).
        // Each message keeps all twelve languages together; unknown languages use English.
        internal static string Resolve(UiText key, SupportedLanguages language)
        {
            string[] values = translations[key]; int index = (int)language;
            return values[index >= 0 && index < values.Length ? index : 0];
        }
        private static string Get(UiText key) { return Resolve(key, Localization.GetLanguage()); }
        internal static string Title { get { return Get(UiText.Title); } }
        internal static string PanelTitle { get { return Get(UiText.PanelTitle); } }
        internal static string Search { get { return Get(UiText.Search); } }
        internal static string Advanced { get { return Get(UiText.Advanced); } }
        internal static string Defaults { get { return Get(UiText.Defaults); } }
        internal static string Empty { get { return Get(UiText.Empty); } }
        internal static string Confirm { get { return Get(UiText.Confirm); } }
        internal static string Cancel { get { return Get(UiText.Cancel); } }
        internal static string Clear { get { return Get(UiText.Clear); } }
        internal static string Close { get { return Get(UiText.Close); } }
        internal static string Refresh { get { return Get(UiText.Refresh); } }
        internal static string Loading { get { return Get(UiText.Loading); } }
        internal static string Rebind { get { return Get(UiText.Rebind); } }
        internal static string PressKey { get { return Get(UiText.PressKey); } }
        internal static string InvalidValue { get { return Get(UiText.InvalidValue); } }
        internal static string ReadOnly { get { return Get(UiText.ReadOnly); } }
        internal static string Locked { get { return Get(UiText.Locked); } }
        internal static string Capture { get { return Get(UiText.Capture); } }
        internal static string PickColor { get { return Get(UiText.PickColor); } }
        internal static string Hue { get { return Get(UiText.Hue); } }
        internal static string Opacity { get { return Get(UiText.Opacity); } }
        internal static string ColorHint { get { return Get(UiText.ColorHint); } }
        internal static string Boolean(bool enabled)
        {
            string value;
            return Localization.Get(enabled ? "OptionValue.On" : "OptionValue.Off", out value)
                ? value : (enabled ? "ON" : "OFF");
        }
    }
}
