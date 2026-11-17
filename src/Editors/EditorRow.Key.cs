using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class EditorRow
    {
        private T17Button rebind;
        private KeyCapture keyCapture;
        private void BuildKeyCode()
        {
            T17Button choices = Button("KeyList", "", 0, .65f, OpenKeyCodes);
            valueLabel = choices.GetComponentInChildren<T17Text>(true);
            rebind = Button("Rebind", Texts.Rebind, .66f, .80f, BeginKeyCapture);
        }
        private void OpenKeyCodes()
        {
            List<KeyCode> keys = new List<KeyCode>(); List<string> names = new List<string>();
            HashSet<int> seen = new HashSet<int>();
            AcceptableValueBase acceptable = Row.Entry.Description.AcceptableValues;
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!seen.Add((int)key) || (acceptable != null && !acceptable.IsValid(key))) continue;
                keys.Add(key); names.Add(ValueCodec.Format(Row, key));
            }
            page.Overlay.Choose(Row.Metadata.Name, names.ToArray(), delegate(int index) { Commit(keys[index]); });
            int current = keys.IndexOf((KeyCode)Row.Entry.BoxedValue);
            if (current >= 0) page.Overlay.FocusChoice(current);
        }
        private void BeginKeyCapture()
        {
            page.Overlay.Close(); CancelKeyCapture();
            keyCapture = Root.gameObject.AddComponent<KeyCapture>();
            keyCapture.Initialize(page.Domain, false,
                delegate(object value) { CancelKeyCapture(); Commit(value); },
                delegate { CancelKeyCapture(); page.CaptureCancelled(); });
            RefreshKeyCapture();
        }
        private void CancelKeyCapture()
        {
            KeyCapture old = keyCapture; keyCapture = null;
            if (old != null) { old.Release(); old.enabled = false; UnityEngine.Object.Destroy(old); }
            RefreshKeyCapture();
        }
        private void RefreshKeyCapture()
        { if (rebind != null) NativeUi.Label(rebind.transform, keyCapture == null ? Texts.Rebind : Texts.PressKey); }
    }
}
