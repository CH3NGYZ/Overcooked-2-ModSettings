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
    private IEnumerator VerifyShortcutConflicts(NativePage page)
    {
        const string owner = "fixture.shortcut.conflicts", peer = "fixture.shortcut.peer";
        const string ownerName = "Shortcut test A", peerName = "Shortcut test B";
        const string singleHelp = "Original single-key description", comboHelp = "Original chord description";
        string path = Path.Combine(output, "shortcut-conflicts.cfg");
        ConfigFile file = new ConfigFile(path, false) { SaveOnConfigSet = false };
        ConfigFile other = new ConfigFile(Path.Combine(output, "shortcut-conflicts-peer.cfg"), false) { SaveOnConfigSet = false };
        KeyCode originalGlobal = Shortcut.Key;
        KeyCode[] keys = UnusedShortcutTestKeys(page);
        KeyCode distinct = keys[0], shared = keys[1], chordKey = keys[2];
        KeyboardShortcut sharedChord = new KeyboardShortcut(chordKey, KeyCode.LeftShift);
        ConfigEntry<KeyCode> single = file.Bind("Bindings", "Single", distinct, singleHelp);
        ConfigEntry<KeyboardShortcut> combo = file.Bind("Bindings", "Chord", new KeyboardShortcut(chordKey, KeyCode.LeftControl), comboHelp);
        ConfigEntry<KeyCode> peerSingle = other.Bind("Bindings", "PeerKey", shared, "Peer single key");
        other.Bind("Bindings", "PeerChord", sharedChord, "Peer combination");
        file.Bind("Bindings", "Number", 2, "Numeric validation sentinel");
        ConfigEntry<bool> ordinary = file.Bind("Bindings", "Ordinary", false, "Non-shortcut save sentinel");
        using (ModSettingsPlugin.Instance.RegisterConfigV1(owner, ownerName, "Shortcut conflict fixture", file))
        using (ModSettingsPlugin.Instance.RegisterConfigV1(peer, peerName, "Shortcut conflict peer", other))
        {
            try
            {
                yield return new WaitForSecondsRealtime(.35f);
                Transform tab = page.Left.Content.Find("Mod_" + owner);
                Check(tab != null, "shortcut fixture is discovered through the real registry");
                Submit(tab, page); yield return null; yield return null; yield return null;
                RectTransform body = page.transform.Find("SettingsBody") as RectTransform;
                T17Text help = body.Find("Explanation/Help").GetComponent<T17Text>();
                RectTransform singleRow = page.Right.Content.Find("Setting_Bindings_Single") as RectTransform;
                RectTransform comboRow = page.Right.Content.Find("Setting_Bindings_Chord") as RectTransform;
                T17Button keyList = singleRow.Find("KeyList").GetComponent<T17Button>();
                T17Button rebind = singleRow.Find("Rebind").GetComponent<T17Button>();
                T17Button capture = comboRow.Find("Capture").GetComponent<T17Button>();
                T17Button footer = body.Find("ToggleShortcut").GetComponent<T17Button>();
                RectTransform singleWarning = keyList.transform.Find("ShortcutWarning") as RectTransform;
                RectTransform comboWarning = comboRow.Find("ShortcutValue/ShortcutWarning") as RectTransform;
                RectTransform footerWarning = footer.transform.Find("ShortcutWarning") as RectTransform;
                Check(singleWarning != null && comboWarning != null && footerWarning != null,
                    "native list, capture-only value and global toggle each own a warning graphic");
                bool originalFooterWarning = footerWarning.gameObject.activeSelf;
                Check(!singleWarning.gameObject.activeSelf && !comboWarning.gameObject.activeSelf,
                    "distinct bare keys and different modifier chords start without warnings");
                Navigation before = keyList.navigation, captureBefore = capture.navigation, footerBefore = footer.navigation;
                Rect keyListBefore = BoundsIn((RectTransform)keyList.transform, singleRow);
                Rect comboBefore = BoundsIn((RectTransform)capture.transform, comboRow);
                Rect footerBeforeBounds = BoundsIn((RectTransform)footer.transform, body);

                page.Right.Reveal(singleRow); FocusShortcutConflictControl(page, keyList); yield return null; yield return null;
                Submit(keyList, page); yield return null; yield return null;
                Submit(ShortcutConflictChoice(page, shared), page);
                Check(single.Value == shared && singleWarning.gameObject.activeSelf && !page.Overlay.IsOpen,
                    "native KeyCode list saves a duplicate without blocking and paints its warning synchronously");
                Check(Caption(keyList.transform) == shared + " ▾" && !file.SaveOnConfigSet
                    && new ConfigFile(path, false).Bind("Bindings", "Single", KeyCode.None).Value == shared,
                    "warning does not change the visible key, save mode or saved binding");
                FocusShortcutConflictControl(page, rebind); yield return null; yield return null;
                Check(HasShortcutConflictDetail(help.text, shared.ToString(), peerName, "PeerKey", singleHelp),
                    "native keyboard focus shows the conflicting MOD and entry instead of the original Description");
                const string renamedPeer = "Renamed shortcut peer";
                using (ModSettingsPlugin.Instance.RegisterModDisplayNameV1(peer, renamedPeer))
                {
                    yield return new WaitForSecondsRealtime(.35f); yield return null;
                    Check(HasShortcutConflictDetail(help.text, shared.ToString(), renamedPeer, "PeerKey", singleHelp)
                        && !help.text.Contains(peerName) && page.Domain.currentSelectedGameObject == rebind.gameObject
                        && singleRow == page.Right.Content.Find("Setting_Bindings_Single"),
                        "display-name-only registration refreshes a visible conflict detail without disturbing its row or focus");
                }
                yield return new WaitForSecondsRealtime(.35f); yield return null;
                Check(HasShortcutConflictDetail(help.text, shared.ToString(), peerName, "PeerKey", singleHelp),
                    "releasing the current display-name lease restores the original owner name in visible details");
                Submit(rebind, page); yield return null; yield return null;
                Check(page.Domain.IsDisabled() && Caption(rebind.transform) == Texts.PressKey
                    && help.text.Contains(Texts.PressKey) && !help.text.Contains(peerName),
                    "single-key recording takes precedence over duplicate details while an existing binding conflicts");
                page.Cancel(); yield return null; yield return null;
                Check(!page.Domain.IsDisabled() && single.Value == shared && singleWarning.gameObject.activeSelf
                    && HasShortcutConflictDetail(help.text, shared.ToString(), peerName, "PeerKey", singleHelp),
                    "cancelling single-key recording keeps the binding and restores conflict details");

                page.Right.Reveal(comboRow); FocusShortcutConflictControl(page, capture); yield return null; yield return null;
                Submit(capture, page); yield return null; yield return null;
                KeyCapture chordCapture = page.Overlay.Root == null ? null : page.Overlay.Root.Find("Dialog").GetComponent<KeyCapture>();
                Check(chordCapture != null && page.Domain.IsDisabled(), "the capture-only chord uses the actual modal input owner");
                chordCapture.AcceptKey(chordKey, new KeyCode[] { KeyCode.LeftShift });
                Check(combo.Value.Equals(sharedChord) && comboWarning.gameObject.activeSelf && !page.Overlay.IsOpen && !page.Domain.IsDisabled(),
                    "capture-only combination saves the exact duplicate chord, warns immediately and releases input");
                yield return null; yield return null;
                Check(HasShortcutConflictDetail(help.text, sharedChord.ToString(), peerName, "PeerChord", comboHelp),
                    "restored combination capture focus shows the conflicting chord instead of its Description");

                FocusShortcutConflictControl(page, footer); yield return null; yield return null;
                Submit(footer, page); yield return null; yield return null;
                KeyCapture globalCapture = footer.GetComponent<KeyCapture>();
                Check(globalCapture != null && page.IsRecordingToggleKey && help.text.Contains(Texts.PressKey),
                    "the global capture prompt takes precedence over conflict feedback while listening");
                globalCapture.AcceptKey(shared, new KeyCode[0]);
                Check(Shortcut.Key == shared && footerWarning.gameObject.activeSelf && !page.IsRecordingToggleKey && Shortcut.ActivePage == page,
                    "the reserved panel key can still be assigned through its own capture and warns without toggling this frame");
                yield return null; yield return null;
                Check(help.text.Contains(ownerName) && help.text.Contains(peerName) && help.text.Contains(shared.ToString()),
                    "global shortcut focus enumerates the two matching MOD bindings");
                FocusShortcutConflictControl(page, tab.GetComponent<T17Button>()); yield return null; yield return null;
                Check(help.text.Contains(Texts.ShortcutDuplicateCount(2)), "active MOD idle feedback counts both conflicted rows");
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-conflict-summary.png"));
                yield return new WaitForEndOfFrame(); yield return null;

                page.Right.Reveal(singleRow); yield return null; yield return null;
                Canvas.ForceUpdateCanvases();
                Check(RectNear(keyListBefore, BoundsIn((RectTransform)keyList.transform, singleRow))
                    && RectNear(comboBefore, BoundsIn((RectTransform)capture.transform, comboRow))
                    && RectNear(footerBeforeBounds, BoundsIn((RectTransform)footer.transform, body))
                    && SameShortcutNavigation(before, keyList.navigation) && SameShortcutNavigation(captureBefore, capture.navigation)
                    && SameShortcutNavigation(footerBefore, footer.navigation), "warning visibility neither moves controls nor adds navigation stops");
                Color warningColor = MeasureShortcutWarning(singleWarning, keyList.transform);
                Check(SameColor(warningColor, MeasureShortcutWarning(comboWarning, comboWarning.parent))
                    && SameColor(warningColor, MeasureShortcutWarning(footerWarning, footer.transform)), "all three badges share the same visible warning color");
                PointerEventData ordinaryHover = HoverSettings(page, (RectTransform)keyList.transform, new Vector2(.5f, .5f));
                Check(ordinaryHover.pointerCurrentRaycast.gameObject == keyList.gameObject && single.Value == shared
                    && singleWarning.gameObject.activeSelf && help.text == singleHelp,
                    "ordinary key control hover keeps the original Description while the conflict remains");
                PointerEventData rowHover = HoverSettings(page, singleRow, new Vector2(.12f, .5f));
                Check(rowHover.pointerCurrentRaycast.gameObject == singleRow.gameObject && help.text == singleHelp,
                    "hovering the conflicted row outside its badge also keeps the original Description");
                PointerEventData hover = HoverSettings(page, singleWarning, new Vector2(.5f, .5f));
                Transform hit = hover.pointerCurrentRaycast.gameObject.transform;
                Check(hit == singleWarning && HasShortcutConflictDetail(help.text, shared.ToString(), peerName, "PeerKey", singleHelp)
                    && help.text.Contains(Texts.TogglePanel), "a rendered icon raycast reaches warning feedback with the MOD and panel-key conflict details");
                Check(ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit.gameObject) == keyList.gameObject,
                    "the warning icon preserves the original native click target");
                File.WriteAllLines(Path.Combine(output, "shortcut-conflict-geometry.txt"), new string[] {
                    "Screen=" + Screen.width + "x" + Screen.height,
                    "Single=" + BoundsIn(singleWarning, keyList.transform) + " in " + keyListBefore,
                    "Combination=" + BoundsIn(comboWarning, comboWarning.parent) + " in " + comboBefore,
                    "Global=" + BoundsIn(footerWarning, footer.transform) + " in " + footerBeforeBounds,
                    "Warning tint=#" + ColorUtility.ToHtmlStringRGBA(warningColor),
                    "Footer=" + BoundsIn(help.rectTransform, body), "Hover target=" + hit.name, "Detail=" + help.text
                });
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-conflict-detail.png"));
                yield return new WaitForEndOfFrame(); yield return null;

                page.KeyboardFeedback();
                NativeTextInput number = page.Right.Content.Find("Setting_Bindings_Number/ExactValue").GetComponent<NativeTextInput>();
                number.Select(); yield return null; yield return null;
                number.Begin(); number.ChangeText("-");
                HoverSettings(page, singleWarning, new Vector2(.5f, .5f));
                Check(number.Invalid && help.text == number.ValidationMessage && help.color.r > help.color.g,
                    "warning hover does not mask an active numeric validation error");
                number.Cancel(); page.KeyboardFeedback();

                FocusShortcutConflictControl(page, footer); yield return null; yield return null;
                Submit(footer, page); yield return null; yield return null;
                footer.GetComponent<KeyCapture>().AcceptKey(originalGlobal, new KeyCode[0]);
                Check(Shortcut.Key == originalGlobal && footerWarning.gameObject.activeSelf == originalFooterWarning && singleWarning.gameObject.activeSelf,
                    "restoring the panel shortcut removes its new conflict without changing the surviving MOD duplicate");

                FocusShortcutConflictControl(page, keyList); yield return null; yield return null;
                peerSingle.Value = distinct;
                yield return new WaitForSecondsRealtime(.35f); yield return null;
                Check(singleRow == page.Right.Content.Find("Setting_Bindings_Single") && comboRow == page.Right.Content.Find("Setting_Bindings_Chord")
                    && page.Domain.currentSelectedGameObject == keyList.gameObject && !singleWarning.gameObject.activeSelf
                    && comboWarning.gameObject.activeSelf && help.text == singleHelp,
                    "external change refreshes warning and focused Description in place without replacing either row or stealing focus");

                Submit(singleRow.Find("Default"), page);
                Check(single.Value == distinct && singleWarning.gameObject.activeSelf,
                    "restoring the original key through the row default immediately warns if it is now shared");
                Submit(keyList, page); yield return null; yield return null;
                Submit(ShortcutConflictChoice(page, chordKey), page);
                Check(single.Value == chordKey && !singleWarning.gameObject.activeSelf && comboWarning.gameObject.activeSelf,
                    "choosing a distinct bare key immediately clears its warning, even if that key has a conflicting modified chord");
                peerSingle.Value = shared;
                yield return new WaitForSecondsRealtime(.35f); yield return null;
                Check(!singleWarning.gameObject.activeSelf, "next external change clears the restored key warning in place");

                string savedPath = path + ".saved";
                File.Move(path, savedPath);
                try
                {
                    Directory.CreateDirectory(path);
                    FocusShortcutConflictControl(page, keyList); yield return null; yield return null;
                    Submit(keyList, page); yield return null; yield return null;
                    Submit(ShortcutConflictChoice(page, shared), page);
                    Check(single.Value == shared && singleWarning.gameObject.activeSelf && help.color.r > help.color.g
                        && !string.IsNullOrEmpty(help.text) && !help.text.Contains(peerName) && singleRow == page.Right.Content.Find("Setting_Bindings_Single"),
                        "a real save failure retains the duplicate actual value and displays the save error rather than warning detail");

                    T17Button toggle = page.Right.Content.Find("Setting_Bindings_Ordinary/Toggle").GetComponent<T17Button>();
                    FocusShortcutConflictControl(page, toggle); yield return null; yield return null;
                    Submit(toggle, page);
                    string saveError = help.text;
                    Check(ordinary.Value && saveError.StartsWith("Ordinary: ", StringComparison.Ordinal) && help.color.r > help.color.g,
                        "an ordinary toggle reports its real save failure while conflicts exist");
                    peerSingle.Value = distinct;
                    yield return new WaitForSecondsRealtime(.35f); yield return null;
                    Check(!singleWarning.gameObject.activeSelf && comboWarning.gameObject.activeSelf
                        && page.Domain.currentSelectedGameObject == toggle.gameObject && help.text == saveError && help.color.r > help.color.g,
                        "external shortcut changes update warnings without hiding an active non-shortcut save error");
                    FocusShortcutConflictControl(page, keyList); yield return null; yield return null;
                    Check(help.text == singleHelp, "moving focus away from the failed toggle restores ordinary feedback");
                }
                finally
                {
                    if (Directory.Exists(path)) Directory.Delete(path);
                    File.Move(savedPath, path);
                }
            }
            finally
            {
                if (Shortcut.Key != originalGlobal) Shortcut.Setting.Entry.BoxedValue = originalGlobal;
                page.KeyboardFeedback();
            }
        }
        yield return new WaitForSecondsRealtime(.35f);
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
    }

    private KeyCode[] UnusedShortcutTestKeys(NativePage page)
    {
        HashSet<KeyCode> used = new HashSet<KeyCode> { Shortcut.Key };
        foreach (ModConfig mod in page.Catalog.Snapshot())
            foreach (SettingRow row in mod.Rows)
            {
                if (row.Entry == null) continue;
                if (row.Entry.SettingType == typeof(KeyCode)) used.Add((KeyCode)row.Entry.BoxedValue);
                else if (row.Entry.SettingType == typeof(KeyboardShortcut)) used.Add(((KeyboardShortcut)row.Entry.BoxedValue).MainKey);
            }
        List<KeyCode> keys = new List<KeyCode>();
        foreach (KeyCode key in new KeyCode[] { KeyCode.F15, KeyCode.F14, KeyCode.F13, KeyCode.F12, KeyCode.F11, KeyCode.F10, KeyCode.F9, KeyCode.F8, KeyCode.F7, KeyCode.F6 })
            if (!used.Contains(key)) keys.Add(key);
        for (KeyCode key = KeyCode.Z; keys.Count < 3 && key >= KeyCode.A; key--)
            if (!used.Contains(key)) keys.Add(key);
        Check(keys.Count >= 3, "three isolated test keys are available in this install");
        return keys.ToArray();
    }

    private T17Button ShortcutConflictChoice(NativePage page, KeyCode key)
    {
        Transform options = page.Overlay.Root.Find("Dialog/Choices/Viewport/Content");
        foreach (T17Button option in options.GetComponentsInChildren<T17Button>())
        {
            string label = Caption(option.transform);
            if (label == key.ToString())
            {
                Check(option.transform.Find("ShortcutWarning") == null, "dropdown keeps the original plain choice label for " + key);
                return option;
            }
        }
        throw new InvalidOperationException("Missing unmodified dropdown label for " + key);
    }

    private static void FocusShortcutConflictControl(NativePage page, Selectable control)
    { page.KeyboardFeedback(); page.Domain.SetSelectedGameObject(control.gameObject); }

    private static bool HasShortcutConflictDetail(string actual, string key, string mod, string title, string description)
    {
        const string marker = "{bindings}";
        string prefix = Texts.ShortcutDuplicate(key, marker).Split(new string[] { marker }, StringSplitOptions.None)[0].Trim();
        return actual.Contains(prefix) && actual.Contains(mod) && actual.Contains(title) && !actual.Contains(description);
    }

    private static bool SameShortcutNavigation(Navigation before, Navigation after)
    {
        return before.mode == after.mode && before.selectOnUp == after.selectOnUp && before.selectOnDown == after.selectOnDown
            && before.selectOnLeft == after.selectOnLeft && before.selectOnRight == after.selectOnRight;
    }

    private static bool RectNear(Rect before, Rect after)
    { return Vector2.Distance(before.min, after.min) < .1f && Vector2.Distance(before.max, after.max) < .1f; }

    private Color MeasureShortcutWarning(RectTransform badge, Transform holder)
    {
        MaskableGraphic graphic = badge.GetComponent<MaskableGraphic>();
        Check(graphic != null && graphic.enabled && graphic.raycastTarget && badge.GetComponent<Selectable>() == null,
            "warning is a raycastable graphic without a navigation target in " + holder.name);
        Rect area = BoundsIn(badge, holder), parent = ((RectTransform)holder).rect;
        Check(Vector2.Distance(area.size, new Vector2(22, 22)) < .1f
            && area.xMin >= parent.xMin - .1f && area.xMax <= parent.xMax + .1f
            && area.yMin >= parent.yMin - .1f && area.yMax <= parent.yMax + .1f, "22 UI warning fits inside " + holder.name);
        Color tint = graphic.color * graphic.canvasRenderer.GetColor();
        Check(tint.a > .9f && tint.r > tint.b && tint.g > tint.b, "warning uses a visible warm tint in " + holder.name);
        return tint;
    }
}
