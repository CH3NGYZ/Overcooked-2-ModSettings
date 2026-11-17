using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using OC2ModSettings.SDK;

namespace ModSettingsExample
{
    [BepInPlugin("example.modsettings.consumer", "MOD Settings consumer example", "1.0.0")]
    [BepInDependency(ModSettingsClient.ServiceGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class ConsumerPlugin : BaseUnityPlugin
    {
        private readonly ModSettingsClient client = new ModSettingsClient();
        private readonly ModSettingsAttributes enabledDisplay = new ModSettingsAttributes { CategoryOrder = 100, Order = 20 };
        private readonly ModSettingsAttributes roleDisplay = new ModSettingsAttributes { CategoryOrder = 100, Order = 18 };
        private ConfigEntry<string> language;
        private ConfigEntry<bool> featureEnabled;
        private IDisposable display, renderer, action, attached;
        private void Start()
        {
            language = Config.Bind("Language", "Language", "en", new ConfigDescription("Settings display language",
                new AcceptableValueList<string>("en", "zh"), new ModSettingsAttributes { CategoryOrder = 200 }));
            featureEnabled = Config.Bind("General", "Enabled", true, new ConfigDescription("Example setting", null, enabledDisplay));
            // Persist stable IDs; translate only the presentation, including dropdown candidates.
            roleDisplay.ObjToStr = delegate(object value) {
                bool chinese = language.Value == "zh";
                return (string)value == "host" ? (chinese ? "强制主机" : "Force host") : (chinese ? "游戏默认" : "Game default"); };
            Config.Bind("General", "Role", "default", new ConfigDescription("Role", new AcceptableValueList<string>("default", "host"), roleDisplay));
            language.SettingChanged += OnLanguageChanged;
            RefreshLocalizedSettings(); // Also initializes metadata when the optional service is absent.
            try
            {
                if (client.Connect()) renderer = client.Renderer(featureEnabled, NativeBooleanRenderer.Protocol());
            }
            catch (Exception error) { Logger.LogWarning("Optional MOD Settings integration: " + error.Message); }
        }
        private void OnLanguageChanged(object sender, EventArgs args) { RefreshLocalizedSettings(); }
        private void RefreshLocalizedSettings()
        {
            bool chinese = language.Value == "zh";
            enabledDisplay.DispName = chinese ? "开启功能" : "Enabled";
            enabledDisplay.Description = chinese ? "示例配置，合法修改由设置页保存。" : "Example setting; the settings page saves valid edits.";
            enabledDisplay.Category = chinese ? "常用功能" : "General";
            roleDisplay.Category = enabledDisplay.Category;
            roleDisplay.DispName = chinese ? "默认角色" : "Default role";
            roleDisplay.Description = chinese ? "候选文案跟随本 MOD 语言，保存值保持不变。" : "Choice labels follow this MOD's language; saved IDs stay stable.";
            try
            {
                if (!client.Connect()) return; // Business behavior and ConfigEntry remain independent of the service.
                Replace(ref display, client.Register(Info.Metadata.GUID, chinese ? "示例 MOD" : "Example MOD",
                    chinese ? "SDK 接入示例" : "SDK consumer example", Config));
                if (client.SupportsActions)
                {
                    Replace(ref action, client.Action(Info.Metadata.GUID, "check", new Dictionary<string, object> {
                        { "Version", 1 }, { "DisplayName", chinese ? "检查状态" : "Check status" },
                        { "Category", enabledDisplay.Category }, { "CategoryOrder", 100 }, { "Order", 19 },
                        { "Execute", new Action(delegate { Logger.LogInfo("Existing business action invoked"); }) } }));
                    Replace(ref attached, client.Action(Info.Metadata.GUID, "details", new Dictionary<string, object> {
                        { "Version", 1 }, { "DisplayName", chinese ? "详情" : "Details" }, { "Entry", featureEnabled },
                        { "Execute", new Action(delegate { Logger.LogInfo("Existing detail action invoked"); }) } }));
                }
                client.Refresh(); // All translated tags/actions are ready; do not Reload or re-Bind ConfigFile.
            }
            catch (Exception error) { Logger.LogWarning("Optional MOD Settings integration: " + error.Message); }
        }
        private static void Replace(ref IDisposable lease, IDisposable replacement)
        {
            IDisposable previous = lease; lease = replacement;
            if (previous != null) previous.Dispose();
        }
        private void OnDestroy()
        {
            if (language != null) language.SettingChanged -= OnLanguageChanged;
            if (attached != null) attached.Dispose(); if (action != null) action.Dispose();
            if (renderer != null) renderer.Dispose(); if (display != null) display.Dispose();
        }
    }
}
