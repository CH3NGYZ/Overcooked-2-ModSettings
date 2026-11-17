using System.Collections;
using System.Collections.Generic;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyPageDismiss()
    {
        NativePage page = Shortcut.CachedPage;
        foreach (bool top in new bool[] { true, false })
        {
            Shortcut.Open(); yield return new WaitForSecondsRealtime(.35f);
            RectTransform body = page.transform.Find("SettingsBody") as RectTransform;
            Canvas canvas = page.GetComponentInParent<Canvas>();
            PointerEventData inside = new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null,
                    body.TransformPoint(new Vector3(0, body.rect.yMin + body.rect.height * .15f, 0))) };
            OverlayDismiss dismiss = canvas.GetComponent<OverlayDismiss>();
            dismiss.OnPointerClick(inside);
            Check(Shortcut.ActivePage == page, "clicking empty panel footer keeps global page open");
            PointerEventData title = new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
                position = RectTransformUtility.WorldToScreenPoint(null, body.Find("HeaderBacker").TransformPoint(Vector3.zero)) };
            dismiss.OnPointerClick(title);
            Check(Shortcut.ActivePage == page, "native title remains inside the clickable settings panel");
            PointerEventData outside = new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
                position = new Vector2(Screen.width * .8f, Screen.height * (top ? .95f : .05f)) };
            List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(outside, hits);
            List<string> trace = new List<string>(); foreach (RaycastResult hit in hits) trace.Add(hit.gameObject.name);
            File.AppendAllText(Path.Combine(output, "page-dismiss.txt"), (top ? "top" : "bottom")
                + " hits=" + string.Join(",", trace.ToArray()) + "\n");
            Check(hits.Count > 0 && hits[0].module == canvas.GetComponent<GraphicRaycaster>()
                && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == canvas.gameObject,
                (top ? "top" : "bottom") + " outside panel is owned by the global shield");
            if (top)
            {
                page.Overlay.Choose("Dismiss ownership", new string[] { "Choice" }, delegate(int ignored) { });
                yield return null; yield return null;
                hits.Clear(); page.Domain.RaycastAll(outside, hits);
                Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)
                    == page.Overlay.Root.gameObject, "outside click belongs to the secondary shield while a dialog is open");
                ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, outside, ExecuteEvents.pointerClickHandler);
                yield return null; yield return null;
                Check(!page.Overlay.IsOpen && Shortcut.ActivePage == page,
                    "one outside click closes only the secondary dialog without closing the page");
                hits.Clear(); page.Domain.RaycastAll(outside, hits);
            }
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, outside, ExecuteEvents.pointerClickHandler);
            yield return null; yield return null;
            Check(Shortcut.ActivePage == null && !page.Domain.IsDisabled(),
                (top ? "top" : "bottom") + " outside click closes the page and releases its input");
        }
        Shortcut.Open(); yield return new WaitForSecondsRealtime(.35f);
        T17Button close = page.transform.Find("SettingsBody/CloseModSettings").GetComponent<T17Button>();
        CheckRaycast(close, page.Domain, "header close button has a real reachable hit target");
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        Check(search.navigation.selectOnRight == close && close.navigation.selectOnLeft == search,
            "native horizontal navigation connects search and header close");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "header-close.png")); yield return new WaitForEndOfFrame(); yield return null;
        Submit(close, page); yield return null;
        Check(Shortcut.ActivePage == null, "header close button closes the same global page");
    }
    private IEnumerator VerifyOverlayDismiss(NativePage page)
    {
        NativeTextInput search = page.transform.Find("SettingsBody/Search").GetComponent<NativeTextInput>();
        CheckRaycast(search, page.Domain, "top-right search receives real canvas raycasts");
        object old = fixture["Types", "choice"].BoxedValue;
        Submit(page.Right.Content.Find("Setting_Types_choice/Dropdown"), page); yield return null; yield return null;
        Transform root = page.Overlay.Root;
        RectTransform dialog = root.Find("Dialog") as RectTransform;
        Canvas canvas = root.GetComponentInParent<Canvas>();
        GraphicRaycaster raycaster = root.GetComponentInParent<GraphicRaycaster>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster.eventCamera;
        PointerEventData inside = new PointerEventData(page.Domain) { position = RectTransformUtility.WorldToScreenPoint(camera,
            dialog.TransformPoint(new Vector3(dialog.rect.xMin + 3, dialog.rect.center.y, 0))), button = PointerEventData.InputButton.Left };
        ExecuteEvents.ExecuteHierarchy(dialog.gameObject, inside, ExecuteEvents.pointerClickHandler);
        Check(page.Overlay.IsOpen, "clicking blank area inside secondary dialog keeps it open");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "secondary-dialog.png")); yield return new WaitForEndOfFrame(); yield return null;
        PointerEventData pointer = OutsideOverlay(page);
        List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<OverlayDismiss>() != null,
            "outside dialog raycast belongs to full-canvas shield rather than underlying setting");
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler); yield return null; yield return null;
        Check(!page.Overlay.IsOpen && Equals(old, fixture["Types", "choice"].BoxedValue),
            "outside click dismisses choice overlay without selecting or changing underlying config");
        Submit(page.Right.Content.Find("Setting_Types_shortcut/Capture"), page); yield return null; yield return null;
        object key = fixture["Types", "shortcut"].BoxedValue;
        Check(page.Domain.IsDisabled(), "capture owns native input before outside dismissal");
        ExecuteEvents.Execute(page.Overlay.Root.gameObject, OutsideOverlay(page), ExecuteEvents.pointerClickHandler);
        yield return null; yield return null;
        Check(!page.Overlay.IsOpen && !page.Domain.IsDisabled() && Equals(key, fixture["Types", "shortcut"].BoxedValue),
            "outside capture dismissal releases input without committing a key");
    }
    private PointerEventData OutsideOverlay(NativePage page)
    {
        RectTransform shield = page.Overlay.Root as RectTransform;
        Canvas canvas = shield.GetComponentInParent<Canvas>();
        GraphicRaycaster raycaster = shield.GetComponentInParent<GraphicRaycaster>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster.eventCamera;
        return new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(camera,
                shield.TransformPoint(new Vector3(shield.rect.xMin + shield.rect.width * .05f, shield.rect.center.y, 0))) };
    }
}
