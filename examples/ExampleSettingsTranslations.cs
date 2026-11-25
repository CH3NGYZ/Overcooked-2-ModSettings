using System;
using System.Collections.Generic;
using OC2ModSettings.SDK;

namespace ModSettingsExample
{
    internal static class ExampleSettingsTranslations
    {
        private static readonly string[] Locales = { "en", "fr", "it", "de", "es", "ru", "pt-BR", "pl", "zh-CN", "ja", "ko", "zh-TW" };

        internal static IDictionary<string, IDictionary<string, string>> Create()
        {
            var table = new Dictionary<string, IDictionary<string, string>>(StringComparer.Ordinal);
            foreach (string locale in Locales) table.Add(locale, new Dictionary<string, string>(StringComparer.Ordinal));

            Add(table, ModSettingsTranslations.ModName,
                "Example MOD", "Mod exemple", "Mod di esempio", "Beispiel-Mod", "Mod de ejemplo", "Пример мода",
                "Mod de exemplo", "Przykładowy mod", "示例 MOD", "サンプル MOD", "예제 모드", "範例 MOD");
            Add(table, ModSettingsTranslations.Group(ConsumerPlugin.Section),
                "General", "Général", "Generale", "Allgemein", "General", "Общие",
                "Geral", "Ogólne", "常用功能", "一般", "일반", "常用功能");
            Add(table, ModSettingsTranslations.ConfigName(ConsumerPlugin.Section, ConsumerPlugin.EnabledKey),
                "Enable feature", "Activer la fonction", "Attiva funzione", "Funktion aktivieren", "Activar función", "Включить функцию",
                "Ativar função", "Włącz funkcję", "开启功能", "機能を有効にする", "기능 사용", "啟用功能");
            Add(table, ModSettingsTranslations.ConfigDescription(ConsumerPlugin.Section, ConsumerPlugin.EnabledKey),
                "Enable this example feature.", "Active cette fonction d’exemple.", "Attiva questa funzione di esempio.",
                "Aktiviert diese Beispielfunktion.", "Activa esta función de ejemplo.", "Включает демонстрационную функцию.",
                "Ativa este recurso de exemplo.", "Włącza tę przykładową funkcję.", "开启后启用示例功能。",
                "サンプル機能を有効にします。", "예제 기능을 사용합니다.", "開啟後啟用範例功能。");
            Add(table, ModSettingsTranslations.ActionTitle(ConsumerPlugin.CheckAction),
                "Check service status", "Vérifier l’état du service", "Verifica lo stato del servizio", "Dienststatus prüfen",
                "Comprobar el estado del servicio", "Проверить состояние сервиса", "Verificar o estado do serviço",
                "Sprawdź stan usługi", "检查服务状态", "サービス状態を確認", "서비스 상태 확인", "檢查服務狀態");
            Add(table, ModSettingsTranslations.ActionLabel(ConsumerPlugin.CheckAction),
                "Check now", "Vérifier", "Verifica", "Prüfen", "Comprobar", "Проверить",
                "Verificar", "Sprawdź", "立即\n检查", "今すぐ確認", "지금 확인", "立即\n檢查");
            Add(table, ModSettingsTranslations.ActionDescription(ConsumerPlugin.CheckAction),
                "Run a status check.", "Lance une vérification de l’état.", "Avvia un controllo dello stato.", "Führt eine Statusprüfung aus.",
                "Realiza una comprobación del estado.", "Запускает проверку состояния.", "Executa uma verificação de status.",
                "Sprawdza stan usługi.", "执行一次状态检查。", "状態チェックを実行します。", "상태를 한 번 확인합니다.", "執行一次狀態檢查。");
            Add(table, ModSettingsTranslations.ActionTitle(ConsumerPlugin.DetailsAction),
                "Feature details", "Détails de la fonction", "Dettagli della funzione", "Funktionsdetails", "Detalles de la función",
                "Сведения о функции", "Detalhes do recurso", "Szczegóły funkcji", "功能详情", "機能の詳細", "기능 세부 정보", "功能詳情");
            Add(table, ModSettingsTranslations.ActionLabel(ConsumerPlugin.DetailsAction),
                "View details", "Voir les détails", "Visualizza dettagli", "Details anzeigen", "Ver detalles", "Подробнее",
                "Ver detalhes", "Pokaż szczegóły", "查看详情", "詳細を見る", "자세히 보기", "查看詳情");
            Add(table, ModSettingsTranslations.ActionDescription(ConsumerPlugin.DetailsAction),
                "Show details for this feature.", "Affiche les détails de cette fonction.", "Mostra i dettagli di questa funzione.",
                "Zeigt Details zu dieser Funktion an.", "Muestra los detalles de esta función.", "Показывает сведения об этой функции.",
                "Exibe detalhes sobre este recurso.", "Wyświetla szczegóły tej funkcji.", "显示此功能的详情。",
                "この機能の詳細を表示します。", "이 기능의 세부 정보를 표시합니다.", "顯示此功能的詳情。");
            return table;
        }

        private static void Add(IDictionary<string, IDictionary<string, string>> table, string key, params string[] values)
        {
            if (values.Length != Locales.Length) throw new ArgumentException("Expected 12 translations for " + key, "values");
            for (int index = 0; index < Locales.Length; index++) table[Locales[index]].Add(key, values[index]);
        }
    }
}
