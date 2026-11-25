using System;
using System.Collections;
using System.IO;
using OC2ModSettings;
using OC2ModSettings.SDK;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifyModDisplayNames(NativePage page)
    {
        const string owner = "fixture.00", actionOwner = "fixture.display-name.action";
        const string firstName = "ZZ Runtime Name", secondName = "AA 测试设置", actionName = "Named action fixture";
        ModSettingsClient client = new ModSettingsClient();
        Check(client.Connect() && client.SupportsModDisplayName && client.SupportsActions,
            "the elected real settings provider exposes display-name registration through the SDK");
        int actionCalls = 0;
        using (IDisposable action = client.RegisterAction(actionOwner, "display-name-test",
            new ModSettingsAction("Run fixture action", delegate { actionCalls++; })))
        {
            yield return new WaitForSecondsRealtime(.35f);
            RectTransform activeTab = page.Left.Content.Find("Mod_" + owner) as RectTransform;
            RectTransform actionTab = page.Left.Content.Find("Mod_" + actionOwner) as RectTransform;
            Check(activeTab != null && actionTab != null && Caption(actionTab) == actionOwner,
                "registered config and action-only owners are present; the action-only fallback is its GUID");
            PinnedMods pins = ModSettingsPlugin.Instance.Pins;
            bool wasPinned = pins.Contains(owner);
            IDisposable originalLease = null, replacement = null, actionNameLease = null;
            try
            {
                if (!wasPinned) Submit(activeTab.GetComponent<ModPin>().Button, page);
                Submit(activeTab, page); yield return null; yield return null; yield return null;
                Check(pins.Contains(owner) && PageMemory.Selected == owner && ModDisplayTabOrderIsStable(page, pins),
                    "MOD tabs sort by ordinal GUID within fixed, pinned and unpinned groups");
                string originalCaption = Caption(activeTab);
                RectTransform numberRow = page.Right.Content.Find("Setting_Types_int") as RectTransform;
                NativeTextInput number = numberRow.Find("ExactValue").GetComponent<NativeTextInput>();
                page.Left.Reveal(activeTab); page.Right.Reveal(numberRow);
                page.Domain.SetSelectedGameObject(number.gameObject); yield return null; yield return null;
                Submit(number, page); number.ChangeText("-");
                Check(number.Invalid && number.IsEditing && Caption(number.transform) == "-",
                    "the real numeric editor has a visible unconfirmed invalid draft before renaming");
                object originalValue = fixture["Types", "int"].BoxedValue;
                Vector2 columnOffsets = new Vector2(page.Left.Content.anchoredPosition.y, page.Right.Content.anchoredPosition.y);
                Transform[] originalTabs = new Transform[page.Left.Content.childCount];
                for (int index = 0; index < originalTabs.Length; index++) originalTabs[index] = page.Left.Content.GetChild(index);
                RectTransform body = page.transform.Find("SettingsBody") as RectTransform;
                Canvas.ForceUpdateCanvases();
                Rect before = BoundsIn(activeTab, body);

                originalLease = client.RegisterModDisplayName(owner, firstName);
                actionNameLease = client.RegisterModDisplayName(actionOwner, actionName);
                yield return new WaitForSecondsRealtime(.35f); yield return null;
                Check(Caption(activeTab) == "● " + firstName && Caption(actionTab) == actionName,
                    "one SDK call per GUID updates config and action-only tab captions without an explicit Refresh");
                replacement = client.RegisterModDisplayName(owner, secondName);
                originalLease.Dispose(); originalLease = null;
                yield return new WaitForSecondsRealtime(.35f); yield return null;
                Check(Caption(activeTab) == "● " + secondName && PageMemory.Selected == owner
                    && pins.Contains(owner) && activeTab.GetComponent<ModPin>().Guid == owner,
                    "re-registering the same GUID wins over disposal of its old lease and preserves selection and pin identity");
                bool sameTabs = page.Left.Content.childCount == originalTabs.Length;
                for (int index = 0; sameTabs && index < originalTabs.Length; index++)
                    sameTabs = page.Left.Content.GetChild(index) == originalTabs[index];
                Check(sameTabs, "changing captions leaves every tab instance and its sibling index unchanged");
                Check(page.Right.Content.Find("Setting_Types_int") == numberRow
                    && page.Domain.currentSelectedGameObject == number.gameObject
                    && number.IsEditing && number.Invalid && Caption(number.transform) == "-"
                    && Equals(originalValue, fixture["Types", "int"].BoxedValue),
                    "renaming does not rebuild the right editor, steal focus, commit or discard the draft");
                Vector2 afterOffsets = new Vector2(page.Left.Content.anchoredPosition.y, page.Right.Content.anchoredPosition.y);
                Canvas.ForceUpdateCanvases();
                Rect after = BoundsIn(activeTab, body);
                Check(Vector2.Distance(afterOffsets, columnOffsets) < .1f && RectNear(before, after),
                    "original tab bounds and independent column scroll positions survive the display-only update");
                File.WriteAllLines(Path.Combine(output, "mod-display-name-geometry.txt"), new string[] {
                    "Screen=" + Screen.width + "x" + Screen.height, "Before=" + before, "After=" + after,
                    "Scroll before=" + columnOffsets, "Scroll after=" + afterOffsets,
                    "GUID=" + owner, "Caption=" + Caption(activeTab), "Position=" + activeTab.GetSiblingIndex(),
                    "Label=" + BoundsIn(activeTab.GetComponentInChildren<T17Text>().rectTransform, body)
                });
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "mod-display-name.png"));
                yield return new WaitForEndOfFrame(); yield return null;

                number.Cancel(); page.Hide(true, false); yield return null;
                replacement.Dispose(); replacement = null;
                Shortcut.Open(); yield return new WaitForSecondsRealtime(.35f); yield return null;
                Check(Shortcut.ActivePage == page && PageMemory.Selected == owner
                    && page.Left.Content.Find("Mod_" + owner) == activeTab && Caption(activeTab) == originalCaption
                    && page.Right.Content.Find("Setting_Types_int") == numberRow,
                    "reopening after disposal while hidden restores the original label without rebuilding the cached page");

                actionNameLease.Dispose(); actionNameLease = null;
                yield return new WaitForSecondsRealtime(.35f);
                Check(page.Left.Content.Find("Mod_" + actionOwner) == actionTab && Caption(actionTab) == actionOwner,
                    "disposing the active action-only name lease restores the fallback without replacing its tab");
                Submit(actionTab, page); yield return null; yield return null; yield return null;
                Submit(page.Right.Content.Find("Setting_@Action_display-name-test/Action_display-name-test"), page);
                Check(actionCalls == 1, "renamed action-only owner keeps its original working action");
                Submit(activeTab, page); yield return null; yield return null; yield return null;
            }
            finally
            {
                if (originalLease != null) originalLease.Dispose();
                if (replacement != null) replacement.Dispose();
                if (actionNameLease != null) actionNameLease.Dispose();
                if (pins.Contains(owner) != wasPinned) page.TogglePin(owner);
            }
        }
        yield return new WaitForSecondsRealtime(.35f); yield return null;
    }

    private static bool ModDisplayTabOrderIsStable(NativePage page, PinnedMods pins)
    {
        int previousGroup = -1;
        string previousGuid = null;
        foreach (Transform tab in page.Left.Content)
        {
            string guid = tab.GetComponent<ModPin>().Guid;
            int group = PinnedMods.IsFixed(guid) ? 0 : pins.Contains(guid) ? 1 : 2;
            if (group < previousGroup || (group == previousGroup && StringComparer.Ordinal.Compare(previousGuid, guid) >= 0)) return false;
            previousGroup = group; previousGuid = guid;
        }
        return true;
    }
}
