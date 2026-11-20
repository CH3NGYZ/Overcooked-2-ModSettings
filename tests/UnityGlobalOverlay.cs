using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyGlobalOverlay(FrontendRootMenu root)
    {
        NativePage page = Shortcut.CachedPage;
        T17EventSystem native = root.CachedEventSystem;
        GamepadUser user = root.CurrentGamepadUser;
        List<T17EventSystem> free = (List<T17EventSystem>)AccessTools.Field(typeof(T17EventSystemsManager), "m_FreeEventSystems")
            .GetValue(T17EventSystemsManager.Instance);
        Check(!free.Contains(page.Domain) && T17EventSystemsManager.Instance.GetEventSystemForGamepadUser(user) == native,
            "independent domain never enters native free pool or steals primary assignment");
        bool priorReady = NativeHooks.ProfileReady;
        float previousScale = Time.timeScale;
        bool previousActive = root.gameObject.activeSelf;
        try
        {
            NativeHooks.ProfileReady = false; root.gameObject.SetActive(false); Time.timeScale = 0;
            IEnumerator press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
            Check(Shortcut.ActivePage == page && page.Domain.isActiveAndEnabled,
                "latched readiness opens with native root inactive, profile gate reset and timeScale zero");
            Check(page.transform.root.name == "ModSettingsGlobalHost"
                && page.gameObject.scene.name == "DontDestroyOnLoad", "page belongs to independent DDOL host");
            Check(Cursor.visible && Cursor.lockState == CursorLockMode.None, "global page exposes unlocked cursor");
            yield return new WaitForSecondsRealtime(.4f);
            IEnumerator raycast = VerifyVisibleTabRaycast(page, "global controls receive canvas raycasts without native root");
            while (raycast.MoveNext()) yield return raycast.Current;
            GraphicRaycaster raycaster = page.GetComponentInParent<GraphicRaycaster>();
            PointerEventData pointer = new PointerEventData(page.Domain) { position = new Vector2(1, 1) };
            List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(pointer, hits);
            Check(hits.Count > 0 && hits[0].module == raycaster, "full viewport shield prevents margin clicks reaching the game");
            WriteGeometry(page); ScreenCapture.CaptureScreenshot(Path.Combine(output, "global-rootless-paused.png"));
            yield return new WaitForEndOfFrame();
            page.Hide(true, false); yield return null;
        }
        finally
        {
            if (Shortcut.ActivePage != null) Shortcut.ActivePage.Hide(false, false);
            Time.timeScale = previousScale; NativeHooks.ProfileReady = priorReady;
            root.gameObject.SetActive(previousActive);
        }
        yield return null; yield return null;
        T17DialogBox dialog = T17DialogBoxManager.GetDialog(false);
        Check(dialog != null, "real native dialog acquired for global overlay regression");
        dialog.Initialize("Global overlay test", "Native dialog must remain open underneath settings", "OK", "", "",
            T17DialogBox.Symbols.Warning, false, false, false);
        dialog.Show(); yield return new WaitForSecondsRealtime(.4f);
        Check(dialog.IsActive && T17DialogBoxManager.HasAnyOpenDialogs(), "actual native dialog is active before shortcut");
        try
        {
            IEnumerator press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
            yield return new WaitForSecondsRealtime(.4f);
            Check(Shortcut.ActivePage == page && dialog.IsActive && !page.Domain.IsDisabled(),
                "global shortcut opens above active native dialog without dismissing it");
            CheckGlobalCanvases(page, "native dialog");
            IEnumerator raycast = VerifyVisibleTabRaycast(page, "settings raycasts outrank actual native dialog and spinner");
            while (raycast.MoveNext()) yield return raycast.Current;
            Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
            Transform toggle = page.Right.Content.Find("Setting_Types_boolean/Toggle");
            object before = fixture["Types", "boolean"].BoxedValue;
            Submit(toggle, page); yield return null;
            Check(!Equals(before, fixture["Types", "boolean"].BoxedValue) && dialog.IsActive,
                "settings edits still save while native dialog retains ownership underneath");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "global-native-dialog.png")); yield return new WaitForEndOfFrame();
            page.Hide(true, false); yield return null; yield return null;
            Check(dialog.IsActive, "closing global page restores existing dialog without confirming or cancelling it");
        }
        finally
        {
            if (Shortcut.ActivePage != null) Shortcut.ActivePage.Hide(false, false);
            dialog.Hide();
        }
        yield return null; yield return null;
    }
    private IEnumerator VerifyVisibleTabRaycast(NativePage page, string label)
    {
        T17Button tab = page.Left.Content.GetChild(0).GetComponent<T17Button>();
        RectTransform rect = (RectTransform)tab.transform;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        bool visible = RectTransformUtility.RectangleContainsScreenPoint(page.Left.Viewport, point);
        File.AppendAllText(Path.Combine(output, "raycasts.txt"), label + " (before reveal): " + tab.gameObject.name
            + " screen=" + point + " viewport=" + page.Left.Viewport.rect + " visible=" + visible
            + " offset=" + page.Left.Content.anchoredPosition.y + Environment.NewLine);
        // The cached list can reopen at a nonzero offset. Probe a hittable point, not a clipped tab.
        if (!visible)
        {
            page.Left.Reveal(rect);
            yield return null; yield return null;
        }
        CheckRaycast(tab, page.Domain, label);
    }
    private void CheckGlobalCanvases(NativePage page, string context)
    {
        Canvas owner = page.GetComponentInParent<Canvas>();
        string report = context + " ownLayer=" + owner.sortingLayerID + " order=" + owner.sortingOrder
            + " renderOrder=" + owner.renderOrder + Environment.NewLine;
        Check(owner.renderMode == RenderMode.ScreenSpaceOverlay && owner.sortingOrder == short.MaxValue,
            context + ": global Canvas uses overlay and maximum serialized sorting order");
        int ownerLayer = SortingLayer.GetLayerValueFromID(owner.sortingLayerID);
        foreach (Canvas other in UnityEngine.Object.FindObjectsOfType<Canvas>())
        {
            if (other == owner || !other.isActiveAndEnabled || !other.isRootCanvas || other.renderMode != RenderMode.ScreenSpaceOverlay) continue;
            int otherLayer = SortingLayer.GetLayerValueFromID(other.sortingLayerID);
            report += other.name + " layer=" + otherLayer + " order=" + other.sortingOrder + " renderOrder=" + other.renderOrder + Environment.NewLine;
            Check(ownerLayer > otherLayer || ownerLayer == otherLayer && (owner.sortingOrder > other.sortingOrder
                || owner.sortingOrder == other.sortingOrder && owner.renderOrder >= other.renderOrder),
                context + ": settings render after " + other.name);
        }
        File.AppendAllText(Path.Combine(output, "global-canvas-order.txt"), report);
    }
}
