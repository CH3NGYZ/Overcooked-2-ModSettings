using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // Full-canvas shield owns the click. Its dialog background absorbs inside clicks.
    internal sealed class OverlayDismiss : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform dialog;
        private T17EventSystem domain;
        private Action close;
        private Func<bool> canDismiss;
        private Func<Vector2, bool> contains;
        private int openedFrame;
        private bool pressedOutside;
        internal void Initialize(RectTransform bounds, T17EventSystem input, Action dismissed)
        { Initialize(bounds, input, dismissed, null, null); }
        internal void Initialize(RectTransform bounds, T17EventSystem input, Action dismissed, Func<bool> allowed, Func<Vector2, bool> hitTest)
        {
            dialog = bounds; domain = input; close = dismissed; canDismiss = allowed; contains = hitTest;
            openedFrame = Time.frameCount; pressedOutside = false;
        }
        public void OnPointerClick(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left || Inside(data.position)) return;
            data.Use(); Dismiss();
        }
        private void LateUpdate()
        {
            // Key capture deliberately disables the native input module; observe only its outside click.
            if (Time.frameCount <= openedFrame || domain == null || !domain.IsDisabled()) return;
            if (Input.GetMouseButtonDown(0)) pressedOutside = (canDismiss == null || canDismiss()) && !Inside(Input.mousePosition);
            if (!Input.GetMouseButtonUp(0)) return;
            bool dismiss = pressedOutside && !Inside(Input.mousePosition); pressedOutside = false;
            if (dismiss) Dismiss();
        }
        private bool Inside(Vector2 position)
        {
            if (contains != null) return contains(position);
            Canvas canvas = GetComponentInParent<Canvas>();
            GraphicRaycaster raycaster = GetComponentInParent<GraphicRaycaster>();
            Camera camera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null
                : raycaster != null ? raycaster.eventCamera : canvas.worldCamera;
            return dialog != null && RectTransformUtility.RectangleContainsScreenPoint(dialog, position, camera);
        }
        private void Dismiss()
        {
            if (canDismiss != null && !canDismiss()) return;
            Action action = close; close = null; if (action != null) action();
        }
    }
}
