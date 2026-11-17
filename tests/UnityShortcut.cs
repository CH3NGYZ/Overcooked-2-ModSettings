using System;
using System.Collections;
using System.IO;
using HarmonyLib;
using OC2ModSettings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed partial class ModSettingsUnityTests
{
    private NativeShortcut Shortcut
    { get { return (NativeShortcut)AccessTools.Field(typeof(ModSettingsPlugin), "shortcut").GetValue(ModSettingsPlugin.Instance); } }

    private IEnumerator ShortcutPress()
    {
        // A real KeyDown cannot occur twice in one frame, even if loading completes between
        // two end-of-frame coroutine continuations. Match that constraint in scripted input.
        if ((int)AccessTools.Field(typeof(NativeShortcut), "handledFrame").GetValue(Shortcut) == Time.frameCount)
            yield return null;
        // The exact service Tick used by Update; physical keyboard delivery is a separate check.
        Shortcut.Tick(true); yield return null; yield return null;
    }
    private IEnumerator VerifyShortcut(FrontendRootMenu root)
    {
        NativeShortcut shortcut = Shortcut;
        GameObject previous = root.CachedEventSystem.currentSelectedGameObject;
        IEnumerator press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        float deadline = Time.realtimeSinceStartup + 4;
        while (shortcut.ActivePage == null && Time.realtimeSinceStartup < deadline) yield return null;
        NativePage page = shortcut.ActivePage;
        Check(page != null && page.Domain != root.CachedEventSystem && page.Domain is GlobalEventSystem,
            "fixed-key path opens independent global domain without submitting chef entry");
        yield return new WaitForSecondsRealtime(.35f);
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
        NativeTextInput text = page.Right.Content.Find("Setting_Types_string").GetComponentInChildren<NativeTextInput>();
        object original = fixture["Types", "string"].BoxedValue;
        text.Begin(); text.ChangeText("saved before shortcut");
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        Check(shortcut.ActivePage == null && !text.HasPending && Equals("saved before shortcut", fixture["Types", "string"].BoxedValue), "fixed-key close ends edit and retains immediately saved text");
        Check(previous == null || root.CachedEventSystem.currentSelectedGameObject == previous, "frontend shortcut restores previous focus");
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        yield return new WaitForSecondsRealtime(.35f);
        page = shortcut.ActivePage;
        Submit(page.Right.Content.Find("Setting_Types_shortcut/Capture"), page); yield return null; yield return null;
        T17EventSystem domain = page.Domain;
        object oldKey = fixture["Types", "shortcut"].BoxedValue;
        Check(page.Overlay.IsOpen && domain.IsDisabled(), "key capture owns input before global toggle");
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        Check(shortcut.ActivePage == null && !domain.IsDisabled() && Equals(oldKey, fixture["Types", "shortcut"].BoxedValue), "fixed-key close overrides capture and releases input without assigning the key");
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        yield return new WaitForSecondsRealtime(.35f);
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-frontend.png")); yield return new WaitForEndOfFrame(); yield return null;
        shortcut.ActivePage.Hide(true, false); yield return null; yield return null;
        T17EventSystem native = root.CachedEventSystem;
        Suppressor blocked = native.Disable(this);
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        Check(shortcut.ActivePage == page && !domain.IsDisabled() && native.IsDisabled() && !blocked.IsReleased(),
            "fixed key opens above a live native input owner without releasing its token");
        shortcut.ActivePage.Hide(true, false); yield return null; yield return null;
        Check(native.IsDisabled() && !blocked.IsReleased(), "closing global page preserves another native input lease");
        native.ReleaseSuppressor(blocked);
        IEnumerator global = VerifyGlobalOverlay(root); while (global.MoveNext()) yield return global.Current;
    }
    private IEnumerator VerifyShortcutScenes()
    {
        FrontendRootMenu frontend = T17FrontendFlow.Instance.m_Rootmenu;
        FrontendCoopTabOptions arcade = frontend.GetComponentInChildren<FrontendCoopTabOptions>(true);
        Check(arcade != null, "native arcade controller available");
        arcade.OnCouchPlayClicked();
        float deadline = Time.realtimeSinceStartup + 65;
        while (Time.realtimeSinceStartup < deadline && (LobbyFlowController.Instance == null
            || LobbyFlowController.Instance.m_themeSelectMenu.CurrentGamepadUser == null || LoadingScreenFlow.IsLoading)) yield return null;
        Check(LobbyFlowController.Instance != null && !LoadingScreenFlow.IsLoading, "native couch arcade enters actual lobby scene");
        yield return null; yield return null;
        IEnumerator verify = VerifyGameplayShortcut("arcade"); while (verify.MoveNext()) yield return verify.Current;
        // Select an installed stock kitchen through the game's real lobby loader.
        ServerLobbyFlowController lobby = UnityEngine.Object.FindObjectOfType<ServerLobbyFlowController>();
        Check(lobby != null, "actual server lobby controller exists");
        ThemeSelectRootMenu themes = LobbyFlowController.Instance.m_themeSelectMenu;
        CarouselButton chosen = themes.GetCurrentButton();
        Check(chosen != null, "lobby has a focused native theme for space confirmation");
        ExecuteEvents.Execute(chosen.Button.gameObject, new BaseEventData(themes.CachedEventSystem), ExecuteEvents.submitHandler);
        deadline = Time.realtimeSinceStartup + 15;
        while (!LoadingScreenFlow.IsLoading && Time.realtimeSinceStartup < deadline) yield return null;
        Check(LoadingScreenFlow.IsLoading, "native lobby confirm starts kitchen loading after countdown");
        IEnumerator pending = ShortcutPress(); while (pending.MoveNext()) yield return pending.Current;
        NativePage loadingPage = Shortcut.ActivePage;
        Check(loadingPage != null && loadingPage.Domain is GlobalEventSystem, "fixed key opens above actual kitchen loading immediately");
        yield return new WaitForEndOfFrame(); Canvas.ForceUpdateCanvases();
        CheckGlobalCanvases(loadingPage, "kitchen loading");
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-loading.png")); yield return new WaitForEndOfFrame();
        deadline = Time.realtimeSinceStartup + 75;
        while (Time.realtimeSinceStartup < deadline)
        {
            bool inLevel = Team17.Online.ClientUserSystem.m_Users.Count > 0
                && Team17.Online.ClientUserSystem.m_Users._items[0].GameState == GameState.InLevel;
            if (inLevel && !LoadingScreenFlow.IsLoading && UnityEngine.Object.FindObjectOfType<ServerKitchenFlowControllerBase>() != null) break;
            yield return null;
        }
        Check(!LoadingScreenFlow.IsLoading && Team17.Online.ClientUserSystem.m_Users._items[0].GameState == GameState.InLevel,
            "native arcade theme selection reaches an actual playable kitchen at InLevel");
        deadline = Time.realtimeSinceStartup + 8;
        while (Shortcut.ActivePage == null && Time.realtimeSinceStartup < deadline) yield return null;
        Check(Shortcut.ActivePage == loadingPage, "the same global page survives actual loading into kitchen");
        pending = ShortcutPress(); while (pending.MoveNext()) yield return pending.Current;
        Check(Shortcut.ActivePage == null, "persistent kitchen page closes through the same global toggle");
        verify = VerifyGameplayShortcut("kitchen"); while (verify.MoveNext()) yield return verify.Current;
    }
    private IEnumerator VerifyGameplayShortcut(string scene)
    {
        RootMenu root = scene == "arcade" ? (RootMenu)LobbyFlowController.Instance.m_themeSelectMenu : T17InGameFlow.Instance.m_Rootmenu;
        BaseMenuBehaviour before = root.GetCurrentOpenMenu();
        PlayerControls[] players = UnityEngine.Object.FindObjectsOfType<PlayerControls>();
        bool[] suppressed = new bool[players.Length];
        for (int i = 0; i < players.Length; i++) suppressed[i] = players[i].IsSuppressed();
        IEnumerator press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        float deadline = Time.realtimeSinceStartup + 5;
        while (Shortcut.ActivePage == null && Time.realtimeSinceStartup < deadline) yield return null;
        yield return new WaitForSecondsRealtime(.5f);
        NativePage page = Shortcut.ActivePage;
        File.AppendAllText(Path.Combine(output, "shortcut-context.txt"), scene + " root=" + root.GetType().Name
            + " loading=" + LoadingScreenFlow.IsLoading + " scene=" + SceneManager.GetActiveScene().name
            + " domain=" + root.CachedEventSystem.GetInstanceID() + " active=" + root.CachedEventSystem.isActiveAndEnabled
            + " disabled=" + root.CachedEventSystem.IsDisabled() + "\n");
        Check(page != null && page.gameObject.activeInHierarchy, scene + ": global key opens actual page");
        System.Collections.Generic.List<BaseMenuBehaviour> children; root.GetChildMenus(out children);
        Check((children == null || !children.Contains(page)) && root.GetCurrentOpenMenu() == before
            && page.Domain != root.CachedEventSystem && page.Domain.isActiveAndEnabled && root.CachedEventSystem.IsDisabled(),
            scene + ": independent page suppresses native input without joining its menu stack");
        CheckRaycast(page.Left.Content.GetComponentInChildren<T17Button>(), page.Domain, scene + ": native controls receive topmost raycasts");
        foreach (PlayerControls player in players) Check(player.IsSuppressed(), scene + ": player input suppressed while settings open");
        Submit(page.Left.Content.Find("Mod_fixture.00"), page); yield return null; yield return null; yield return null;
        VerifyLayout(page);
        ScreenCapture.CaptureScreenshot(Path.Combine(output, "shortcut-" + scene + ".png"));
        WriteGeometry(page); yield return new WaitForEndOfFrame(); yield return null;
        // Back out of the settings column, then close through the same global input owner.
        page.Cancel(); yield return null; yield return null;
        page.Cancel();
        yield return null; yield return null;
        Check(Shortcut.ActivePage == null, scene + ": direct native cancel closes settings without invalid menu casts");
        Check(root.GetCurrentOpenMenu() == before, scene + ": original menu stack restored");
        for (int i = 0; i < players.Length; i++) Check(players[i].IsSuppressed() == suppressed[i], scene + ": prior player suppression restored");
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        yield return new WaitForSecondsRealtime(.3f);
        page = Shortcut.ActivePage;
        Submit(page.Right.Content.Find("Setting_Types_shortcut/Capture"), page); yield return null;
        T17EventSystem domain = page.Domain;
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        Check(Shortcut.ActivePage == null && !domain.IsDisabled() && root.GetCurrentOpenMenu() == before,
            scene + ": same fixed-key path closes during capture and restores game input ownership");
        press = ShortcutPress(); while (press.MoveNext()) yield return press.Current;
        page = Shortcut.ActivePage;
        Submit(page.Right.Content.Find("Setting_Types_shortcut/Capture"), page); yield return null;
        page.gameObject.SetActive(false); yield return null; yield return null;
        root.GetChildMenus(out children);
        Check(Shortcut.ActivePage == null && !domain.IsDisabled() && (children == null || !children.Contains(page)),
            scene + ": external disable releases capture and detaches child menu");
        for (int i = 0; i < players.Length; i++) Check(players[i].IsSuppressed() == suppressed[i], scene + ": external disable restores player input");
    }
}
