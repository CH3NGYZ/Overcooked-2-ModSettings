using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        private readonly List<IDisposable> captures = new List<IDisposable>();
        private readonly Dictionary<string, Delegate> customCallbacks = new Dictionary<string, Delegate>();
        private float customHeight = 140;
        private void BuildCustom()
        {
            Height = 140;
            RectTransform content = NativeUi.Rect(Root, "CustomEditor", Vector2.zero, new Vector2(.8f, .62f));
            IDictionary<string, object> host = new Dictionary<string, object>();
            host["Version"] = 1;
            host["Domain"] = page.Domain;
            host["Commit"] = new Func<object, IDictionary<string, object>>(delegate(object value)
            {
                CommitResult result = page.CommitSetting(Row, value);
                if (!result.Success) page.ShowCommitError(Row, result.Error);
                else page.ClearError();
                return new Dictionary<string, object> { { "Success", result.Success }, { "ActualValue", result.ActualValue }, { "Error", result.Error } };
            });
            host["SetHeight"] = new Action<float>(delegate(float height)
            {
                customHeight = Mathf.Clamp(height, 80, 600);
                if (custom != null) Rearrange();
            });
            host["SetCallbacks"] = new Action<IDictionary<string, object>>(delegate(IDictionary<string, object> callbacks)
            {
                foreach (string key in new string[] { "Refresh", "Cancel", "Commit", "HasPending" })
                {
                    object callback; if (!callbacks.TryGetValue(key, out callback)) continue;
                    if ((key == "HasPending" || key == "Commit") ? !(callback is Func<bool>) : !(callback is Action))
                        throw new ArgumentException("Invalid instance callback: " + key);
                    customCallbacks[key] = (Delegate)callback;
                }
            });
            host["RegisterSelectable"] = new Action<Selectable>(delegate(Selectable selectable)
            {
                if (selectable == null || !selectable.transform.IsChildOf(Root)) throw new ArgumentException("Selectable must belong to its row.");
                if (!Controls.Contains(selectable)) Controls.Add(selectable);
                IT17EventHelper helper = selectable as IT17EventHelper;
                if (helper != null) helper.SetEventSystem(page.Domain);
            });
            host["Choose"] = new Action<string, string[], Action<int>>(page.Overlay.Choose);
            host["CaptureInput"] = new Func<IDisposable>(delegate
            {
                Suppressor suppressor = page.Domain == null ? null : page.Domain.Disable(Root);
                T17EventSystem capturedDomain = page.Domain;
                IDisposable lease = null;
                lease = new ReleaseAction(delegate
                {
                    if (suppressor != null) { if (capturedDomain != null) capturedDomain.ReleaseSuppressor(suppressor); else suppressor.Release(); }
                    captures.Remove(lease);
                });
                captures.Add(lease); return lease;
            });
            try
            {
                custom = ((Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>)renderer["Create"])(content, Row.Entry, host);
                if (custom == null) throw new InvalidOperationException("Renderer Create must return a disposable lifetime.");
            }
            catch (Exception error)
            {
                foreach (IDisposable capture in captures.ToArray()) capture.Dispose(); captures.Clear();
                customCallbacks.Clear(); renderer = null; NativeUi.ClearChildren(content); ReadOnly(); page.ShowError(error.Message);
            }
        }
    }
}
