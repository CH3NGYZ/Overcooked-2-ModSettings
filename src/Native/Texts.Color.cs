using System.Collections.Generic;

namespace OC2ModSettings
{
    internal static partial class Texts
    {
        internal const string HexHint = "#" + "RR" + "GG" + "BB" + "AA";
        private static void AddColor(Dictionary<UiText, string[]> t)
        {
            t.Add(UiText.PickColor, new string[] { "Pick color", "Choisir la couleur", "Scegli colore", "Farbe wählen", "Elegir color", "Выбрать цвет", "Escolher cor", "Wybierz kolor", "选色", "色を選択", "색상 선택", "選色" });
            t.Add(UiText.Hue, new string[] { "Hue", "Teinte", "Tonalità", "Farbton", "Tono", "Цветовой тон", "Matiz", "Odcień", "色相", "色相", "색조", "色相" });
            t.Add(UiText.Opacity, new string[] { "Opacity A", "Opacité A", "Opacità A", "Deckkraft A", "Opacidad A", "Непрозрачность A", "Opacidade A", "Krycie A", "透明度 A", "不透明度 A", "불투명도 A", "透明度 A" });
            t.Add(UiText.ColorHint, new string[] {
                "Drag to preview; release to save\nPad: confirm, adjust, confirm to save",
                "Glissez pour choisir ; relâchez pour enregistrer\nManette : validez, ajustez, puis validez",
                "Trascina per scegliere; rilascia per salvare\nController: conferma, regola, poi conferma",
                "Ziehen zur Vorschau; loslassen zum Speichern\nController: bestätigen, anpassen, bestätigen",
                "Arrastra para elegir; suelta para guardar\nMando: confirma, ajusta y confirma",
                "Перетащите для выбора; отпустите для сохранения\nГеймпад: подтвердите, настройте, подтвердите",
                "Arraste para escolher; solte para salvar\nControle: confirme, ajuste e confirme",
                "Przeciągnij, aby wybrać; puść, aby zapisać\nPad: potwierdź, dostosuj i potwierdź",
                "拖动选色，松手保存\n方向键：确认后调色，再确认保存",
                "ドラッグで色を選び、離して保存\nパッド：決定、調整、もう一度決定で保存",
                "드래그하여 선택하고 놓으면 저장\n컨트롤러: 확인, 조정 후 다시 확인",
                "拖曳選色，放開儲存\n方向鍵：確認後調色，再確認儲存" });
        }
    }
}
