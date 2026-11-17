using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private PinnedMods pinned;
        private int pinRevision;
        private ModPin hoveredPin;
        private int CompareMods(ModConfig a, ModConfig b)
        { return pinned.Compare(a, b); }
        internal void TogglePin(string guid)
        {
            if (PinnedMods.IsFixed(guid)) return;
            CommitResult result = pinned.Toggle(guid); ReorderPinnedTabs();
            if (!result.Success) ShowError(result.Error);
        }
        private void ReorderPinnedTabs()
        {
            mods.Sort(CompareMods); pinRevision = pinned.Revision;
            Dictionary<string, int> order = new Dictionary<string, int>();
            for (int i = 0; i < mods.Count; i++) order[mods[i].Guid] = i;
            tabs.Sort(delegate(T17Button a, T17Button b) { return order[a.GetComponent<ModPin>().Guid].CompareTo(order[b.GetComponent<ModPin>().Guid]); });
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].transform.SetSiblingIndex(i);
                NativeUi.Box(tabs[i].GetComponent<RectTransform>(), 4, i * 72, Mathf.Max(100, Left.Viewport.rect.width - 8), 68);
            }
            WireNavigation(); UpdateSelectionMarker(); RefreshPins();
        }
        internal void PinSelected() { onRight = false; RefreshPins(); }
        internal void HoverPin(ModPin pin) { hoveredPin = pin; RefreshPins(); }
        internal void LeavePin(ModPin pin) { if (hoveredPin == pin) hoveredPin = null; RefreshPins(); }
        internal void RefreshPins()
        {
            GameObject selected = Domain == null ? null : Domain.currentSelectedGameObject;
            ModPin focused = selected == null ? null : selected.GetComponentInParent<ModPin>();
            ModPin visible = focused != null && focused.Button != null && selected == focused.Button.gameObject ? focused : (hoveredPin != null ? hoveredPin : focused);
            foreach (T17Button tab in tabs)
            {
                ModPin pin = tab.GetComponent<ModPin>(); bool isPinned = pinned.Contains(pin.Guid);
                pin.Show(isPinned || pin == visible, isPinned);
            }
        }
    }
}
