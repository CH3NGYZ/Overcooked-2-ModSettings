using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace OC2ModSettings
{
    internal static class NativeHooks
    {
        internal static bool ProfileReady;
        internal static void Install(Harmony harmony)
        {
            MethodInfo show = AccessTools.Method(typeof(FrontendMenuBehaviour), "Show");
            MethodInfo load = AccessTools.Method(typeof(PCSaveManager), "LoadProfile");
            if (show == null || load == null) throw new MissingMethodException("Native menu/profile hooks unavailable.");
            harmony.Patch(show, null, new HarmonyMethod(typeof(NativeHooks), "Shown"));
            harmony.Patch(load, null, new HarmonyMethod(typeof(NativeHooks), "Loaded"));
            harmony.Patch(AccessTools.Method(typeof(T17EventSystemsManager), "RegisterEventSystem"),
                null, new HarmonyMethod(typeof(NativeHooks), "DomainCreated"));
            harmony.Patch(AccessTools.Method(typeof(T17EventSystem), "ResetSystem"),
                null, new HarmonyMethod(typeof(NativeHooks), "DomainReset"));
            harmony.Patch(AccessTools.Method(typeof(PlayerControls), "OnEnable"),
                null, new HarmonyMethod(typeof(NativeHooks), "PlayerEnabled"));
            harmony.Patch(AccessTools.Method(typeof(InGameRootMenu), "DoNavigateOnUICancel"),
                new HarmonyMethod(typeof(NativeHooks), "GameCancel"));
            harmony.Patch(AccessTools.Method(typeof(ClientLobbyFlowController), "ShowLeaveDialog"),
                new HarmonyMethod(typeof(NativeHooks), "LobbyCancel"));
            harmony.Patch(AccessTools.Method(typeof(ClientLobbyFlowController), "ChangeTeam", new Type[] { typeof(Team17.Online.User) }),
                new HarmonyMethod(typeof(NativeHooks), "LobbyTeam"));
        }
        private static bool LobbyCancel()
        {
            try
            {
                NativePage page = ModSettingsPlugin.Instance == null ? null : ModSettingsPlugin.Instance.ActivePage;
                if (page == null) return true;
                page.Cancel(); return false;
            }
            catch (Exception error) { ModSettingsPlugin.Instance.Report("ClientLobbyFlowController.ShowLeaveDialog Prefix", error); return false; }
        }
        private static bool LobbyTeam()
        { return ModSettingsPlugin.Instance == null || ModSettingsPlugin.Instance.ActivePage == null; }
        private static bool GameCancel(InGameRootMenu __instance)
        {
            try
            {
                NativePage page = ModSettingsPlugin.Instance == null ? null : ModSettingsPlugin.Instance.ActivePage;
                if (page == null) return true;
                page.Cancel(); return false;
            }
            catch (Exception error) { ModSettingsPlugin.Instance.Report("InGameRootMenu.DoNavigateOnUICancel Prefix", error); return false; }
        }
        private static void DomainCreated(T17EventSystem eventSystem)
        { if (ModSettingsPlugin.Instance != null) ModSettingsPlugin.Instance.Track(eventSystem); }
        private static void DomainReset(T17EventSystem __instance)
        {
            if (ModSettingsPlugin.Instance == null) return;
            ModSettingsPlugin.Instance.Track(__instance);
            if (ModSettingsPlugin.Instance.ActivePage != null) ModSettingsPlugin.Instance.ActivePage.MaintainInput();
        }
        private static void PlayerEnabled(PlayerControls __instance)
        { if (ModSettingsPlugin.Instance != null) ModSettingsPlugin.Instance.Track(__instance); }
        private static void Shown(FrontendMenuBehaviour __instance, GamepadUser __0)
        {
            try { if (ModSettingsPlugin.Instance != null) ModSettingsPlugin.Instance.FrontendShown(__instance, __0); }
            catch (Exception e) { ModSettingsPlugin.Instance.Report("FrontendMenuBehaviour.Show Postfix", e); }
        }
        private static void Loaded(ref IEnumerator<SaveLoadResult?> __result)
        {
            try { ProfileReady = false; __result = Observe(__result); }
            catch (Exception e) { ModSettingsPlugin.Instance.Report("PCSaveManager.LoadProfile Postfix", e); }
        }
        private static IEnumerator<SaveLoadResult?> Observe(IEnumerator<SaveLoadResult?> original)
        {
            using (original)
                while (original.MoveNext())
                {
                    SaveLoadResult? current = original.Current;
                    // Native NotExist means a successfully initialized fresh profile, not a corrupt read.
                    if (current == SaveLoadResult.Exists || current == SaveLoadResult.NotExist) ProfileReady = true;
                    yield return current;
                }
        }
        internal static bool Ready(FrontendRootMenu root)
        {
            return ProfileReady && root != null && root.isActiveAndEnabled && root.CurrentGamepadUser != null
                && root.CachedEventSystem != null && !root.CachedEventSystem.IsDisabled()
                && !LoadingScreenFlow.IsLoading && T17DialogBoxManager.Instance != null
                && T17EventSystemsManager.Instance != null && !T17DialogBoxManager.HasGlobalDialogs()
                && !T17DialogBoxManager.HasAnyOpenDialogs();
        }
    }
}
