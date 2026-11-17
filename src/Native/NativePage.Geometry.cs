using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private bool ContainsPanel(Vector2 screenPoint)
        {
            RectTransform body = transform.Find("SettingsBody") as RectTransform;
            RectTransform banner = body.Find("HeaderBacker") as RectTransform;
            if (RectTransformUtility.RectangleContainsScreenPoint(banner, screenPoint, null)) return true;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(body, screenPoint, null, out local)) return false;
            Rect visible = NativePageTemplate.VisibleBody, rect = body.rect;
            return Rect.MinMaxRect(rect.xMin + visible.xMin * rect.width, rect.yMin + visible.yMin * rect.height,
                rect.xMin + visible.xMax * rect.width, rect.yMin + visible.yMax * rect.height).Contains(local);
        }
        private static void ResizeFrame(RectTransform body)
        {
            RectTransform canvas = body.parent as RectTransform;
            float width = canvas == null || canvas.rect.width <= 0 ? body.rect.width : canvas.rect.width;
            float height = canvas == null || canvas.rect.height <= 0 ? 1080 : canvas.rect.height;
            // The stock frame already spans the full canvas width. Grow vertically within its safe area.
            body.sizeDelta = new Vector2(Mathf.Min(width, body.rect.width + 100), Mathf.Min(height * .94f, body.rect.height + 180));
            RectTransform banner = body.Find("HeaderBacker") as RectTransform;
            banner.anchorMin = new Vector2(0, 1); banner.anchorMax = new Vector2(.58f, 1);
            banner.pivot = new Vector2(.5f, 1); banner.anchoredPosition = new Vector2(0, -12);
            banner.sizeDelta = new Vector2(30, 150);
            RectTransform title = banner.Find("Header") as RectTransform;
            title.anchorMin = Vector2.zero; title.anchorMax = Vector2.one; title.pivot = new Vector2(.5f, .5f);
            title.offsetMin = new Vector2(50, 15); title.offsetMax = new Vector2(-50, -15);
        }
    }
}
