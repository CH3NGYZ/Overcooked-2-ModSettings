using System;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class NativeEntry : IDisposable
    {
        private readonly ModSettingsPlugin plugin;
        private FrontendRootMenu root;
        private T17Button anchor, button;
        private RectTransform panel;
        private Selectable next;
        private bool nextLinked;
        private float extension, nextAttempt;
        private GameObject placeholder;
        internal NativeEntry(ModSettingsPlugin owner) { plugin = owner; }
        internal void Refresh(FrontendMenuBehaviour menu, GamepadUser user)
        {
            FrontendRootMenu current = menu.GetComponentInParent<FrontendRootMenu>();
            if (current == null) return;
            if (root != current) { Dispose(); root = current; }
            if (button != null && user != null) button.SetEventSystem(T17EventSystemsManager.Instance.GetEventSystemForGamepadUser(user));
            if (NativeHooks.Ready(root)) { NativePageTemplate.Capture(root); Ensure(); }
        }
        internal void Tick()
        {
            if (Time.realtimeSinceStartup < nextAttempt) return;
            nextAttempt = Time.realtimeSinceStartup + .5f;
            try { if (button == null && NativeHooks.Ready(root)) Ensure(); }
            catch (Exception e) { plugin.Report("NativeEntry.Tick", e); }
        }
        private void Ensure()
        {
            NativePageTemplate.Capture(root);
            if (button != null) return;
            Transform tab = root.transform.Find("WorldSpaceCanvas/Safezone_Rootmenu/RootTab/CustomizationOptions");
            Transform source = tab == null ? null : tab.Find("CustomChef");
            if (source == null) throw new MissingMemberException("CustomizationOptions/CustomChef");
            anchor = source.GetComponent<T17Button>();
            panel = source.parent as RectTransform;
            button = NativeUi.CloneButton(anchor, panel, "ModSettingsEntry", Texts.Title, delegate { Open(); });
            button.gameObject.AddComponent<MenuEntryFocus>().Initialize(button);
            button.transform.SetSiblingIndex(source.GetSiblingIndex() + 1);
            button.SetEventSystem(root.CachedEventSystem);
            Navigation original = anchor.navigation;
            next = original.selectOnDown;
            Navigation inserted = original; inserted.selectOnUp = anchor; inserted.selectOnDown = next;
            button.navigation = inserted;
            original.selectOnDown = button; anchor.navigation = original;
            if (next != null && next != anchor)
            {
                Navigation following = next.navigation;
                if (following.selectOnUp == anchor) { following.selectOnUp = button; next.navigation = following; nextLinked = true; }
            }
            // Only reuse an entirely empty layout slot. Any child/listener makes it another owner's slot.
            LayoutElement empty = null;
            foreach (Transform child in panel)
                if (child != button.transform && child.childCount == 0 && child.GetComponents<Component>().Length == 2
                    && child.GetComponent<LayoutElement>() != null) { empty = child.GetComponent<LayoutElement>(); break; }
            if (empty != null) { button.transform.SetSiblingIndex(empty.transform.GetSiblingIndex()); placeholder = empty.gameObject; placeholder.SetActive(false); }
            else Grow(anchor.GetComponent<RectTransform>().rect.height + 4f);
            LayoutRebuilder.MarkLayoutForRebuild(panel);
        }
        private void Grow(float delta)
        {
            float oldHeight = panel.rect.height;
            if (oldHeight + delta <= 0f) return;
            panel.sizeDelta += new Vector2(0f, delta);
            panel.pivot = new Vector2(panel.pivot.x, 1f - (1f - panel.pivot.y) * oldHeight / (oldHeight + delta));
            extension += delta;
        }
        private void Open()
        {
            try
            {
                if (!NativeHooks.Ready(root)) return;
                plugin.OpenSettings();
            }
            catch (Exception e) { plugin.Report("NativeEntry.Open", e); }
        }
        public void Dispose()
        {
            if (anchor != null && anchor.navigation.selectOnDown == button)
            { Navigation n = anchor.navigation; n.selectOnDown = next; anchor.navigation = n; }
            if (nextLinked && next != null && next.navigation.selectOnUp == button)
            { Navigation n = next.navigation; n.selectOnUp = anchor; next.navigation = n; }
            if (panel != null && extension != 0f) Grow(-extension);
            if (button != null) UnityEngine.Object.Destroy(button.gameObject);
            if (placeholder != null) placeholder.SetActive(true);
            placeholder = null;
            button = anchor = null; panel = null; root = null; nextLinked = false;
        }
    }
}
