namespace OC2ModSettings
{
    internal static class Texts
    {
        private static bool Chinese
        {
            get { SupportedLanguages l = Localization.GetLanguage(); return l == SupportedLanguages.Chinese || l == SupportedLanguages.ChineseTraditional; }
        }
        internal static string Pick(string zh, string en) { return Chinese ? zh : en; }
        internal static string Title { get { return Pick("MOD 设置", "MOD Settings"); } }
        internal static string Search { get { return Pick("搜索当前 MOD…", "Search current MOD…"); } }
        internal static string Advanced { get { return Pick("显示高级项", "Show advanced"); } }
        internal static string Defaults { get { return Pick("恢复默认", "Restore defaults"); } }
        internal static string Empty { get { return Pick("没有匹配的设置", "No matching settings"); } }
        internal static string Confirm { get { return Pick("确认", "Confirm"); } }
        internal static string Cancel { get { return Pick("取消", "Cancel"); } }
        internal static string Clear { get { return Pick("清空", "Clear"); } }
        internal static string Rebind { get { return Pick("改键", "Rebind"); } }
        internal static string PressKey { get { return Pick("请按键", "Press a key"); } }
        internal static string Close { get { return Pick("关闭", "Close"); } }
        internal static string Refresh { get { return Pick("刷新", "Refresh"); } }
        internal static string Loading { get { return Pick("正在读取 MOD 设置…", "Loading MOD settings…"); } }
        internal static string InvalidValue { get { return Pick("数值有误", "Invalid value"); } }
        internal static string Boolean(bool enabled)
        {
            string value;
            return Localization.Get(enabled ? "OptionValue.On" : "OptionValue.Off", out value)
                ? value : (enabled ? "ON" : "OFF");
        }
        internal static string ReadOnly { get { return Pick("只读：此类型没有可靠的文本转换器。", "Read-only: no reliable text converter for this type."); } }
        internal static string Locked { get { return Pick("只读：由 MOD 声明。", "Read-only: declared by the MOD."); } }
        internal static string Capture { get { return Pick("按下按键或组合键；Esc 取消", "Press a key or shortcut; Esc cancels"); } }
    }
}
