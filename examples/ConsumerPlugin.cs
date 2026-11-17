using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;

namespace ModSettingsExample
{
    [BepInPlugin("example.modsettings.consumer", "MOD Settings consumer example", "1.0.0")]
    [BepInDependency(ModSettingsClient.ServiceGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class ConsumerPlugin : BaseUnityPlugin
    {
        private IDisposable display, renderer, action, attached;
        private void Start()
        {
            ConfigEntry<bool> enabled = Config.Bind("General", "Enabled", true, "Example setting");
            ModSettingsClient client = new ModSettingsClient();
            if (!client.Connect()) return; // The mod's business behavior remains available.
            try
            {
                display = client.Register(Info.Metadata.GUID, "Example", "Example description", Config);
                renderer = client.Renderer(enabled, NativeBooleanRenderer.Protocol());
                if (client.SupportsActions)
                {
                    action = client.Action(Info.Metadata.GUID, "check", new Dictionary<string, object> {
                        { "Version", 1 }, { "DisplayName", "Check status" }, { "Category", "General" },
                        { "Execute", new Action(delegate { Logger.LogInfo("Existing business action invoked"); }) } });
                    attached = client.Action(Info.Metadata.GUID, "details", new Dictionary<string, object> {
                        { "Version", 1 }, { "DisplayName", "Details" }, { "Entry", enabled },
                        { "Execute", new Action(delegate { Logger.LogInfo("Existing detail action invoked"); }) } });
                }
            }
            catch (Exception error) { Logger.LogWarning("Optional MOD Settings integration: " + error.Message); }
        }
        private void OnDestroy()
        {
            if (attached != null) attached.Dispose(); if (action != null) action.Dispose();
            if (renderer != null) renderer.Dispose(); if (display != null) display.Dispose();
        }
    }
}
