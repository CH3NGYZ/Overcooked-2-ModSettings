using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OC2ModSettings
{
    // One scene-independent Canvas and input domain own the existing settings page.
    internal sealed class GlobalSettingsHost : IDisposable
    {
        internal GameObject Root { get; private set; }
        internal GlobalEventSystem Domain { get; private set; }
        internal Canvas Canvas { get; private set; }
        private bool visible, previousCursor;
        private CursorLockMode previousLock;
        private float nextOrderCheck;
        private bool checkingOrder;
        internal GlobalSettingsHost(GamepadUser user)
        {
            Root = new GameObject("ModSettingsGlobalHost", typeof(RectTransform));
            Root.SetActive(false); UnityEngine.Object.DontDestroyOnLoad(Root);
            try
            {
                Canvas = Root.AddComponent<Canvas>(); Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Canvas.sortingOrder = short.MaxValue;
                CanvasScaler scaler = Root.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
                Root.AddComponent<GraphicRaycaster>();
                // The whole viewport consumes pointer hits, including the native frame's margins.
                Image shield = Root.AddComponent<Image>(); shield.color = Color.clear; shield.raycastTarget = true;
                Domain = Root.AddComponent<GlobalEventSystem>();
                T17StandaloneInputModule module = Root.AddComponent<T17StandaloneInputModule>();
                module.forceModuleActive = true;
                Root.SetActive(true); Domain.SetAssignedGamepadUser(user);
                UnityEngine.Canvas.ForceUpdateCanvases(); Root.SetActive(false);
                SceneManager.sceneLoaded += SceneLoaded;
                UnityEngine.Canvas.willRenderCanvases += PlaceOnTop;
                PlaceOnTop();
            }
            catch { Dispose(); throw; }
        }
        internal void Show()
        {
            if (!visible)
            {
                previousCursor = Cursor.visible; previousLock = Cursor.lockState; visible = true;
            }
            PlaceOnTop(); Root.SetActive(true);
            Canvas.enabled = false; Canvas.enabled = true;
            nextOrderCheck = 0; Tick();
        }
        internal void Tick()
        {
            if (!visible) return;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        internal void Hide()
        {
            if (Root != null) Root.SetActive(false);
            if (!visible) return;
            visible = false; Cursor.lockState = previousLock; Cursor.visible = previousCursor;
        }
        private void SceneLoaded(Scene scene, LoadSceneMode mode) { PlaceOnTop(); }
        private void PlaceOnTop()
        {
            if (Canvas == null) return;
            int layer = 0, highest = int.MinValue;
            foreach (SortingLayer candidate in SortingLayer.layers)
                if (candidate.value >= highest) { layer = candidate.id; highest = candidate.value; }
            if (Canvas.sortingLayerID != layer) Canvas.sortingLayerID = layer;
            Canvas.sortingOrder = short.MaxValue;
            Root.transform.SetAsLastSibling();
            // The native spinner also uses 32767 (the serialized SInt16 limit). Re-register
            // only if a live equal-order Canvas actually renders later, including new scenes.
            if (!visible || checkingOrder || Time.realtimeSinceStartup < nextOrderCheck) return;
            nextOrderCheck = Time.realtimeSinceStartup + .2f; checkingOrder = true;
            try
            {
                foreach (Canvas other in UnityEngine.Object.FindObjectsOfType<Canvas>())
                    if (other != Canvas && other.isActiveAndEnabled && other.isRootCanvas && other.renderMode == RenderMode.ScreenSpaceOverlay
                        && other.sortingLayerID == Canvas.sortingLayerID && other.sortingOrder == Canvas.sortingOrder
                        && other.renderOrder > Canvas.renderOrder)
                    { Canvas.enabled = false; Canvas.enabled = true; break; }
            }
            finally { checkingOrder = false; }
        }
        public void Dispose()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            UnityEngine.Canvas.willRenderCanvases -= PlaceOnTop;
            Hide(); if (Root != null) UnityEngine.Object.Destroy(Root);
            Root = null; Canvas = null; Domain = null;
        }
    }
}
