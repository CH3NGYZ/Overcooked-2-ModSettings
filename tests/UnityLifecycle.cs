using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed partial class ModSettingsUnityTests
{
    private IEnumerator VerifySceneRebuild(FrontendRootMenu oldRoot)
    {
        int oldId = oldRoot.GetInstanceID();
        NativePage retained = Shortcut.CachedPage;
        Shortcut.Open(); yield return null; yield return null;
        VerifyFontAssets(retained, "before native scene unload");
        int pageId = retained.GetInstanceID(), domainId = retained.Domain.GetInstanceID();
        LoadingScreenFlow.LoadScene("StartScreen", GameState.NotSet);
        float deadline = Time.realtimeSinceStartup + 60;
        FrontendRootMenu current = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            AcknowledgeKnownNotice();
            current = T17FrontendFlow.Instance == null ? null : T17FrontendFlow.Instance.m_Rootmenu;
            if (current != null && current.GetInstanceID() != oldId && NativeHooks.ProfileReady
                && current.CurrentGamepadUser != null && !LoadingScreenFlow.IsLoading) break;
            yield return null;
        }
        Check(Shortcut.ActivePage == retained && retained.GetInstanceID() == pageId && retained.Domain.GetInstanceID() == domainId,
            "same DDOL page and independent domain survive actual frontend unload and rebuild");
        CheckRaycast(retained.Left.Content.GetComponentInChildren<T17Button>(), retained.Domain,
            "retained template controls still raycast after native bundle unload");
        foreach (T17Text text in retained.GetComponentsInChildren<T17Text>())
            Check(text.font != null && text.font.material != null, "retained font and material survive scene unload: " + text.name);
        VerifyFontAssets(retained, "after native scene unload");
        WriteSceneRecovery(current, "before hide");
        retained.Hide(false, false); yield return null; yield return null;
        WriteSceneRecovery(current, "two frames after hide");
        deadline = Time.realtimeSinceStartup + 8;
        while (!NativeHooks.Ready(current) && Time.realtimeSinceStartup < deadline)
        { AcknowledgeKnownNotice(); yield return null; }
        WriteSceneRecovery(current, "settled after hide");
        Check(current != null && current.GetInstanceID() != oldId && NativeHooks.Ready(current), "native loading flow recreates a distinct interactive frontend");
        Transform tab = current.transform.Find("WorldSpaceCanvas/Safezone_Rootmenu/RootTab/CustomizationOptions");
        tab.GetComponent<FrontendMenuBehaviour>().Show(current.CurrentGamepadUser, current, null, false);
        yield return new WaitForSecondsRealtime(.7f);
        Transform entrance = tab.Find("ModSettingsEntry");
        Check(entrance != null && tab.GetComponentsInChildren<T17Button>(true).Length > 0, "rebuilt frontend creates settings entry");
        ExecuteEvents.Execute(entrance.gameObject, new BaseEventData(current.CachedEventSystem), ExecuteEvents.submitHandler);
        yield return new WaitForSecondsRealtime(.4f);
        NativePage page = Shortcut.CachedPage;
        Check(page != null && page.gameObject.activeInHierarchy && page.Domain != current.CachedEventSystem, "rebuilt frontend reuses independent global input domain");
        Check(Resources.FindObjectsOfTypeAll<NativePage>().Length == 1 && tab.Find("ModSettingsEntry (Clone)") == null, "scene rebuild retains one settings page and entry");
        CheckRaycast(page.Left.Content.GetComponentInChildren<T17Button>(), page.Domain, "rebuilt page receives real canvas raycasts");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "settings-after-scene-rebuild.png"));
        // CaptureScreenshot reads the end-of-frame state. Keep the page open through the capture.
        yield return new WaitForEndOfFrame();
        yield return null;
        File.WriteAllText(Path.Combine(output, "scene-rebuild.txt"), "OldRoot=" + oldId + "\nNewRoot=" + current.GetInstanceID() + "\nDomain=" + page.Domain.GetInstanceID());
        page.Hide(true, false); yield return null;
    }
    private void WriteSceneRecovery(FrontendRootMenu current, string stage)
    {
        string report = stage + " profile=" + NativeHooks.ProfileReady + " loading=" + LoadingScreenFlow.IsLoading
            + " globalDialogs=" + T17DialogBoxManager.HasGlobalDialogs()
            + " anyDialogs=" + T17DialogBoxManager.HasAnyOpenDialogs();
        if (current != null)
        {
            report += " rootId=" + current.GetInstanceID() + " active=" + current.isActiveAndEnabled
                + " userPresent=" + (current.CurrentGamepadUser != null) + " ready=" + NativeHooks.Ready(current);
            T17EventSystem domain = current.CachedEventSystem;
            report += " domainPresent=" + (domain != null);
            if (domain != null)
            {
                report += " domainId=" + domain.GetInstanceID() + " disabled=" + domain.IsDisabled();
                List<Suppressor> tokens = (List<Suppressor>)AccessTools.Field(typeof(SuppressionController), "m_suppressors")
                    .GetValue(domain.SuppressionController);
                report += " tokenCount=" + tokens.Count;
                foreach (Suppressor token in tokens)
                {
                    report += Environment.NewLine + "  released=" + token.IsReleased();
                    foreach (System.Reflection.FieldInfo field in token.GetType().GetFields(
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic))
                    {
                        UnityEngine.Object owner = field.GetValue(token) as UnityEngine.Object;
                        if (owner != null) report += " owner=" + owner.name + " type=" + owner.GetType().Name;
                    }
                }
            }
        }
        foreach (T17DialogBox dialog in Resources.FindObjectsOfTypeAll<T17DialogBox>())
            if (dialog.IsActive && dialog.m_Title != null) report += Environment.NewLine + "  dialog=" + dialog.m_Title.text;
        File.AppendAllText(Path.Combine(output, "scene-recovery.txt"), report + Environment.NewLine);
    }
    private void Move(Component target, NativePage page, MoveDirection direction, Vector2 vector)
    { ExecuteEvents.Execute(target.gameObject, new AxisEventData(page.Domain) { moveDir = direction, moveVector = vector }, ExecuteEvents.moveHandler); }
    private IEnumerator VerifyLifecycle(NativePage page, T17Button entrance, FrontendRootMenu root)
    {
        T17Button tab = page.Left.Content.Find("Mod_fixture.00").GetComponent<T17Button>();
        Submit(tab, page); yield return null; yield return null; yield return null;
        page.Domain.SetSelectedGameObject(tab.gameObject); yield return null; yield return null;
        Move(tab, page, MoveDirection.Right, Vector2.right); yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == tab.GetComponent<ModPin>().Button.gameObject, "native right enters the row pin without activating another MOD");
        Move(tab.GetComponent<ModPin>().Button, page, MoveDirection.Right, Vector2.right); yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject != null && page.Domain.currentSelectedGameObject.transform.IsChildOf(page.Right.Content), "native OnMove crosses from tab into settings");
        // The remembered child may be inside a compound row; its first control owns the cross-column edge.
        NativeTextInput firstAxis = page.Right.Content.Find("Setting_Types_vector3").GetComponentsInChildren<NativeTextInput>()[0];
        page.Domain.SetSelectedGameObject(firstAxis.gameObject); yield return null; yield return null;
        Move(firstAxis, page, MoveDirection.Left, Vector2.left);
        yield return null; yield return null;
        Check(page.Domain.currentSelectedGameObject == tab.gameObject, "unconsumed native left returns to active tab");
        NativeTextInput text = page.Right.Content.Find("Setting_Types_string").GetComponentInChildren<NativeTextInput>();
        text.Begin();
        // The independent page owns its cancel callback; BaseMenuBehaviour's dispatch requires
        // joining the native menu stack and rejects global pages above native dialogs.
        page.Cancel();
        Check(!text.HasPending && page.gameObject.activeSelf, "global cancel callback discards draft and retains page");
        ConfigEntryBase boolean = fixture["Types", "boolean"];
        IDictionary<string, object> customHost = null;
        IDisposable renderer = ModSettingsPlugin.Instance.RegisterRendererV1(boolean, new Dictionary<string, object> {
            { "Version", 1 }, { "Create", new Func<RectTransform, ConfigEntryBase, IDictionary<string, object>, IDisposable>(
                delegate(RectTransform parent, ConfigEntryBase entry, IDictionary<string, object> host) {
                    customHost = host; return new ReleaseAction(delegate {});
                }) }
        });
        yield return new WaitForSecondsRealtime(.3f); yield return null; yield return null;
        IDisposable capture = ((Func<IDisposable>)customHost["CaptureInput"])();
        Check(page.Domain.IsDisabled(), "custom renderer input lease suppresses native domain");
        capture.Dispose(); Check(!page.Domain.IsDisabled(), "custom renderer input lease releases immediately");
        IDictionary<string, object> result = ((Func<object, IDictionary<string, object>>)customHost["Commit"])(true);
        Check((bool)result["Success"] && (bool)result["ActualValue"], "custom renderer commit returns actual saved value");
        RectTransform row = page.Right.Content.Find("Setting_Types_boolean") as RectTransform;
        float oldHeight = row.rect.height, oldContentHeight = page.Right.Content.rect.height;
        ((Action<float>)customHost["SetHeight"])(oldHeight + 45); yield return null;
        Check(Mathf.Abs(row.rect.height - oldHeight - 45) < .1f && Mathf.Abs(page.Right.Content.rect.height - oldContentHeight - 45) < .1f, "custom renderer dynamic height updates row and scroll extent");
        renderer.Dispose(); yield return new WaitForSecondsRealtime(.3f);
        // A real ConfigFile whose parent is an ordinary file reliably fails Save without OS ACL changes.
        string blocker = Path.Combine(output, "save-blocker"); File.WriteAllText(blocker, "file");
        ConfigFile failed = new ConfigFile(Path.Combine(blocker, "blocked.cfg"), false); failed.SaveOnConfigSet = false;
        ConfigEntry<bool> failing = failed.Bind("General", "Enabled", false, "Save failure");
        IDisposable failingLease = ModSettingsPlugin.Instance.RegisterConfigV1("fixture.failure", "Save failure", "", failed);
        yield return new WaitForSecondsRealtime(.3f);
        Submit(page.Left.Content.Find("Mod_fixture.failure"), page); yield return null; yield return null; yield return null;
        Submit(page.Right.Content.Find("Setting_General_Enabled/Toggle"), page);
        T17Text explanation = (T17Text)AccessTools.Field(typeof(NativePage), "help").GetValue(page);
        Check(failing.Value && explanation.color.r > explanation.color.g, "save failure shows error and retains actual mutated value");
        failingLease.Dispose(); yield return new WaitForSecondsRealtime(.3f);
        tab = page.Left.Content.Find("Mod_fixture.00").GetComponent<T17Button>(); Submit(tab, page); yield return null; yield return null; yield return null;
        Submit(page.Right.Content.Find("Setting_Types_shortcut/Capture"), page); yield return null;
        T17EventSystem domain = page.Domain;
        page.gameObject.SetActive(false); yield return null; yield return null;
        Check(!domain.IsDisabled() && !page.Overlay.IsOpen && page.CurrentGamepadUser == null, "external disable releases capture and native menu ownership");
        List<BaseMenuBehaviour> children; root.GetChildMenus(out children);
        Check(children == null || !children.Contains(page), "external disable detaches native child menu");
        bool priorBlock = T17FrontendFlow.Instance.BlockFocusKitchen;
        T17FrontendFlow.Instance.BlockFocusKitchen = true;
        Submit(entrance, page); yield return new WaitForSecondsRealtime(.3f);
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
        fixture["Types", "string"].BoxedValue = "changed before reset";
        AccessTools.Method(typeof(NativePage), "RestoreDefaults").Invoke(page, null); yield return null;
        Submit(page.Overlay.Root.Find("Dialog/Choices/Viewport/Content/Choice_0"), page); yield return null;
        Check((string)fixture["Types", "string"].BoxedValue == "original" && (int)fixture["Types", "int"].BoxedValue == int.MinValue, "confirmed bulk defaults restore owning MOD values");
        // Use the page-owned clear button, through the native button event path.
        Submit(page.Right.Content.Find("Setting_Types_shortcut/Capture"), page); yield return null;
        Submit(page.Overlay.Root.Find("Dialog/Clear"), page); yield return null;
        Check(((KeyboardShortcut)fixture["Types", "shortcut"].BoxedValue).MainKey == KeyCode.None && !page.Domain.IsDisabled(), "capture clear commits empty shortcut and releases domain");
        Submit(page.transform.Find("SettingsBody/CloseModSettings"), page); yield return null;
        Check(!page.gameObject.activeSelf && T17FrontendFlow.Instance.BlockFocusKitchen, "close button closes immediately and preserves previous kitchen block");
        T17FrontendFlow.Instance.BlockFocusKitchen = priorBlock;
        Submit(entrance, page); yield return new WaitForSecondsRealtime(.3f);
    }
    private void VerifyHostIsolation()
    {
        if (expectedHosts < 2) return;
        ConfigFile a = Chainloader.PluginInfos["modsettings.fixture.host.a"].Instance.Config;
        ConfigFile b = Chainloader.PluginInfos["modsettings.fixture.host.b"].Instance.Config;
        Check(!ReferenceEquals(a, b) && a.ConfigFilePath != b.ConfigFilePath, "embedded hosts own distinct configuration files");
        object old = a["Host a", "Enabled"].BoxedValue;
        a["Host a", "Enabled"].BoxedValue = false;
        Check((bool)b["Host b", "Enabled"].BoxedValue && !b.ContainsKey(new ConfigDefinition("Host a", "Enabled")), "host mutation does not leak entries or values into another host");
        a["Host a", "Enabled"].BoxedValue = old;
        List<ModConfig> mods = ModSettingsPlugin.Instance.Catalog.Snapshot();
        ModConfig owner = mods.Find(delegate(ModConfig m) { return m.Guid == "modsettings.fixture.host.a"; });
        Check(owner != null && owner.Rows.Count == 1 && ReferenceEquals(owner.Rows[0].Entry.ConfigFile, a), "catalog groups host entries under their own GUID");
    }
}
