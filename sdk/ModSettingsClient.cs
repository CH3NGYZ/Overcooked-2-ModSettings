using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;

namespace OC2ModSettings.SDK
{
    // Compile into the consumer. This file deliberately has no reference to OC2ModSettings.dll.
    public sealed class ModSettingsClient
    {
        public const string ServiceGuid = "com.ch3ngyz.plugin.overcooked2.modsettings";
        private object instance;
        private readonly Dictionary<string, MethodInfo> methods = new Dictionary<string, MethodInfo>();
        public bool Connect()
        {
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(ServiceGuid, out info) || info.Instance == null)
            { instance = null; methods.Clear(); return false; }
            if (!ReferenceEquals(instance, info.Instance)) { instance = info.Instance; methods.Clear(); }
            try { return (int)Call("GetApiVersionV1") == 1 && (string)Call("GetStatusV1") == "ready"; }
            catch { instance = null; methods.Clear(); return false; }
        }
        private object Call(string name, params object[] args)
        {
            if (instance == null) throw new InvalidOperationException("MOD Settings is unavailable.");
            MethodInfo method = Find(name);
            if (method == null) throw new MissingMethodException(name);
            return method.Invoke(instance, args);
        }
        private MethodInfo Find(string name)
        {
            if (instance == null) return null;
            MethodInfo method;
            if (!methods.TryGetValue(name, out method))
            {
                method = instance.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public);
                methods.Add(name, method);
            }
            return method;
        }
        public bool SupportsActions { get { return Find("RegisterActionV1") != null; } }
        public IDisposable Action(string ownerGuid, string actionId, IDictionary<string, object> descriptor)
        { return (IDisposable)Call("RegisterActionV1", ownerGuid, actionId, descriptor); }
        public IDisposable RegisterAction(string ownerGuid, string actionId, ModSettingsAction action)
        {
            if (action == null) throw new ArgumentNullException("action");
            return Action(ownerGuid, actionId, new Dictionary<string, object> {
                { "Version", 1 }, { "DisplayName", action.DisplayName }, { "Execute", action.Execute },
                { "ButtonLabel", action.ButtonLabel }, { "Description", action.Description }, { "Category", action.Category },
                { "CategoryOrder", action.CategoryOrder }, { "Order", action.Order }, { "Entry", action.Entry },
                { "CanExecute", action.CanExecute }, { "ClosePage", action.ClosePage }
            });
        }
        public IDisposable Register(string guid, string name, string description, ConfigFile config)
        { return (IDisposable)Call("RegisterConfigV1", guid, name, description, config); }
        public bool SupportsModDisplayName { get { return Find("RegisterModDisplayNameV1") != null; } }
        public IDisposable RegisterModDisplayName(string ownerGuid, string displayName)
        { return (IDisposable)Call("RegisterModDisplayNameV1", ownerGuid, displayName); }
        public bool SupportsTranslations { get { return Find("RegisterTranslationsV1") != null; } }
        public IDisposable RegisterTranslations(string ownerGuid, IDictionary<string, IDictionary<string, string>> translations)
        { return (IDisposable)Call("RegisterTranslationsV1", ownerGuid, translations); }
        public IDisposable Configure(ConfigEntryBase entry, IDictionary<string, object> metadata)
        { return (IDisposable)Call("ConfigureEntryV1", entry, metadata); }
        public IDisposable Renderer(ConfigEntryBase entry, IDictionary<string, object> protocol)
        { return (IDisposable)Call("RegisterRendererV1", entry, protocol); }
        public void Refresh() { Call("RefreshV1"); }
    }

    /// <summary>Consumer-side action description. Register again with the same ID to update it.</summary>
    public sealed class ModSettingsAction
    {
        /// <summary>Standalone row title and searchable action name, not a configuration key.</summary>
        public string DisplayName;
        /// <summary>Button-only caption, optionally with newlines. Null or empty uses DisplayName.</summary>
        public string ButtonLabel;
        public string Description, Category;
        public int? CategoryOrder;
        public int Order;
        /// <summary>Attach to this config entry, or leave null for a standalone row.</summary>
        public ConfigEntryBase Entry;
        public Action Execute;
        public Func<bool> CanExecute;
        public bool ClosePage;

        public ModSettingsAction(string displayName, Action execute)
        { DisplayName = displayName; Execute = execute; }
    }
}
