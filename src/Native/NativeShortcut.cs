using System;
using UnityEngine;

namespace OC2ModSettings
{
    // Readiness is latched once. Loading, profile reloads and dialogs cannot revoke it.
    internal sealed class NativeShortcut : IDisposable
    {
        private readonly ModSettingsPlugin plugin;
        private GlobalSettingsHost host;
        private NativePage page;
        private bool pending;
        private int handledFrame = -1;
        internal NativeShortcut(ModSettingsPlugin owner) { plugin = owner; }
        internal void Tick(bool pressed)
        {
            NativePage open = ActivePage;
            if (pressed && handledFrame != Time.frameCount)
            {
                handledFrame = Time.frameCount;
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
                    page = NativePage.Create(host, plugin.Catalog);
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
