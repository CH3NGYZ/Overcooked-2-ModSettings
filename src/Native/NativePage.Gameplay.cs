using System.Collections.Generic;
using UnityEngine;

namespace OC2ModSettings
{
    internal sealed partial class NativePage
    {
        private sealed class PlayerLease { internal PlayerControls Player; internal Suppressor Token; }
        private sealed class DomainLease { internal T17EventSystem Domain; internal Suppressor Token; }
        private readonly List<PlayerLease> players = new List<PlayerLease>();
        private readonly List<DomainLease> domains = new List<DomainLease>();
        private GamepadEngagementManager engagementManager;
        private Suppressor engagement;
        private T17FrontendFlow blockedFrontend;
        private bool previousBlockKitchen;
        private void AcquireGameplayInput()
        {
            foreach (T17EventSystem domain in Resources.FindObjectsOfTypeAll<T17EventSystem>()) Track(domain);
            foreach (PlayerControls player in Resources.FindObjectsOfTypeAll<PlayerControls>()) Track(player);
            MaintainInput();
        }
        internal void Track(T17EventSystem domain)
        {
            if (!ownedState || domain == null || domain == Domain || !domain.gameObject.scene.IsValid()) return;
            foreach (DomainLease lease in domains) if (lease.Domain == domain) return;
            domains.Add(new DomainLease { Domain = domain, Token = domain.Disable(this) });
        }
        internal void Track(PlayerControls player)
        {
            if (!ownedState || player == null || !player.gameObject.scene.IsValid()) return;
            foreach (PlayerLease lease in players) if (lease.Player == player) return;
            players.Add(new PlayerLease { Player = player, Token = player.Suppress(this) });
        }
        internal void MaintainInput()
        {
            if (!ownedState) return;
            for (int i = domains.Count - 1; i >= 0; i--)
                if (domains[i].Domain == null) { domains[i].Token.Release(); domains.RemoveAt(i); }
            for (int i = players.Count - 1; i >= 0; i--)
                if (players[i].Player == null) { players[i].Token.Release(); players.RemoveAt(i); }
            // Native transitions may reset tokens; reacquire only our own released lease.
            foreach (DomainLease lease in domains)
                if (lease.Domain != null && lease.Token.IsReleased()) lease.Token = lease.Domain.Disable(this);
            foreach (PlayerLease lease in players)
                if (lease.Player != null && lease.Token.IsReleased()) lease.Token = lease.Player.Suppress(this);
            GamepadEngagementManager manager = GameUtils.RequestManager<GamepadEngagementManager>();
            if (manager != engagementManager || engagement == null || engagement.IsReleased())
            {
                if (engagement != null) engagement.Release();
                engagementManager = manager; engagement = manager == null ? null : manager.Suppressor.AddSuppressor(this);
            }
            T17FrontendFlow flow = T17FrontendFlow.Instance;
            if (flow != blockedFrontend)
            {
                if (blockedFrontend != null) blockedFrontend.BlockFocusKitchen = previousBlockKitchen;
                blockedFrontend = flow;
                if (flow != null) { previousBlockKitchen = flow.BlockFocusKitchen; flow.BlockFocusKitchen = true; }
            }
        }
        private void ReleaseGameplayInput()
        {
            foreach (DomainLease lease in domains)
                if (lease.Domain != null) lease.Domain.ReleaseSuppressor(lease.Token); else lease.Token.Release();
            domains.Clear();
            foreach (PlayerLease lease in players)
                if (lease.Player != null) lease.Player.ReleaseSuppressor(lease.Token);
                else lease.Token.Release();
            players.Clear();
            if (engagement != null) engagement.Release();
            engagement = null; engagementManager = null;
            if (blockedFrontend != null) blockedFrontend.BlockFocusKitchen = previousBlockKitchen;
            blockedFrontend = null;
        }
    }
}
