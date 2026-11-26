using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private const float HeaderControlAnchor = .89f, HeaderControlHeight = 46, ContentTopGap = 10;
        private const float ContentLeftAnchor = .035f, ModTabInset = 4;
        private T17Text bannerTitle;

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
        }
        private static void PlaceHeaderControl(RectTransform rect, float start, float end, float left, float right)
        {
            rect.anchorMin = new Vector2(start, HeaderControlAnchor); rect.anchorMax = new Vector2(end, HeaderControlAnchor);
            rect.offsetMin = new Vector2(left, -HeaderControlHeight * .5f);
            rect.offsetMax = new Vector2(right, HeaderControlHeight * .5f);
        }
        private void BuildBanner(Transform body)
        {
            RectTransform banner = body.Find("HeaderBacker") as RectTransform;
            bannerTitle = banner.Find("Header").GetComponent<T17Text>();
            // Keep the stock decorations but hide inherited labels such as the game's build number.
            foreach (T17Text text in GetComponentsInChildren<T17Text>(true))
                if (text != bannerTitle) text.gameObject.SetActive(false);
            banner.anchorMin = new Vector2(0, 1); banner.anchorMax = new Vector2(.58f, 1);
            banner.pivot = new Vector2(.5f, 1); banner.anchoredPosition = new Vector2(0, -12);
            banner.sizeDelta = new Vector2(30, 150);

            const float versionRightInset = 50, versionWidth = 102, titleInset = versionRightInset + versionWidth + 16;
            T17Text version = NativeUi.Text(BodyTextTemplate, banner, "PluginVersion", "v" + ModSettingsPlugin.Version, 28);
            RectTransform stamp = version.rectTransform;
            stamp.anchorMin = stamp.anchorMax = new Vector2(1, 0);
            stamp.offsetMin = new Vector2(-versionRightInset - versionWidth, 40);
            stamp.offsetMax = new Vector2(-versionRightInset, 78);
            version.alignment = TextAnchor.MiddleRight; version.color = bannerTitle.color;

            RectTransform title = bannerTitle.rectTransform;
            title.anchorMin = Vector2.zero; title.anchorMax = Vector2.one; title.pivot = new Vector2(.5f, .5f);
            title.offsetMin = new Vector2(titleInset, 26); title.offsetMax = new Vector2(-titleInset, -24);
            bannerTitle.alignment = TextAnchor.MiddleCenter; bannerTitle.raycastTarget = false;
            bannerTitle.horizontalOverflow = HorizontalWrapMode.Wrap; bannerTitle.verticalOverflow = VerticalWrapMode.Truncate;
            bannerTitle.resizeTextForBestFit = true; bannerTitle.resizeTextMinSize = 36; bannerTitle.resizeTextMaxSize = 80;
            NativeUi.Label(title, Texts.PanelTitle);
        }
    }
}
