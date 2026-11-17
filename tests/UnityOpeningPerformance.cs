using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed partial class ModSettingsUnityTests
{
    private sealed class OpeningCost { internal long Ticks; internal int Calls; }
    private static readonly Dictionary<string, OpeningCost> openingCosts = new Dictionary<string, OpeningCost>();
    private static void OpeningStarted(out long __state) { __state = Stopwatch.GetTimestamp(); }
    private static void OpeningFinished(MethodBase __originalMethod, long __state)
    {
        string key = __originalMethod.DeclaringType.Name + "." + __originalMethod.Name;
        OpeningCost cost;
        if (!openingCosts.TryGetValue(key, out cost)) { cost = new OpeningCost(); openingCosts.Add(key, cost); }
        cost.Ticks += Stopwatch.GetTimestamp() - __state; cost.Calls++;
    }
    private void ObserveOpening(Type type, string method)
    {
        foreach (MethodInfo candidate in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            if (candidate.Name == method)
                harmony.Patch(candidate, new HarmonyMethod(typeof(ModSettingsUnityTests), "OpeningStarted"),
                    new HarmonyMethod(typeof(ModSettingsUnityTests), "OpeningFinished"));
    }
    private static void WriteOpeningCosts(string label, double milliseconds, double frameMilliseconds)
    {
        string text = label + " submitMs=" + milliseconds.ToString("F3", CultureInfo.InvariantCulture)
            + " nextFrameMs=" + frameMilliseconds.ToString("F3", CultureInfo.InvariantCulture) + Environment.NewLine;
        foreach (KeyValuePair<string, OpeningCost> item in openingCosts)
            text += "  " + item.Key + " calls=" + item.Value.Calls + " ms="
                + (item.Value.Ticks * 1000.0 / Stopwatch.Frequency).ToString("F3", CultureInfo.InvariantCulture) + Environment.NewLine;
        File.AppendAllText(Path.Combine(output, "opening-performance.txt"), text); openingCosts.Clear();
    }
    private IEnumerator VerifyOpeningPerformance(FrontendRootMenu root)
    {
        ObserveOpening(typeof(NativePage), "Create"); ObserveOpening(typeof(NativePage), "Open");
        ObserveOpening(typeof(NativePage), "Reload"); ObserveOpening(typeof(NativePage), "BuildSettings");
        ObserveOpening(typeof(ConfigCatalog), "Snapshot"); ObserveOpening(typeof(EditorRow), "Create");
        ObserveOpening(typeof(NativeUi), "CloneButton"); ObserveOpening(typeof(NativeUi), "Text");
        ObserveOpening(typeof(NativeIcons), "Get");
        ObserveOpening(typeof(EditorRow), "Arrange"); ObserveOpening(typeof(EditorRow), "Refresh");
        Transform tab = root.transform.Find("WorldSpaceCanvas/Safezone_Rootmenu/RootTab/CustomizationOptions");
        tab.GetComponent<FrontendMenuBehaviour>().Show(root.CurrentGamepadUser, root, null, false);
        yield return new WaitForSecondsRealtime(.5f);
        T17Button entrance = tab.Find("ModSettingsEntry").GetComponent<T17Button>();
        PageMemory.Selected = "fixture.00";
        int stableTab = 0, stableEditor = 0;
        bool reusedTabs = true, reusedEditors = true;
        double maximumWarm = 0;
        NativePage page = null;
        for (int sample = 0; sample < 7; sample++)
        {
            yield return new WaitForEndOfFrame();
            openingCosts.Clear();
            Stopwatch clock = Stopwatch.StartNew();
            ExecuteEvents.Execute(entrance.gameObject, new BaseEventData(root.CachedEventSystem), ExecuteEvents.submitHandler);
            double elapsed = clock.Elapsed.TotalMilliseconds;
            yield return null;
            double frame = clock.Elapsed.TotalMilliseconds;
            double maximumFrame = frame, previousFrame = frame;
            page = Shortcut.CachedPage;
            while (page != null && !page.ContentReady && clock.Elapsed.TotalMilliseconds < 3000)
            {
                yield return null;
                double currentFrame = clock.Elapsed.TotalMilliseconds;
                maximumFrame = Math.Max(maximumFrame, currentFrame - previousFrame); previousFrame = currentFrame;
            }
            double ready = clock.Elapsed.TotalMilliseconds;
            WriteOpeningCosts(sample == 0 ? "cold" : "warm-" + sample, elapsed, frame);
            File.AppendAllText(Path.Combine(output, "opening-performance.txt"), "  readyMs=" + ready.ToString("F3", CultureInfo.InvariantCulture)
                + " maxObservedFrameMs=" + maximumFrame.ToString("F3", CultureInfo.InvariantCulture) + Environment.NewLine);
            yield return null; yield return null;
            page = Shortcut.CachedPage;
            Check(page != null && page.gameObject.activeInHierarchy, "performance sample opens real native page " + sample);
            Check(page.ContentReady, "performance sample completes initial content build " + sample);
            Transform mod = page.Left.Content.Find("Mod_fixture.00"), editor = page.Right.Content.Find("Setting_Types_boolean");
            Check(mod != null && editor != null, "performance sample contains fixture tabs and editors " + sample);
            if (sample == 0)
            {
                stableTab = mod.GetInstanceID(); stableEditor = editor.GetInstanceID();
                Check(elapsed < 50, "first open displays frame and returns submit within 50ms, actual=" + elapsed.ToString("F3"));
                Check(ready < 1000, "initial editors finish loading within one second, actual=" + ready.ToString("F3"));
                Check(maximumFrame < 50, "initial load avoids an observed frame over 50ms, actual=" + maximumFrame.ToString("F3"));
            }
            else
            {
                reusedTabs &= stableTab == mod.GetInstanceID(); reusedEditors &= stableEditor == editor.GetInstanceID();
                maximumWarm = Math.Max(maximumWarm, elapsed);
            }
            page.Hide(true, false); yield return null; yield return null;
        }
        Check(reusedTabs, "unchanged reopen reuses existing MOD tabs without synchronous destroy and clone");
        Check(reusedEditors, "unchanged reopen reuses existing setting editors without synchronous destroy and clone");
        Check(maximumWarm < 50, "six unchanged reopens each complete submit within 50ms, max=" + maximumWarm.ToString("F3"));

        fixture["Types", "boolean"].BoxedValue = true;
        Submit(entrance, page); yield return null; yield return null; yield return null;
        Check(page.Right.Content.Find("Setting_Types_boolean/Toggle").GetComponentInChildren<T17Text>().text == Texts.Boolean(true),
            "reused page refreshes values changed while hidden");
        Check(page.Right.Content.Find("Setting_Types_boolean").GetInstanceID() == stableEditor, "hidden value changes preserve editor instances");
        fixture.Bind("Opening", "AddedWhileVisible", true, "Structural refresh");
        ModSettingsPlugin.Instance.RefreshV1();
        yield return new WaitForSecondsRealtime(.4f); yield return null; yield return null;
        Check(page.Right.Content.Find("Setting_Opening_AddedWhileVisible") != null, "explicit refresh discovers a new config entry");
        CheckRaycast(page.Right.Content.Find("Setting_Types_boolean/Toggle").GetComponent<T17Button>(), page.Domain, "reused page controls retain actual raycast reachability");
        WriteGeometry(page);
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "opening-performance.png")); yield return new WaitForEndOfFrame();
        page.Hide(true, false);
        fixture.Bind("Opening", "AddedWhileHidden", true, "Hidden structural refresh");
        Submit(entrance, page); yield return null; yield return null; yield return null;
        Check(page.Right.Content.Find("Setting_Opening_AddedWhileHidden") != null, "reopen discovers entries bound without a value notification");
        int created = 0, disposed = 0, refreshed = 0;
        IDisposable renderer = ModSettingsPlugin.Instance.RegisterRendererV1(fixture["Types", "boolean"], new Dictionary<string, object> {
            { "Version", 1 }, { "Create", new Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>(
                delegate(RectTransform parent, ConfigEntryBase entry, IDictionary<string, object> host) {
                    created++;
                    ((Action<IDictionary<string, object>>)host["SetCallbacks"])(new Dictionary<string, object> {
                        { "Refresh", new Action(delegate { refreshed++; }) }, { "Cancel", new Action(delegate {}) }
                    });
                    return new ReleaseAction(delegate { disposed++; });
                }) }
        });
        yield return new WaitForSecondsRealtime(.4f); yield return null; yield return null;
        Check(created == 1 && disposed == 0, "renderer registration creates exactly one current instance");
        page.Hide(true, false); yield return null;
        int beforeRefresh = refreshed;
        Submit(entrance, page); yield return null; yield return null; yield return null;
        Check(created == 1 && disposed == 0 && refreshed > beforeRefresh, "unchanged reopen retains custom renderer and refreshes its values");
        renderer.Dispose(); yield return new WaitForSecondsRealtime(.4f); yield return null; yield return null;
        Check(disposed == 1 && page.Right.Content.Find("Setting_Types_boolean/Toggle") != null, "renderer release disposes retained instance once and restores builtin control");
        NativeTextInput remembered = page.Right.Content.Find("Setting_Types_vector3").GetComponentsInChildren<NativeTextInput>()[1];
        page.Domain.SetSelectedGameObject(remembered.gameObject); yield return null; yield return null;
        float scroll = page.Right.Content.anchoredPosition.y;
        int rememberedId = remembered.GetInstanceID();
        page.Hide(true, false); yield return null;
        Submit(entrance, page); yield return null; yield return null; yield return null;
        Check(Mathf.Abs(page.Right.Content.anchoredPosition.y - scroll) < 1, "unchanged reopen preserves remembered settings scroll");
        T17Button activeTab = page.Left.Content.Find("Mod_fixture.00").GetComponent<T17Button>();
        Check(activeTab.GetComponent<ModPin>().Button.navigation.selectOnRight.GetInstanceID() == rememberedId,
            "unchanged reopen navigates back to remembered compound child");
        page.Hide(true, false);
        fixture.Remove(new ConfigDefinition("Opening", "AddedWhileHidden"));
        Submit(entrance, page); yield return null; yield return null; yield return null;
        Check(page.Right.Content.Find("Setting_Opening_AddedWhileHidden") == null, "reopen removes entries deleted while hidden");
        page.Hide(true, false);
        int cachedId = page.GetInstanceID(), cachedDomain = page.Domain.GetInstanceID();
        NativeEntry entryPoint = (NativeEntry)AccessTools.Field(typeof(ModSettingsPlugin), "entry").GetValue(ModSettingsPlugin.Instance);
        entryPoint.Dispose(); yield return null; yield return null;
        entryPoint.Refresh(tab.GetComponent<FrontendMenuBehaviour>(), root.CurrentGamepadUser); yield return null;
        entrance = tab.Find("ModSettingsEntry").GetComponent<T17Button>();
        ExecuteEvents.Execute(entrance.gameObject, new BaseEventData(root.CachedEventSystem), ExecuteEvents.submitHandler);
        page = Shortcut.CachedPage;
        Check(page.ContentReady && page.GetInstanceID() == cachedId && page.Domain.GetInstanceID() == cachedDomain,
            "replacing scene entry retains completed global page and domain");
        page.Hide(true, false); Shortcut.Dispose(); yield return null; yield return null;
        Shortcut.Tick(false); page = Shortcut.CachedPage; Shortcut.Open();
        Check(!page.ContentReady, "new global page defers initial editor creation");
        page.Hide(true, false); yield return null; yield return null;
        Check(!page.gameObject.activeSelf && !page.ContentReady, "closing during first load cancels pending construction");
        Submit(entrance, page);
        float deadline = Time.realtimeSinceStartup + 3;
        while (!page.ContentReady && Time.realtimeSinceStartup < deadline) yield return null;
        Check(page.ContentReady && page.Right.Content.Find("Setting_Types_boolean") != null, "cancelled initial load can reopen and finish normally");
        page.Hide(true, false);
    }
}
