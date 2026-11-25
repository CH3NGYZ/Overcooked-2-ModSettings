using System;
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
        internal const string Section = "General", EnabledKey = "Enabled", CheckAction = "check", DetailsAction = "details";
        private IDisposable translations, renderer, action, attached;

        private void Start()
        {
            ConfigEntry<bool> enabled = Config.Bind(Section, EnabledKey, true,
                new ConfigDescription("Enable this example feature.", null,
                    new ModSettingsAttributes { DispName = "Enable feature", CategoryOrder = 100, Order = 20 }));
            try
            {
                if (!client.Connect()) return;
                string owner = Info.Metadata.GUID;
                if (client.SupportsTranslations)
                    translations = client.RegisterTranslations(owner, ExampleSettingsTranslations.Create());
                if (client.SupportsActions)
                {
                    action = client.RegisterAction(owner, CheckAction,
                        new ModSettingsAction("Check service status", delegate { Logger.LogInfo("Check status"); }) {
                            ButtonLabel = "Check now", Description = "Run a status check.", Category = Section, CategoryOrder = 100, Order = 19 });
                    attached = client.RegisterAction(owner, DetailsAction,
                        new ModSettingsAction("Feature details", delegate { Logger.LogInfo("Show feature details"); }) {
                            Entry = enabled, ButtonLabel = "View details", Description = "Show details for this feature." });
                }
                renderer = client.Renderer(enabled, NativeBooleanRenderer.Protocol());
            }
            catch (Exception error) { Logger.LogWarning("Optional MOD Settings integration: " + error.Message); }
        }

        private void OnDestroy()
        {
            if (attached != null) attached.Dispose(); if (action != null) action.Dispose();
            if (renderer != null) renderer.Dispose(); if (translations != null) translations.Dispose();
        }
    }
}
