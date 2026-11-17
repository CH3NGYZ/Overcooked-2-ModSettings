using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace ModSettingsExample
{
    // A per-row lifetime, including callbacks and delegates, is created on each page build.
    public sealed class NativeBooleanRenderer : IDisposable
    {
        private readonly GameObject root;
        private readonly T17Button button;
        private readonly Text label;
        private readonly ConfigEntryBase entry;
        private readonly Func<object, IDictionary<string, object>> commit;
        public static IDictionary<string, object> Protocol()
        {
            return new Dictionary<string, object> {
                { "Version", 1 },
                { "Create", new Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>(Create) }
            };
        }
        private static IDisposable Create(RectTransform parent, ConfigEntryBase entry, IDictionary<string, object> host)
        { return new NativeBooleanRenderer(parent, entry, host); }
        private NativeBooleanRenderer(RectTransform parent, ConfigEntryBase config, IDictionary<string, object> host)
        {
            if (config.SettingType != typeof(bool)) throw new ArgumentException("Boolean setting required.");
            entry = config; commit = (Func<object, IDictionary<string, object>>)host["Commit"];
            root = new GameObject("CustomBoolean", typeof(RectTransform), typeof(Image), typeof(T17Button));
            RectTransform rect = (RectTransform)root.transform; rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(.15f, .25f, .35f);
            button = root.GetComponent<T17Button>(); button.targetGraphic = root.GetComponent<Image>();
            GameObject text = new GameObject("Label", typeof(RectTransform), typeof(Text)); text.transform.SetParent(root.transform, false);
            RectTransform bounds = (RectTransform)text.transform; bounds.anchorMin = Vector2.zero; bounds.anchorMax = Vector2.one;
            bounds.offsetMin = bounds.offsetMax = Vector2.zero;
            label = text.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 24; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            button.onClick.AddListener(delegate { Toggle(); });
            ((Action<Selectable>)host["RegisterSelectable"])(button);
            ((Action<float>)host["SetHeight"])(120);
            ((Action<IDictionary<string, object>>)host["SetCallbacks"])(new Dictionary<string, object> {
                { "Refresh", new Action(Refresh) }, { "Cancel", new Action(delegate {}) },
                { "HasPending", new Func<bool>(delegate { return false; }) }, { "Commit", new Func<bool>(delegate { return true; }) }
            });
            Refresh();
        }
        private void Toggle() { commit(!(bool)entry.BoxedValue); Refresh(); }
        private void Refresh() { label.text = (bool)entry.BoxedValue ? "ON" : "OFF"; }
        public void Dispose() { button.onClick.RemoveAllListeners(); UnityEngine.Object.Destroy(root); }
    }
}
