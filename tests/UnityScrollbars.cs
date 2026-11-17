using System.Collections;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyScrollbars(NativePage page)
    {
        yield return null; Canvas.ForceUpdateCanvases(); yield return null;
        foreach (ScrollColumn column in new ScrollColumn[] { page.Left, page.Right })
        {
            string name = column == page.Left ? "left" : "right";
            ScrollColumn other = column == page.Left ? page.Right : page.Left;
            T17Scrollbar bar = column.Bar;
            Check(bar != null && bar.gameObject.activeInHierarchy && column.Scroll.verticalScrollbar == bar
                && bar.GetDomain() == page.Domain, name + " column has its own native scrollbar bound to current input domain");
            Rect track = BoundsIn(bar.GetComponent<RectTransform>(), column.Scroll.transform);
            Rect view = BoundsIn(column.Viewport, column.Scroll.transform);
            Check(track.xMin - view.xMax >= 4 && track.height > 0, name + " scrollbar reserves its own gutter outside content viewport");
            float expected = Mathf.Clamp01(column.Viewport.rect.height / column.Content.rect.height);
            Check(Mathf.Abs(bar.size - expected) < .01f, name + " scrollbar thumb reflects visible content proportion");
            CheckRaycast(bar, page.Domain, name + " scrollbar track is reachable through native canvas");
            Vector2 before = column.Content.anchoredPosition, otherBefore = other.Content.anchoredPosition;
            GameObject focus = page.Domain.currentSelectedGameObject;
            if (bar.size < .99f)
            {
                Canvas canvas = bar.GetComponentInParent<Canvas>();
                GraphicRaycaster raycaster = bar.GetComponentInParent<GraphicRaycaster>();
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster.eventCamera;
                Vector2 start = RectTransformUtility.WorldToScreenPoint(camera, bar.handleRect.TransformPoint(bar.handleRect.rect.center));
                RectTransform rail = bar.GetComponent<RectTransform>();
                Vector2 finish = RectTransformUtility.WorldToScreenPoint(camera,
                    rail.TransformPoint(new Vector3(rail.rect.center.x, rail.rect.yMin + rail.rect.height * .2f, 0)));
                PointerEventData pointer = new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
                    position = start, pressPosition = start, pointerPressRaycast = new RaycastResult { module = raycaster } };
                ExecuteEvents.Execute(bar.gameObject, pointer, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.Execute(bar.gameObject, pointer, ExecuteEvents.initializePotentialDrag);
                ExecuteEvents.Execute(bar.gameObject, pointer, ExecuteEvents.beginDragHandler);
                pointer.position = finish;
                ExecuteEvents.Execute(bar.gameObject, pointer, ExecuteEvents.dragHandler);
                ExecuteEvents.Execute(bar.gameObject, pointer, ExecuteEvents.endDragHandler);
                ExecuteEvents.Execute(bar.gameObject, pointer, ExecuteEvents.pointerUpHandler);
                yield return null; yield return null;
                Check(column.Content.anchoredPosition.y > before.y + 10, name + " native scrollbar thumb drag scrolls content down");
                Check(Vector2.Distance(other.Content.anchoredPosition, otherBefore) < .1f, name + " scrollbar drag leaves other column unchanged");
                Check(page.Domain.currentSelectedGameObject == focus, name + " scrollbar drag preserves existing settings focus");
                bar.value = 1; yield return null;
                Check(Mathf.Abs(column.Content.anchoredPosition.y) < 1, name + " scrollbar can return content to top");
            }
            else Check(Mathf.Abs(column.Content.anchoredPosition.y) < 1 && Mathf.Abs(bar.size - 1) < .01f,
                name + " non-overflowing scrollbar spans full rail and content remains at top");
            column.Content.anchoredPosition = before;
            File.AppendAllText(Path.Combine(output, "scrollbars.txt"), name + " track=" + track + " viewport=" + view
                + " thumbSize=" + bar.size + " expected=" + expected + "\n");
        }
        yield return null;
    }
}
