using System.Collections;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyPins(NativePage page)
    {
        T17Button active = page.Left.Content.Find("Mod_fixture.00").GetComponent<T17Button>();
        ModPin pin = active.GetComponent<ModPin>();
        Check(pin.Button.GetComponent<RectTransform>().rect.size == new Vector2(42, 42)
            && pin.Button.transform.Find("Icon").GetComponent<RectTransform>().rect.size == new Vector2(28, 28), "pin matches reference 42 UI hit target and 28 UI icon");
        if (fixtureCount < 2) yield break;
        string guid = "fixture." + (fixtureCount - 1).ToString("D2");
        ModPin candidate = page.Left.Content.Find("Mod_" + guid).GetComponent<ModPin>();
        string[] originalOrder = new string[page.Left.Content.childCount];
        for (int i = 0; i < originalOrder.Length; i++) originalOrder[i] = page.Left.Content.GetChild(i).name;
        float left = page.Left.Content.anchoredPosition.y, right = page.Right.Content.anchoredPosition.y;
        Transform activeRow = page.Right.Content.Find("Setting_Types_boolean");
        Transform fixedRow = page.Left.Content.Find("Mod_" + PinnedMods.FixedFirstGuid);
        if (fixedRow != null)
        {
            Check(fixedRow.GetSiblingIndex() == 0 && fixedRow.GetComponent<ModPin>().Button == null && fixedRow.Find("Pin") == null,
                "HostUtilities is fixed first without a pin control");
            string before = File.ReadAllText(ModSettingsPlugin.Instance.Config.ConfigFilePath);
            page.TogglePin(PinnedMods.FixedFirstGuid);
            Check(!ModSettingsPlugin.Instance.Pins.Contains(PinnedMods.FixedFirstGuid)
                && File.ReadAllText(ModSettingsPlugin.Instance.Config.ConfigFilePath) == before,
                "fixed HostUtilities row ignores attempted toggles without writing configuration");
            T17Button host = fixedRow.GetComponent<T17Button>();
            page.Left.Reveal(fixedRow as RectTransform); page.Domain.SetSelectedGameObject(host.gameObject); yield return null; yield return null;
            Selectable rightTarget = host.navigation.selectOnRight;
            Move(host, page, MoveDirection.Right, Vector2.right); yield return null; yield return null;
            Check(rightTarget != null && page.Domain.currentSelectedGameObject == rightTarget.gameObject,
                "fixed HostUtilities row navigates directly right without a missing pin target");
            page.Domain.SetSelectedGameObject(active.gameObject); yield return null; yield return null;
        }
        left = page.Left.Content.anchoredPosition.y; right = page.Right.Content.anchoredPosition.y;
        Submit(candidate.Button, page); yield return null; yield return null;
        Check(ModSettingsPlugin.Instance.Pins.Contains(guid) && ModDisplayTabOrderIsStable(page, ModSettingsPlugin.Instance.Pins),
            "pin moves owner into the GUID-sorted pinned group, below fixed HostUtilities and before ordinary MODs");
        Check(activeRow != null && page.Right.Content.Find("Setting_Types_boolean") == activeRow,
            "pinning a different MOD does not activate it");
        Check(Mathf.Abs(left - page.Left.Content.anchoredPosition.y) < 1 && Mathf.Abs(right - page.Right.Content.anchoredPosition.y) < 1,
            "pin reorder preserves independent column scroll positions");
        ConfigFile persisted = new ConfigFile(ModSettingsPlugin.Instance.Config.ConfigFilePath, false);
        using (PinnedMods reloaded = new PinnedMods(persisted)) Check(reloaded.Contains(guid), "pin GUID persists through a fresh ConfigFile read");
        Image image = candidate.Button.transform.Find("Icon").GetComponent<Image>();
        Check(image.sprite != null && image.color.r > .95f && image.color.b < .4f,
            "pinned MOD uses filled gold reference icon");
        CheckRaycast(candidate.Button, page.Domain, "pinned MOD icon is pointer reachable");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "settings-pinned.png")); yield return new WaitForEndOfFrame(); yield return null;
        Submit(candidate.Button, page); yield return null; yield return null;
        bool restored = !ModSettingsPlugin.Instance.Pins.Contains(guid) && page.Left.Content.childCount == originalOrder.Length;
        for (int i = 0; restored && i < originalOrder.Length; i++) restored &= page.Left.Content.GetChild(i).name == originalOrder[i];
        Check(restored, "unpin restores prior stable order while preserving existing user pins");
        ExecuteEvents.Execute(candidate.Owner.gameObject, new PointerEventData(page.Domain), ExecuteEvents.pointerEnterHandler);
        Check(candidate.Button.GetComponent<CanvasGroup>().alpha == 1, "hover reveals unpinned MOD icon");
        ExecuteEvents.Execute(candidate.Owner.gameObject, new PointerEventData(page.Domain), ExecuteEvents.pointerExitHandler);
        Check(candidate.Button.GetComponent<CanvasGroup>().alpha == 0 && !candidate.Button.GetComponent<CanvasGroup>().blocksRaycasts,
            "hidden unpinned icon yields pointer hits to its row");
        page.Domain.SetSelectedGameObject(active.gameObject); yield return null; yield return null;
        ModPin selected = active.GetComponent<ModPin>();
        Move(active, page, MoveDirection.Right, Vector2.right); yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == selected.Button.gameObject && selected.Button.GetComponent<CanvasGroup>().alpha == 1,
            "native direction focuses and reveals selected MOD pin");
        Move(selected.Button, page, MoveDirection.Left, Vector2.left); yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == active.gameObject, "native left returns from pin to MOD row");
    }
}
