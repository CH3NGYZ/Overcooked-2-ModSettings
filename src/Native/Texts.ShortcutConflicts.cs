using System.Collections.Generic;

namespace OC2ModSettings
{
    internal static partial class Texts
    {
        internal static string ShortcutDuplicate(string key, string bindings)
        { return string.Format(Get(UiText.ShortcutDuplicate), key, bindings); }
        internal static string ShortcutDuplicateCount(int count)
        { return string.Format(Get(UiText.ShortcutDuplicateCount), count); }
        internal static string ShortcutDuplicateMore(int count)
        { return string.Format(Get(UiText.ShortcutDuplicateMore), count); }
        internal static string TogglePanel { get { return Get(UiText.TogglePanel); } }

        private static void AddShortcutConflicts(Dictionary<UiText, string[]> translations)
        {
            translations.Add(UiText.ShortcutDuplicate, new string[] {
                "{0} is also used by: {1}", "{0} est aussi utilisé par : {1}", "{0} è in uso anche per: {1}", "{0} wird auch verwendet für: {1}",
                "{0} también se usa para: {1}", "{0} также используется для: {1}", "{0} também é usado em: {1}", "{0} służy też do: {1}",
                "{0} 也用于：{1}", "{0} は次にも使われています：{1}", "{0}을(를) 사용하는 다른 항목: {1}", "{0} 也用於：{1}" });
            translations.Add(UiText.ShortcutDuplicateCount, new string[] {
                "Conflicting shortcuts in this MOD: {0}", "Raccourcis en conflit dans ce mod : {0}", "Scorciatoie in conflitto in questa mod: {0}", "Kollidierende Tastenkürzel in diesem Mod: {0}",
                "Atajos en conflicto en este mod: {0}", "Сочетания с конфликтами в этом моде: {0}", "Atalhos em conflito neste mod: {0}", "Konfliktujące skróty w tym modzie: {0}",
                "当前 MOD 有 {0} 项快捷键重复", "この MOD で重複しているショートカット：{0} 件", "현재 모드에서 중복된 단축키: {0}개", "目前 MOD 有 {0} 項快捷鍵重複" });
            translations.Add(UiText.ShortcutDuplicateMore, new string[] {
                " +{0} more", " +{0} de plus", " +{0} altri", " +{0} weitere", " +{0} más", " ещё {0}",
                " +{0} a mais", " i {0} więcej", " +{0} 项", " ほか {0} 件", " 외 {0}개", " +{0} 項" });
            translations.Add(UiText.TogglePanel, new string[] {
                "Toggle panel", "Afficher/masquer le panneau", "Mostra/nascondi pannello", "Fenster öffnen/schließen",
                "Abrir/cerrar panel", "Открыть/закрыть панель", "Abrir/fechar painel", "Otwórz/zamknij panel",
                "开关面板", "パネルの開閉", "패널 열기/닫기", "開關面板" });
        }
    }
}
