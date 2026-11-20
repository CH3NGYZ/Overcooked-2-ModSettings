using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[BepInPlugin("com.ch3ngyz.plugin.overcooked2.modsettings.tests", "ModSettings Unity Tests", "1.0.0")]
[BepInDependency(ModSettingsPlugin.Guid)]
public sealed partial class ModSettingsUnityTests : BaseUnityPlugin
{
    private static string output;
    private static readonly Dictionary<string, string> redirected = new Dictionary<string, string>();
    private readonly List<IDisposable> leases = new List<IDisposable>();
    private Harmony harmony;
    private int assertions;
    private int fixtureCount = 30, expectedHosts;
    private string expectedVersion = "1.0.0";
    private void Update()
    {
        if (string.IsNullOrEmpty(output)) return;
        AcknowledgeKnownNotice();
        // Scripted focus probes isolate physical mouse movement on both input domains.
        NativePage cached = Shortcut.CachedPage;
        if (cached == null) return;
        T17StandaloneInputModule input = cached.Domain.GetComponent<T17StandaloneInputModule>();
        if (input != null)
        {
            input.allowMouseInput = false; input.forceModuleActive = true;
            AccessTools.Field(typeof(T17StandaloneInputModule), "m_CurrentMouseDelta").SetValue(input, 0f);
        }
    }
    private void Awake()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-modSettingsTest") output = args[i + 1];
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "-modSettingsFixtureCount") fixtureCount = int.Parse(args[i + 1]);
            if (args[i] == "-modSettingsExpectedHosts") expectedHosts = int.Parse(args[i + 1]);
            if (args[i] == "-modSettingsExpectedVersion") expectedVersion = args[i + 1];
        }
        if (string.IsNullOrEmpty(output)) { enabled = false; return; }
        Directory.CreateDirectory(Path.Combine(output, "saves"));
        harmony = new Harmony("com.ch3ngyz.plugin.overcooked2.modsettings.tests");
        harmony.Patch(AccessTools.Method(typeof(PCSaveManager), "GetFileAddress"), null, new HarmonyMethod(typeof(ModSettingsUnityTests), "Redirect"));
        Type diy = AccessTools.TypeByName("OC2DIYLevel.DIYLevelSaveManager");
        if (diy != null) harmony.Patch(AccessTools.Method(diy, "GetFileAddress"), null, new HarmonyMethod(typeof(ModSettingsUnityTests), "Redirect"));
        InstallThemeFixture();
        StartCoroutine(Protected());
    }
    private static void Redirect(ref string __result)
    {
        string sandbox;
        if (!redirected.TryGetValue(__result, out sandbox))
        {
            string hash = "-";
            using (SHA256 sha = SHA256.Create())
            {
                byte[] identity = System.Text.Encoding.UTF8.GetBytes(__result);
                // PCSaveManager.SaveFile splits the directory at '/', even on Windows.
                sandbox = Path.Combine(Path.Combine(output, "saves"), BitConverter.ToString(sha.ComputeHash(identity)).Replace("-", "") + ".save").Replace('\\', '/');
                if (File.Exists(__result)) { hash = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(__result))).Replace("-", ""); File.Copy(__result, sandbox); }
            }
            File.AppendAllText(Path.Combine(output, "saves.tsv"), hash + "\t" + __result + Environment.NewLine);
            redirected.Add(__result, sandbox);
        }
        __result = sandbox;
    }
    private IEnumerator Protected()
    {
        IEnumerator run = Run();
        while (true)
        {
            bool next; object current;
            try { next = run.MoveNext(); current = next ? run.Current : null; }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(output, "result.txt"), "FAIL after " + assertions + " assertions\n" + error);
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "failure.png"));
                Logger.LogError(error); break;
            }
            if (!next) break;
            yield return current;
        }
        yield return new WaitForSecondsRealtime(.5f);
        foreach (IDisposable lease in leases) lease.Dispose();
        Application.Quit();
    }
    private void Check(bool value, string label)
    {
        assertions++;
        File.AppendAllText(Path.Combine(output, "assertions.txt"), (value ? "PASS " : "FAIL ") + label + Environment.NewLine);
        if (!value) throw new InvalidOperationException(label);
    }
    private IEnumerator Run()
    {
        bool engaged = false;
        float deadline = Time.realtimeSinceStartup + 100;
        FrontendRootMenu root = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            AcknowledgeKnownNotice();
            StartScreenFlow flow = StartScreenFlow.Instance;
            PlayerManager player = GameUtils.RequestManager<PlayerManager>();
            if (!engaged && flow != null && player != null && !player.HasPlayer() && !LoadingScreenFlow.IsLoading && SteamPlayerManager.Initialized
                && (bool)AccessTools.Field(typeof(StartScreenFlow), "m_bCheckingForEngagement").GetValue(null))
            {
                engaged = true; AccessTools.Field(typeof(StartScreenFlow), "m_bCheckingForEngagement").SetValue(null, false);
                player.StartGameownerEngagement(ControlPadInput.PadNum.One, null, delegate(GamepadUser user)
                { AccessTools.Method(typeof(StartScreenFlow), "OnEngagementFinished").Invoke(flow, new object[] { user }); });
            }
            root = T17FrontendFlow.Instance == null ? null : T17FrontendFlow.Instance.m_Rootmenu;
            if (NativeHooks.Ready(root)) break;
            yield return null;
        }
        Check(NativeHooks.Ready(root), "successful profile and interactive frontend readiness");
        T17StandaloneInputModule input = root.CachedEventSystem.GetComponent<T17StandaloneInputModule>();
        if (input != null) { input.allowMouseInput = false; input.forceModuleActive = true; AccessTools.Field(typeof(T17StandaloneInputModule), "m_CurrentMouseDelta").SetValue(input, 0f); }
        Check(BepInEx.Bootstrap.Chainloader.PluginInfos[ModSettingsPlugin.Guid].Metadata.Version.ToString() == expectedVersion, "loader selected expected highest version " + expectedVersion);
        int serviceCount = 0;
        foreach (BaseUnityPlugin plugin in Resources.FindObjectsOfTypeAll<BaseUnityPlugin>())
            foreach (BepInPlugin metadata in plugin.GetType().GetCustomAttributes(typeof(BepInPlugin), false))
                if (metadata.GUID == ModSettingsPlugin.Guid) serviceCount++;
        Check(serviceCount == 1, "exactly one shared service instance");
        if (expectedHosts > 0)
        {
            Check(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("modsettings.fixture.host.a"), "embedded host A runs alongside selected service");
            if (expectedHosts > 1) Check(BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("modsettings.fixture.host.b"), "embedded host B runs alongside selected service");
        }
        RegisterFixtures(fixtureCount);
        VerifyHostIsolation();
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-modSettingsOpeningPerformance") >= 0)
        {
            IEnumerator performance = VerifyOpeningPerformance(root); while (performance.MoveNext()) yield return performance.Current;
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity opening performance assertions"); yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-modSettingsShortcutOnly") >= 0)
        {
            IEnumerator shortcutOnly = VerifyShortcut(root); while (shortcutOnly.MoveNext()) yield return shortcutOnly.Current;
            IEnumerator rebuildOnly = VerifySceneRebuild(root); while (rebuildOnly.MoveNext()) yield return rebuildOnly.Current;
            IEnumerator scenesOnly = VerifyShortcutScenes(); while (scenesOnly.MoveNext()) yield return scenesOnly.Current;
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity shortcut assertions"); yield break;
        }
        Transform tab = root.transform.Find("WorldSpaceCanvas/Safezone_Rootmenu/RootTab/CustomizationOptions");
        Check(tab != null, "native customization tab exists");
        FrontendMenuBehaviour menu = tab.GetComponent<FrontendMenuBehaviour>();
        menu.Show(root.CurrentGamepadUser, root, null, false);
        yield return new WaitForSecondsRealtime(1f);
        T17Button button = tab.Find("ModSettingsEntry").GetComponent<T17Button>();
        Check(button != null && button.isActiveAndEnabled, "single entry created under chef tab");
        CheckRaycast(button, root.CachedEventSystem, "entry is topmost selectable at its real screen center");
        foreach (Component component in button.GetComponentsInChildren<Component>(true))
            File.AppendAllText(Path.Combine(output, "entry-focus.txt"), component.gameObject.name + ": " + component.GetType().FullName + Environment.NewLine);
        File.AppendAllText(Path.Combine(output, "entry-focus.txt"), "Target=" + button.targetGraphic + " transition=" + button.transition + " colors=" + button.colors.normalColor + "/" + button.colors.highlightedColor);
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(root.CachedEventSystem), ExecuteEvents.pointerEnterHandler);
        yield return null; yield return null;
        File.AppendAllText(Path.Combine(output, "entry-focus.txt"), Environment.NewLine
            + "hover selected=" + root.CachedEventSystem.currentSelectedGameObject
            + " pending=" + root.CachedEventSystem.GetPendingSelectedGameObject()
            + " boundId=" + (button.GetDomain() == null ? 0 : button.GetDomain().GetInstanceID())
            + " rootId=" + root.CachedEventSystem.GetInstanceID()
            + " disabled=" + root.CachedEventSystem.IsDisabled() + " page=" + Shortcut.ActivePage + Environment.NewLine);
        Check(root.CachedEventSystem.currentSelectedGameObject == button.gameObject, "hovering settings entry gives it native focus");
        Check(button.targetGraphic != null && button.transform.Find("Highlight").gameObject.activeSelf,
            "focused entry shows its native highlight graphic");
        root.CachedEventSystem.SetSelectedGameObject(tab.Find("CustomChef").gameObject); yield return null; yield return null;
        Check(!button.transform.Find("Highlight").gameObject.activeSelf, "entry highlight clears when focus moves away");
        ExecuteEvents.Execute(button.gameObject, new PointerEventData(root.CachedEventSystem) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerDownHandler);
        yield return null; yield return null;
        Check(root.CachedEventSystem.currentSelectedGameObject == button.gameObject && button.transform.Find("Highlight").gameObject.activeSelf,
            "mouse press gives entry focus and visible highlight before opening");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "chef-entry.png"));
        yield return new WaitForEndOfFrame();
        yield return null;
        DumpHierarchy(root.transform, "hierarchy.txt");
        root.CachedEventSystem.SetSelectedGameObject(button.gameObject); yield return null; yield return null;
        ExecuteEvents.Execute(button.gameObject, new BaseEventData(root.CachedEventSystem), ExecuteEvents.submitHandler);
        yield return new WaitForSecondsRealtime(1f);
        NativePage page = Shortcut.CachedPage;
        Check(page != null && page.gameObject.activeInHierarchy, "entry submits to active native page");
        Check(page.GetComponent<FrontendOptionsMenu>() == null, "game options controller removed");
        Check(page.Domain != root.CachedEventSystem && page.Domain is GlobalEventSystem, "independent global input domain bound");
        Check(page.Left.Content.childCount >= fixtureCount, fixtureCount + " registered MODs present");
        Check(page.Left.Viewport.rect.width > 0 && page.Right.Viewport.rect.width > 0, "both column viewports laid out");
        float ratio = page.Left.Viewport.rect.width / (page.Left.Viewport.rect.width + page.Right.Viewport.rect.width);
        Check(ratio > .26f && ratio < .31f, "left width near 28 percent");
        foreach (T17Button candidate in page.Left.Content.GetComponentsInChildren<T17Button>())
            if (candidate.name == "Mod_fixture.00") { ExecuteEvents.Execute(candidate.gameObject, new BaseEventData(page.Domain), ExecuteEvents.submitHandler); break; }
        yield return new WaitForSecondsRealtime(.4f);
        Check(page.Right.Content.Find("Setting_Types_boolean") != null, "active MOD right column contains boolean");
        Check(page.Right.Content.Find("Setting_Types_decimal") != null, "active MOD includes decimal exact input");
        CheckRaycast(page.Right.Content.Find("Setting_Types_boolean/Toggle").GetComponent<T17Button>(), page.Domain, "setting toggle is topmost selectable");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "settings-" + fixtureCount + "-mods.png"));
        WriteGeometry(page);
        if (themeFixture)
        {
            IEnumerator themed = VerifyFeedback(page); while (themed.MoveNext()) yield return themed.Current;
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity recolored theme assertions"); yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-modSettingsLocalizationOnly") >= 0)
        {
            IEnumerator localized = VerifyLocalization(page, button); while (localized.MoveNext()) yield return localized.Current;
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity localization assertions"); yield break;
        }
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-modSettingsTextEditingOnly") >= 0)
        {
            IEnumerator focusOnly = VerifyTextFocus(page); while (focusOnly.MoveNext()) yield return focusOnly.Current;
            IEnumerator liveOnly = VerifyImmediateEditing(page); while (liveOnly.MoveNext()) yield return liveOnly.Current;
            IEnumerator textOnly = VerifyTextSelection(page); while (textOnly.MoveNext()) yield return textOnly.Current;
            IEnumerator sortOnly = VerifyCategoryOrder(page); while (sortOnly.MoveNext()) yield return sortOnly.Current;
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity text editing/category assertions"); yield break;
        }
        VerifyLayout(page);
        yield return new WaitForEndOfFrame();
        IEnumerator capture = VerifyShortcutCapture(page); while (capture.MoveNext()) yield return capture.Current;
        IEnumerator clipping = VerifyTextClipping(page); while (clipping.MoveNext()) yield return clipping.Current;
        IEnumerator feedback = VerifyFeedback(page); while (feedback.MoveNext()) yield return feedback.Current;
        IEnumerator responsive = VerifyResponsiveRows(page); while (responsive.MoveNext()) yield return responsive.Current;
        IEnumerator overlays = VerifyOverlayDismiss(page); while (overlays.MoveNext()) yield return overlays.Current;
        IEnumerator scrollbars = VerifyScrollbars(page); while (scrollbars.MoveNext()) yield return scrollbars.Current;
        IEnumerator pins = VerifyPins(page); while (pins.MoveNext()) yield return pins.Current;
        IEnumerator editing = VerifyEditing(page); while (editing.MoveNext()) yield return editing.Current;
        if (fixtureCount > 1) { IEnumerator interactions = VerifyInteractions(page); while (interactions.MoveNext()) yield return interactions.Current; }
        IEnumerator types = VerifyTypeEditors(page); while (types.MoveNext()) yield return types.Current;
        IEnumerator immediate = VerifyImmediateEditing(page); while (immediate.MoveNext()) yield return immediate.Current;
        IEnumerator selection = VerifyTextSelection(page); while (selection.MoveNext()) yield return selection.Current;
        IEnumerator textFocus = VerifyTextFocus(page); while (textFocus.MoveNext()) yield return textFocus.Current;
        IEnumerator categories = VerifyCategoryOrder(page); while (categories.MoveNext()) yield return categories.Current;
        IEnumerator colors = VerifyColorPicker(page); while (colors.MoveNext()) yield return colors.Current;
        IEnumerator actions = VerifyActions(page); while (actions.MoveNext()) yield return actions.Current;
        IEnumerator keys = VerifyKeyEditors(page); while (keys.MoveNext()) yield return keys.Current;
        IEnumerator lifecycle = VerifyLifecycle(page, button, root); while (lifecycle.MoveNext()) yield return lifecycle.Current;
        for (int i = 0; i < 3; i++)
        {
            page.Hide(true, false); yield return null;
            // The business-dialog and global shortcut cases can leave another native item
            // selected. A scripted submit alone does not give its recipient native focus.
            File.AppendAllText(Path.Combine(output, "entry-focus.txt"), "\nbefore repeat " + i + ": " + root.CachedEventSystem.currentSelectedGameObject);
            button.Select(); yield return null; yield return null;
            Check(root.CachedEventSystem.currentSelectedGameObject == button.gameObject, "chef entrance is focused before native submit " + i);
            ExecuteEvents.Execute(button.gameObject, new BaseEventData(root.CachedEventSystem), ExecuteEvents.submitHandler);
            yield return new WaitForSecondsRealtime(.35f);
            Check(page.gameObject.activeInHierarchy && Resources.FindObjectsOfTypeAll<NativePage>().Length == 1, "repeated open unique page " + i);
        }
        page.Hide(true, false); yield return null; yield return null;
        Check(root.CachedEventSystem.currentSelectedGameObject == button.gameObject, "close returns focus to MOD settings entry");
        IEnumerator shortcut = VerifyShortcut(root); while (shortcut.MoveNext()) yield return shortcut.Current;
        IEnumerator rebuild = VerifyRebuild(root, button); while (rebuild.MoveNext()) yield return rebuild.Current;
        IEnumerator scene = VerifySceneRebuild(root); while (scene.MoveNext()) yield return scene.Current;
        IEnumerator pageDismiss = VerifyPageDismiss(); while (pageDismiss.MoveNext()) yield return pageDismiss.Current;
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-modSettingsGameplay") >= 0)
        { IEnumerator gameplay = VerifyShortcutScenes(); while (gameplay.MoveNext()) yield return gameplay.Current; }
        File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: " + assertions + " actual Unity assertions");
    }
    private void AcknowledgeKnownNotice()
    {
        foreach (T17DialogBox dialog in Resources.FindObjectsOfTypeAll<T17DialogBox>())
        {
            if (!dialog.IsActive || dialog.m_Title == null || dialog.m_Message == null) continue;
            string title = dialog.m_Title.text, message = dialog.m_Message.text;
            if ((title.Contains("FastInit") || title.Contains("快速载入") || message.Contains("FastInit v1.9")) && dialog.m_ConfirmButton != null)
                dialog.m_ConfirmButton.onClick.Invoke();
            // The installed HostUtilities can finish its startup network check after Ready.
            // Decline this known update notice; never invoke its update or website actions.
            if ((title == "MOD 更新" || title == "MOD Update") && dialog.m_DeclineButton != null)
                dialog.m_DeclineButton.onClick.Invoke();
        }
    }
    private static void DumpHierarchy(Transform root, string file)
    {
        List<string> lines = new List<string>();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        { string path = t.name; Transform p = t.parent; while (p != null && p != root.parent) { path = p.name + "/" + path; p = p.parent; } lines.Add(path); }
        File.WriteAllLines(Path.Combine(output, file), lines.ToArray());
    }
    private static void WriteGeometry(NativePage page)
    {
        File.WriteAllText(Path.Combine(output, "geometry.txt"), "Screen=" + Screen.width + "x" + Screen.height
            + "\nLeftViewport=" + page.Left.Viewport.rect + "\nRightViewport=" + page.Right.Viewport.rect
            + "\nLeftContent=" + page.Left.Content.rect + "\nRightContent=" + page.Right.Content.rect);
        foreach (string path in new string[] { "SettingsBody", "SettingsBody/HeaderBacker", "SettingsBody/HeaderBacker/Header", "SettingsBody/Search", "SettingsBody/ClearSearch", "SettingsBody/Explanation", "SettingsBody/CloseModSettings" })
        {
            Transform item = page.transform.Find(path); RectTransform rect = item as RectTransform;
            Vector3[] corners = new Vector3[4]; rect.GetWorldCorners(corners);
            string line = path + " rect=" + rect.rect + " position=" + rect.anchoredPosition + " pivot=" + rect.pivot + " anchors=" + rect.anchorMin + ":" + rect.anchorMax;
            foreach (T17Text text in item.GetComponentsInChildren<T17Text>(true)) line += " text=" + text.name + " align=" + text.alignment + " size=" + text.fontSize;
            foreach (Vector3 point in corners) line += " screen=" + RectTransformUtility.WorldToScreenPoint(null, point);
            File.AppendAllText(Path.Combine(output, "layout.txt"), line + Environment.NewLine);
        }
    }
    private void CheckRaycast(Selectable target, T17EventSystem domain, string label)
    {
        Canvas canvas = target.GetComponentInParent<Canvas>(); GraphicRaycaster raycaster = target.GetComponentInParent<GraphicRaycaster>();
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : raycaster != null ? raycaster.eventCamera : canvas.worldCamera;
        RectTransform rect = target.GetComponent<RectTransform>();
        PointerEventData pointer = new PointerEventData(domain) { position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)) };
        List<RaycastResult> hits = new List<RaycastResult>(); domain.RaycastAll(pointer, hits);
        List<string> trace = new List<string>(); Selectable first = null;
        foreach (RaycastResult hit in hits)
        { trace.Add(hit.gameObject.name); if (first == null) first = hit.gameObject.GetComponentInParent<Selectable>(); }
        File.AppendAllText(Path.Combine(output, "raycasts.txt"), label + ": " + string.Join(",", trace.ToArray()) + Environment.NewLine);
        Check(first == target, label);
    }
    private void ClickButton(NativePage page, T17Button button)
    {
        RectTransform rect = (RectTransform)button.transform;
        PointerEventData pointer = new PointerEventData(page.Domain) { button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        List<RaycastResult> hits = new List<RaycastResult>(); page.Domain.RaycastAll(pointer, hits);
        Check(hits.Count > 0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject) == button.gameObject,
            button.name + " click routes through the native canvas hit");
        pointer.pointerCurrentRaycast = pointer.pointerPressRaycast = hits[0];
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);
        ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
    }
    private IEnumerator VerifyRebuild(FrontendRootMenu root, T17Button original)
    {
        // Exercise the feature's teardown/rebind against the same live native frontend.
        NativeEntry entry = (NativeEntry)AccessTools.Field(typeof(ModSettingsPlugin), "entry").GetValue(ModSettingsPlugin.Instance);
        RectTransform panel = original.transform.parent as RectTransform;
        float beforeHeight = panel.rect.height;
        Vector2 beforePivot = panel.pivot;
        float growth = (float)AccessTools.Field(typeof(NativeEntry), "extension").GetValue(entry);
        Selectable chef = panel.Find("CustomChef").GetComponent<Selectable>();
        Selectable left = chef.navigation.selectOnLeft, right = chef.navigation.selectOnRight;
        entry.Dispose(); yield return null; yield return null;
        Check(Mathf.Abs(panel.rect.height - beforeHeight + growth) < .1f, "entry teardown removes exactly its owned height contribution");
        Transform tab = root.transform.Find("WorldSpaceCanvas/Safezone_Rootmenu/RootTab/CustomizationOptions");
        entry.Refresh(tab.GetComponent<FrontendMenuBehaviour>(), root.CurrentGamepadUser); yield return null; yield return null;
        T17Button replacement = tab.Find("ModSettingsEntry").GetComponent<T17Button>();
        Check(original == null && replacement != null, "teardown and native rebind creates a fresh entry");
        Check(Mathf.Abs(panel.rect.height - beforeHeight) < .1f && Vector2.Distance(panel.pivot, beforePivot) < .001f, "entry rebind restores identical height and pivot");
        Check(chef.navigation.selectOnLeft == left && chef.navigation.selectOnRight == right, "chef entry retains original cross-tab navigation");
        File.WriteAllText(Path.Combine(output, "entry-restore.txt"), "Height=" + beforeHeight + "\nOwnedGrowth=" + growth + "\nReboundHeight=" + panel.rect.height + "\nPivot=" + panel.pivot);
        ExecuteEvents.Execute(replacement.gameObject, new BaseEventData(root.CachedEventSystem), ExecuteEvents.submitHandler);
        yield return new WaitForSecondsRealtime(.35f);
        NativePage page = Shortcut.CachedPage;
        Check(page != null && page.Domain is GlobalEventSystem, "entry rebind retains independent global input domain");
        page.Hide(true, false); yield return null;
    }
}
