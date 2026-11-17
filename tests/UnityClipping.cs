using System.Collections;
using System.IO;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyTextClipping(NativePage page)
    {
        foreach (ScrollColumn column in new ScrollColumn[] { page.Left, page.Right })
        {
            Vector2 previous = column.Content.anchoredPosition;
            column.Content.anchoredPosition = new Vector2(0, 91);
            Canvas.ForceUpdateCanvases(); yield return null; yield return new WaitForEndOfFrame();
            foreach (T17Text text in column.Content.GetComponentsInChildren<T17Text>())
            {
                Material material = text.materialForRendering;
                File.AppendAllText(Path.Combine(output, "text-clipping.txt"), column.Scroll.name + "/" + text.name
                    + " shader=" + material.shader.name + " maskable=" + text.maskable
                    + " culled=" + text.canvasRenderer.cull + "\n");
                Check(text.maskable && material.shader == Graphic.defaultGraphicMaterial.shader,
                    "scrolling text uses the UI shader that supports rectangular clipping: " + text.name);
                Rect bounds = BoundsIn(text.rectTransform, column.Viewport);
                if (!bounds.Overlaps(column.Viewport.rect))
                    Check(text.canvasRenderer.cull, "text completely outside viewport is culled: " + text.name);
            }
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "clipping-" + column.Scroll.name + ".png"));
            yield return new WaitForEndOfFrame(); yield return null;
            column.Content.anchoredPosition = previous;
        }
        yield return null;
    }
}
