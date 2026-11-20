using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx.Configuration;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyKeyEditors(NativePage page)
    {
        Transform row = page.Right.Content.Find("Setting_Types_enum");
        T17Button list = row.Find("KeyList").GetComponent<T17Button>();
        T17Button rebind = row.Find("Rebind").GetComponent<T17Button>();
        T17Button defaults = row.Find("Default").GetComponent<T17Button>();
        page.Right.Reveal(row as RectTransform); yield return null; yield return null;
        Rect name = BoundsIn(row.Find("Name") as RectTransform, row);
        Rect value = BoundsIn(list.GetComponent<RectTransform>(), row);
        Rect change = BoundsIn(rebind.GetComponent<RectTransform>(), row);
        Rect reset = BoundsIn(defaults.GetComponent<RectTransform>(), row);
        Check(value.xMin > name.xMax && change.xMin - value.xMax >= 6 && reset.xMin - change.xMax >= 6
            && change.width >= 100, "KeyCode row places single-key selector then rebind then defaults without overlap");
        Check(row.Find("Capture") == null && rebind.GetComponentInChildren<T17Text>().text == Texts.Rebind,
            "KeyCode uses separate selector and idle rebind button with no old capture slot");
        CheckRaycast(list, page.Domain, "KeyCode selector receives actual canvas raycasts");
        CheckRaycast(rebind, page.Domain, "KeyCode rebind receives actual canvas raycasts");
        Check(list.navigation.selectOnRight == rebind && rebind.navigation.selectOnLeft == list
            && rebind.navigation.selectOnRight == defaults, "KeyCode child navigation follows visible row order");
        File.WriteAllText(Path.Combine(output, "keycode-geometry.txt"), "Name=" + name + "\nValue=" + value + "\nRebind=" + change + "\nDefault=" + reset);
        object original = fixture["Types", "enum"].BoxedValue;
        Submit(list, page); yield return null; yield return null; yield return null;
        Transform content = page.Overlay.Root.Find("Dialog/Choices/Viewport/Content");
        HashSet<int> values = new HashSet<int>(); Transform target = null; bool singles = true;
        foreach (T17Button button in content.GetComponentsInChildren<T17Button>())
        {
            string label = button.GetComponentInChildren<T17Text>().text;
            KeyCode key = (KeyCode)Enum.Parse(typeof(KeyCode), label);
            singles &= values.Add((int)key) && label.IndexOf('+') < 0;
            if (key == KeyCode.F8) target = button.transform;
        }
        Check(singles, "KeyCode dropdown contains only distinct single keys with enum aliases deduplicated");
        T17Scrollbar bar = page.Overlay.Root.Find("Dialog/Choices/VerticalScrollbar").GetComponent<T17Scrollbar>();
        Check(target != null && values.Count > 100 && bar.size < 1 && bar.GetDomain() == page.Domain,
            "KeyCode dropdown shows scrollable single-key choices on the current input domain");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "keycode-list.png")); yield return new WaitForEndOfFrame(); yield return null;
        ExecuteEvents.Execute(page.Overlay.Root.gameObject, OutsideOverlay(page), ExecuteEvents.pointerClickHandler);
        yield return null; yield return null;
        Check(!page.Overlay.IsOpen && Equals(original, fixture["Types", "enum"].BoxedValue),
            "KeyCode dropdown outside dismissal preserves current key");
        Submit(list, page); yield return null; yield return null;
        content = page.Overlay.Root.Find("Dialog/Choices/Viewport/Content");
        foreach (T17Button button in content.GetComponentsInChildren<T17Button>())
            if (button.GetComponentInChildren<T17Text>().text == "F8") { Submit(button, page); break; }
        yield return null; yield return null;
        Check((KeyCode)fixture["Types", "enum"].BoxedValue == KeyCode.F8 && !page.Overlay.IsOpen
            && list.GetComponentInChildren<T17Text>().text == "F8 ▾", "KeyCode list selection immediately saves and updates the left value");
        Submit(rebind, page); yield return null; yield return null;
        KeyCapture capture = row.GetComponent<KeyCapture>();
        Check(capture != null && page.Domain.IsDisabled() && !page.Overlay.IsOpen
            && rebind.GetComponentInChildren<T17Text>().text == Texts.PressKey,
            "inline rebind changes label to press key and exclusively captures input without a secondary dialog");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "keycode-capture.png")); yield return new WaitForEndOfFrame(); yield return null;
        capture.AcceptKey(Shortcut.Key, new KeyCode[0]);
        Check((KeyCode)fixture["Types", "enum"].BoxedValue == KeyCode.F8 && capture.enabled,
            "reserved global-toggle key cannot be accidentally assigned by inline capture");
        capture.AcceptKey(KeyCode.K, new KeyCode[] { KeyCode.LeftControl }); yield return null; yield return null;
        Check((KeyCode)fixture["Types", "enum"].BoxedValue == KeyCode.K && !page.Domain.IsDisabled()
            && list.GetComponentInChildren<T17Text>().text == "K ▾" && rebind.GetComponentInChildren<T17Text>().text == Texts.Rebind,
            "inline capture persists only the single main key and restores its button and input domain");
        ConfigFile disk = new ConfigFile(fixture.ConfigFilePath, true);
        Check(disk.Bind("Types", "enum", KeyCode.None).Value == KeyCode.K, "captured KeyCode saves to its owning disk file immediately");
        Submit(rebind, page); yield return null; page.Cancel(); yield return null; yield return null;
        Check(!page.Domain.IsDisabled() && row.GetComponent<KeyCapture>() == null
            && (KeyCode)fixture["Types", "enum"].BoxedValue == KeyCode.K && rebind.GetComponentInChildren<T17Text>().text == Texts.Rebind,
            "cancel inline rebind releases capture and restores label without changing the saved key");
        Submit(defaults, page); yield return null;
        Check((KeyCode)fixture["Types", "enum"].BoxedValue == KeyCode.Space
            && list.GetComponentInChildren<T17Text>().text == "Space ▾", "KeyCode defaults restore selector and config together");
        Submit(rebind, page); yield return null;
        T17EventSystem domain = page.Domain;
        page.gameObject.SetActive(false); yield return null; yield return null;
        Check(!domain.IsDisabled() && row.GetComponent<KeyCapture>() == null
            && rebind.GetComponentInChildren<T17Text>().text == Texts.Rebind,
            "external page disable releases inline capture and restores idle label");
        Transform entrance = T17FrontendFlow.Instance.m_Rootmenu.transform.Find("WorldSpaceCanvas/Safezone_Rootmenu/RootTab/CustomizationOptions/ModSettingsEntry");
        Submit(entrance, page); yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
        row = page.Right.Content.Find("Setting_Types_enum"); rebind = row.Find("Rebind").GetComponent<T17Button>();
        Submit(rebind, page); yield return null;
        NativeShortcut shortcut = (NativeShortcut)HarmonyLib.AccessTools.Field(typeof(ModSettingsPlugin), "shortcut").GetValue(ModSettingsPlugin.Instance);
        shortcut.Tick(true); yield return null; yield return null;
        Check(!page.gameObject.activeSelf && !domain.IsDisabled() && row.GetComponent<KeyCapture>() == null,
            "global fixed-key close takes priority over inline rebind and releases its exact input owner");
        Submit(entrance, page); yield return new WaitForSecondsRealtime(.35f); yield return null; yield return null;
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
        IDisposable noDefault = ModSettingsPlugin.Instance.ConfigureEntryV1(fixture["Types", "enum"],
            new Dictionary<string, object> { { "HideDefaultButton", true } });
        yield return new WaitForSecondsRealtime(.35f); yield return null;
        row = page.Right.Content.Find("Setting_Types_enum");
        Check(row.Find("Default") == null && (row.Find("Rebind") as RectTransform).offsetMax.x > -15
            && row.Find("Rebind").GetComponent<Selectable>().navigation.selectOnRight == null,
            "KeyCode without defaults reclaims the right edge and has no hidden navigation target");
        noDefault.Dispose(); yield return new WaitForSecondsRealtime(.35f); yield return null;
    }
}
