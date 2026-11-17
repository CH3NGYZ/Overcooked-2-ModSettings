using System;
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
            T17Button choices = Button("KeyList", "", 0, .65f, OpenChoices);
            valueLabel = choices.GetComponentInChildren<T17Text>(true);
            rebind = Button("Rebind", Texts.Rebind, .66f, .80f, BeginKeyCapture);
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
