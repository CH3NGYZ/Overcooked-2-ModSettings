using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using OC2ModSettings.SDK;
using UnityEngine;

namespace OC2ModSettings
{
    // Readiness is latched once. Loading, profile reloads and dialogs cannot revoke it.
    internal sealed class NativeShortcut : IDisposable
    {
        internal const KeyCode DefaultKey = KeyCode.BackQuote;
        private static readonly HashSet<KeyCode> BindableKeys = ReadBindableKeys();
        internal static bool IsBindable(KeyCode key) { return BindableKeys.Contains(key); }

        private static HashSet<KeyCode> ReadBindableKeys()
        {
            HashSet<KeyCode> keys = new HashSet<KeyCode>();
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                // The shipped enum puts mouse and all joystick buttons after the keyboard.
                if (key <= KeyCode.None || key >= KeyCode.Mouse0
                    || (key >= KeyCode.Numlock && key <= KeyCode.AltGr) // locks, modifiers and their aliases
                    || (key >= KeyCode.UpArrow && key <= KeyCode.PageDown) // navigation
                    || (key >= KeyCode.Help && key <= KeyCode.Menu)) continue; // system keys
                switch (key)
                {
                    case KeyCode.Backspace: case KeyCode.Tab: case KeyCode.Clear: case KeyCode.Return:
                    case KeyCode.KeypadEnter: case KeyCode.Pause: case KeyCode.Escape: case KeyCode.Space:
                    case KeyCode.Delete: break;
                    default: keys.Add(key); break;
                }
            }
            return keys;
        }

        internal static bool CommandModifierHeld
        {
            get
            {
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                    || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr)
                    || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)
                    || Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows);
            }
        }

        private static string KeyLabel(object value)
        {
            KeyCode key = (KeyCode)value;
            if (key == DefaultKey) return "` / ~";
            if (key >= KeyCode.Exclaim && key <= KeyCode.BackQuote) return ((char)(int)key).ToString();
            if (key >= KeyCode.Keypad0 && key <= KeyCode.KeypadPlus)
                return "Num " + "0123456789./*-+"[(int)key - (int)KeyCode.Keypad0];
            if (key == KeyCode.KeypadEquals) return "Num =";
            return key.ToString();
        }

        // BepInEx 5's AcceptableValueList<T> requires IEquatable<T>, which KeyCode lacks.
        private sealed class ToggleKeys : AcceptableValueBase
        {
            internal ToggleKeys() : base(typeof(KeyCode)) { }
            public override bool IsValid(object value) { return value is KeyCode && IsBindable((KeyCode)value); }
            public override object Clamp(object value) { return IsValid(value) ? value : DefaultKey; }
            public override string ToDescriptionString()
            { return "# Acceptable values: keyboard keys, excluding modifiers, locks, navigation, editing and system keys, Space and Esc."; }
        }
        private readonly ConfigEntry<KeyCode> binding;
        internal readonly SettingRow Setting;
        private readonly ModSettingsPlugin plugin;
        private GlobalSettingsHost host;
        private NativePage page;
        private bool pending;
        private int handledFrame = -1;
        internal NativeShortcut(ModSettingsPlugin owner)
        {
            plugin = owner;
            binding = owner.Config.Bind("Navigation", "ToggleKey", DefaultKey, new ConfigDescription(
                "Key to open or close MOD Settings. Change it in the page footer.", new ToggleKeys(),
                new ModSettingsAttributes { Browsable = false }));
            Setting = new SettingRow { Entry = binding, Metadata = EntryMetadata.Read(binding, null) };
            Setting.Metadata.Format = KeyLabel;
        }
        internal KeyCode Key { get { return binding.Value; } }
        internal bool Pressed
        {
            get
            {
                KeyCode key = Key;
                if (!Input.GetKeyDown(key) || CommandModifierHeld) return false;
                // Retain the original shortcut behavior; ordinary characters must remain typeable.
                if (key == DefaultKey || (key >= KeyCode.F1 && key <= KeyCode.F15)) return true;
                NativePage open = ActivePage;
                return open == null || !open.IsEditingText;
            }
        }
        internal void ConsumeCurrentFrame() { handledFrame = Time.frameCount; }
        // HUD Customizer filters this bool by reflection while its editor owns input.
        internal void Tick(bool pressed)
        {
            NativePage open = ActivePage;
            if (pressed && handledFrame != Time.frameCount && (open == null || !open.IsRecordingToggleKey))
            {
                ConsumeCurrentFrame();
                if (open != null) { pending = false; open.Hide(true, false); return; }
                pending = !pending;
            }
            try
            {
                if (host == null)
                {
                    FrontendRootMenu root = T17FrontendFlow.Instance == null ? null : T17FrontendFlow.Instance.m_Rootmenu;
                    if (!NativeHooks.Ready(root)) return;
                    NativePageTemplate.Capture(root);
                    host = new GlobalSettingsHost(root.CurrentGamepadUser);
                    page = NativePage.Create(host, plugin.Catalog, this);
                }
                if (pending) { Open(); pending = false; }
                if (ActivePage != null) { page.MaintainInput(); host.Tick(); }
            }
            catch (Exception error) { pending = false; plugin.Report("NativeShortcut", error); Dispose(); }
        }
        internal void Open()
        {
            if (page == null) { pending = true; return; }
            page.Open();
        }
        internal NativePage ActivePage { get { return page != null && page.gameObject.activeInHierarchy ? page : null; } }
        internal NativePage CachedPage { get { return page; } }
        internal void Track(T17EventSystem domain) { if (ActivePage != null) page.Track(domain); }
        internal void Track(PlayerControls player) { if (ActivePage != null) page.Track(player); }
        public void Dispose()
        {
            pending = false;
            if (page != null) { page.Hide(false, false); UnityEngine.Object.Destroy(page.gameObject); }
            page = null;
            if (host != null) host.Dispose(); host = null;
        }
    }
}
