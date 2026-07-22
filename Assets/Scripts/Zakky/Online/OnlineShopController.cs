using Fusion;
using UnityEngine;

namespace Koitan
{
    public class OnlineShopController : NetworkBehaviour
    {
        const float MoneyIntervalSeconds = 2f;

        [SerializeField] private Transform landParent;
        [SerializeField] private Transform shopParent;
        [SerializeField] private SpriteRenderer shopOutline;
        [SerializeField] private NetworkPrefabRef moneyPrefab;
        [SerializeField] private Transform moneyInitTf;

        [Networked] public bool IsBuild { get; private set; }
        [Networked] public bool IsBroken { get; private set; }
        [Networked] public int TeamIndex { get; private set; }
        [Networked] private TickTimer moneyTimer { get; set; }
        [Networked] private NetworkObject currentMoney { get; set; }

        private bool appliedIsBuild;
        private bool appliedIsBroken;
        private int appliedTeamIndex;

        public override void Spawned()
        {
            ApplyVisual(true);
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!IsBuild || IsBroken)
                return;

            if (currentMoney != null && currentMoney.IsValid)
                return;

            if (moneyTimer.ExpiredOrNotRunning(Runner))
            {
                NetworkObject spawned = Runner.Spawn(moneyPrefab, moneyInitTf.position, Quaternion.identity);
                OnlineMoney money = spawned.GetComponent<OnlineMoney>();
                money.TeamColorIndex = TeamIndex;
                currentMoney = spawned;
                moneyTimer = TickTimer.CreateFromSeconds(Runner, MoneyIntervalSeconds);
            }
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

            if (currentMoney != null && currentMoney.IsValid)
            {
                OnlineMoney money = currentMoney.GetComponent<OnlineMoney>();
                if (money != null)
                    money.Release();
            }
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