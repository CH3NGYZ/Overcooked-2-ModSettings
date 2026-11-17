using System.Collections.Generic;

namespace OC2ModSettings
{
    internal static partial class Texts
    {
        private static void AddEditing(Dictionary<UiText, string[]> t)
        {
            // Same order as SupportedLanguages. Each message owns all twelve translations.
            t.Add(UiText.Rebind, new string[] { "Rebind", "Changer la touche", "Cambia tasto", "Taste ändern", "Cambiar tecla", "Сменить клавишу", "Alterar tecla", "Zmień klawisz", "改键", "キー変更", "키 변경", "改鍵" });
            t.Add(UiText.PressKey, new string[] { "Press a key", "Appuyez sur une touche", "Premi un tasto", "Taste drücken", "Pulsa una tecla", "Нажмите клавишу", "Pressione uma tecla", "Naciśnij klawisz", "请按键", "キーを押してください", "키를 누르세요", "請按鍵" });
            t.Add(UiText.InvalidValue, new string[] { "Invalid value", "Valeur incorrecte", "Valore non valido", "Ungültiger Wert", "Valor no válido", "Недопустимое значение", "Valor inválido", "Nieprawidłowa wartość", "数值有误", "値が正しくありません", "잘못된 값입니다", "數值有誤" });
            t.Add(UiText.ReadOnly, new string[] { "Read-only: no reliable text converter for this type.", "Lecture seule : aucun convertisseur fiable pour ce type.", "Sola lettura: nessun convertitore di testo affidabile per questo tipo.", "Schreibgeschützt: kein zuverlässiger Textkonverter für diesen Typ.", "Solo lectura: no hay un conversor de texto fiable para este tipo.", "Только чтение: для этого типа нет надёжного текстового преобразователя.", "Somente leitura: não há conversor de texto confiável para este tipo.", "Tylko do odczytu: brak niezawodnego konwertera tekstu dla tego typu.", "只读：此类型没有可靠的文本转换器。", "読み取り専用：この型には適切なテキスト変換がありません。", "읽기 전용: 이 형식에 적합한 텍스트 변환기가 없습니다.", "唯讀：此類型沒有可靠的文字轉換器。" });
            t.Add(UiText.Locked, new string[] { "Read-only: declared by the MOD.", "Lecture seule : définie par le mod.", "Sola lettura: impostato dalla mod.", "Schreibgeschützt: vom Mod festgelegt.", "Solo lectura: definido por el mod.", "Только чтение: задано модом.", "Somente leitura: definido pelo mod.", "Tylko do odczytu: ustawione przez mod.", "只读：由 MOD 声明。", "読み取り専用：MOD による指定。", "읽기 전용: 모드에서 지정했습니다.", "唯讀：由 MOD 指定。" });
            t.Add(UiText.Capture, new string[] { "Press a key or shortcut; Esc cancels", "Appuyez sur une touche ou un raccourci ; Échap pour annuler", "Premi un tasto o una combinazione; Esc per annullare", "Taste oder Tastenkombination drücken; Esc zum Abbrechen", "Pulsa una tecla o combinación; Esc para cancelar", "Нажмите клавишу или сочетание; Esc — отмена", "Pressione uma tecla ou combinação; Esc cancela", "Naciśnij klawisz lub kombinację; Esc anuluje", "按下按键或组合键；Esc 取消", "キーまたは組み合わせを入力（Esc でキャンセル）", "키 또는 단축키를 누르세요. Esc로 취소", "按下按鍵或組合鍵；Esc 取消" });
        }
    }
}
