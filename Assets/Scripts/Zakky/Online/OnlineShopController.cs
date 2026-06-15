using Fusion;
using UnityEngine;

namespace Koitan
{
    public class OnlineShopController : NetworkBehaviour
    {
        [SerializeField] private Transform landParent;
        [SerializeField] private Transform shopParent;
        [SerializeField] private SpriteRenderer shopOutline;

        [Networked] public bool IsBuild { get; private set; }
        [Networked] public bool IsBroken { get; private set; }
        [Networked] public int TeamIndex { get; private set; }

        private bool appliedIsBuild;
        private bool appliedIsBroken;
        private int appliedTeamIndex;

        public override void Spawned()
        {
            ApplyVisual(true);
        }

        public override void Render()
        {
            ApplyVisual(false);
        }

        public void TryBuildShop(int teamIndex)
        {
            if (IsBuild)
                return;

            if (HasStateAuthority)
            {
                BuildShopNetworked(teamIndex);
            }
            else
            {
                RPC_RequestBuildShop(teamIndex);
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestBuildShop(int teamIndex)
        {
            if (IsBuild)
                return;

            BuildShopNetworked(teamIndex);
        }

        private void BuildShopNetworked(int teamIndex)
        {
            IsBuild = true;
            IsBroken = false;
            TeamIndex = teamIndex;
        }

        public void TryBrokenShop()
        {
            if (!IsBuild)
                return;

            if (HasStateAuthority)
            {
                BrokenShopNetworked();
            }
            else
            {
                RPC_RequestBrokenShop();
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestBrokenShop()
        {
            if (!IsBuild)
                return;

            BrokenShopNetworked();
        }

        private void BrokenShopNetworked()
        {
            IsBuild = false;
            IsBroken = true;
        }

        private void ApplyVisual(bool force)
        {
            if (!force &&
                appliedIsBuild == IsBuild &&
                appliedIsBroken == IsBroken &&
                appliedTeamIndex == TeamIndex)
            {
                return;
            }

            appliedIsBuild = IsBuild;
            appliedIsBroken = IsBroken;
            appliedTeamIndex = TeamIndex;

            if (landParent != null)
                landParent.gameObject.SetActive(!IsBuild && !IsBroken);

            if (shopParent != null)
                shopParent.gameObject.SetActive(IsBuild);

            if (shopOutline != null && TeamIndex >= 0)
                shopOutline.color = BattleManager.ColorSets.colors[TeamIndex];
        }
    }
}