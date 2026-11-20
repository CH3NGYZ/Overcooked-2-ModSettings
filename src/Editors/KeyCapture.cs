using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ModSettings
{
    internal sealed class KeyCapture : MonoBehaviour
    {
        internal enum Mode { Single, Combination, GlobalToggle }
        private static readonly KeyCode[] Keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        private static readonly KeyCode[] Modifiers = new KeyCode[] { KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift,
            KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftCommand, KeyCode.RightCommand };
        private Suppressor suppressor;
        private T17EventSystem domain;
        private NativeShortcut toggle;
        private Mode mode;
        private int ignoreThroughFrame;
        private Action<object> selected;
        private Action cancel;
        private ILogicalButton cancelButton;
        internal T17Button ClearButton;

        internal void Initialize(NativePage page, Mode kind, Action<object> result, Action cancelled)
        {
            domain = page.Domain; toggle = page.Shortcut; mode = kind; selected = result; cancel = cancelled;
            // Global bindings exclude submit keys and can listen on the next frame.
            ignoreThroughFrame = Time.frameCount + (mode == Mode.GlobalToggle ? 0 : 1);
            cancelButton = PlayerInputLookup.GetEngagedButton(PlayerInputLookup.LogicalButtonID.UICancel, PlayerInputLookup.Player.One, PadSide.Both);
            if (domain != null) suppressor = domain.Disable(this);
        }
        private void Update()
        {
            if (Time.frameCount <= ignoreThroughFrame || (mode != Mode.GlobalToggle && toggle.Pressed)) return;
            if (Input.GetKeyUp(KeyCode.Escape) || (cancelButton != null && cancelButton.JustReleased()))
            { if (cancelButton != null) { cancelButton.ClaimPressEvent(); cancelButton.ClaimReleaseEvent(); } cancel(); return; }
            if (mode == Mode.GlobalToggle && NativeShortcut.CommandModifierHeld) return;
            if (mode != Mode.GlobalToggle && (Input.GetKeyDown(KeyCode.Delete) || Input.GetKeyDown(KeyCode.Backspace)
                || (Input.GetMouseButtonUp(0) && ClearButton != null && RectTransformUtility.RectangleContainsScreenPoint(
                    ClearButton.GetComponent<RectTransform>(), Input.mousePosition, EventCamera()))))
            { selected(mode == Mode.Combination ? (object)new KeyboardShortcut(KeyCode.None) : KeyCode.None); return; }
            foreach (KeyCode key in Keys)
            {
                if (!CanAccept(key) || !Input.GetKeyDown(key)) continue;
                KeyCode[] modifiers = null;
                if (mode == Mode.Combination)
                {
                    List<KeyCode> held = new List<KeyCode>();
                    foreach (KeyCode modifier in Modifiers) if (Input.GetKey(modifier)) held.Add(modifier);
                    modifiers = held.ToArray();
                }
                AcceptKey(key, modifiers); return;
            }
        }
        private bool CanAccept(KeyCode key)
        {
            if (mode == Mode.GlobalToggle) return NativeShortcut.IsBindable(key);
            return key != KeyCode.None && key != KeyCode.Escape
                && (key < KeyCode.Mouse0 || key > KeyCode.Mouse6)
                && (mode != Mode.Combination || Array.IndexOf(Modifiers, key) < 0);
        }
        internal void AcceptKey(KeyCode key, KeyCode[] modifiers)
        {
            if (!isActiveAndEnabled || !CanAccept(key) || (mode != Mode.GlobalToggle && key == toggle.Key)) return;
            selected(mode == Mode.Combination ? (object)new KeyboardShortcut(key, modifiers) : key);
        }
        internal void Stop() { Release(); enabled = false; UnityEngine.Object.Destroy(this); }
        private void Release()
        { if (suppressor != null) { if (domain != null) domain.ReleaseSuppressor(suppressor); else suppressor.Release(); suppressor = null; } }
        private void OnDisable() { Release(); }
        private void OnDestroy() { Release(); }
        private Camera EventCamera()
        {
            Canvas canvas = ClearButton.GetComponentInParent<Canvas>();
            GraphicRaycaster raycaster = ClearButton.GetComponentInParent<GraphicRaycaster>();
            return canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
                : raycaster != null ? raycaster.eventCamera : canvas.worldCamera;
        }
    }
}
