using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace OC2ModSettings
{
    [BepInPlugin(Guid, DisplayName, Version)]
    public sealed class ModSettingsPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.ch3ngyz.plugin.overcooked2.modsettings";
        public const string Version = "1.2.0";
        public const string DisplayName = "MOD Settings / MOD 设置";
        internal const string Author = "Github@CH3NGYZ";
        internal const string Description = "《胡闹厨房 2》原生公共 MOD 设置页面，支持自动发现配置、即时校验保存、改键、色板和业务按钮。";
        internal static ModSettingsPlugin Instance;
        internal readonly ConfigCatalog Catalog = new ConfigCatalog();
        internal PinnedMods Pins;
        private Harmony harmony;
        private NativeEntry entry;
        private NativeShortcut shortcut;
        internal NativePage ActivePage { get { return shortcut == null ? null : shortcut.ActivePage; } }
        internal void OpenSettings() { if (shortcut != null) shortcut.Open(); }
        internal void Track(T17EventSystem domain) { if (shortcut != null) shortcut.Track(domain); }
        internal void Track(PlayerControls player) { if (shortcut != null) shortcut.Track(player); }
        private bool available;
        public int GetApiVersionV1() { return 1; }
        public string GetVersionV1() { return Version; }
        public string GetStatusV1() { return available ? "ready" : "unavailable"; }
        public IDisposable RegisterConfigV1(string ownerGuid, string displayName, string description, ConfigFile config)
        {
            if (string.IsNullOrEmpty(ownerGuid) || config == null) throw new ArgumentException("Owner GUID and ConfigFile are required.");
            return Catalog.Registrations.Set(ownerGuid, new ConfigCatalog.Registration
                { Name = string.IsNullOrEmpty(displayName) ? ownerGuid : displayName, Description = description, File = config });
        }
        public IDisposable ConfigureEntryV1(ConfigEntryBase config, IDictionary<string, object> metadata)
        {
            if (config == null || metadata == null) throw new ArgumentNullException("config/metadata");
            return Catalog.Metadata.Set(config, new Dictionary<string, object>(metadata));
        }
        public IDisposable RegisterRendererV1(ConfigEntryBase config, IDictionary<string, object> protocol)
        {
            RendererContract.Validate(protocol);
            if (config == null) throw new ArgumentNullException("config");
            return Catalog.Renderers.Set(config, new Dictionary<string, object>(protocol));
        }
        public void RefreshV1() { Catalog.Invalidate(); }
        public IDisposable RegisterActionV1(string ownerGuid, string actionId, IDictionary<string, object> descriptor)
        {
            RegisteredAction action = RegisteredAction.Read(ownerGuid, actionId, descriptor);
            return Catalog.Actions.Set(action.Key, action);
        }
        private void Awake()
        {
            Instance = this;
            try
            {
                Pins = new PinnedMods(Config);
                harmony = new Harmony(Guid);
                NativeHooks.Install(harmony);
                entry = new NativeEntry(this);
                shortcut = new NativeShortcut(this);
                available = true;
                Logger.LogInfo("API v1 ready; selected implementation " + Version + " from " + GetType().Assembly.Location
                    + ". Duplicate versions require identical source; loader fallback requires restart/removing the failed candidate.");
            }
            catch (Exception error)
            {
                Report("Awake", error);
                if (harmony != null) harmony.UnpatchSelf();
            }
        }
        internal void FrontendShown(FrontendMenuBehaviour menu, GamepadUser user)
        { if (available && entry != null) entry.Refresh(menu, user); }
        private void Update()
        {
            if (entry != null) entry.Tick();
            if (shortcut != null) shortcut.Tick(shortcut.Pressed);
        }
        internal void Report(string target, Exception error) { Logger.LogError(target + "\n" + error); }
        private void OnDestroy()
        {
            available = false;
            if (shortcut != null) shortcut.Dispose();
            if (entry != null) entry.Dispose();
            NativePageTemplate.Dispose();
            Catalog.Dispose();
            if (Pins != null) Pins.Dispose();
            NativeIcons.Dispose();
            if (harmony != null) harmony.UnpatchSelf();
            if (Instance == this) Instance = null;
        }
    }
}
