using Fusion;
using UnityEngine;

namespace Koitan
{
    public class OnlineMoney : NetworkBehaviour
    {
        [SerializeField] SpriteRenderer outline;

        const float ValueMin = 1000f;
        const float ValueMax = 10000f;
        const float ScaleMin = 1f;
        const float ScaleMax = 2f;
        const float GrowthSpeed = 500f;

        [Networked] public float MoneyValue { get; set; }
        [Networked] public int TeamColorIndex { get; set; } = -1;
        [Networked] private NetworkBool IsGrowing { get; set; }
        [Networked] private NetworkBool IsConsumed { get; set; }

        public override void Spawned()
        {
            Debug.Log($"[OnlineMoney] Spawned name={name} HasStateAuthority={HasStateAuthority} IsProxy={Object.IsProxy} Id={Object.Id}");

            if (HasStateAuthority)
                IsGrowing = true;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority && IsGrowing)
            {
                MoneyValue += GrowthSpeed * Runner.DeltaTime;
            }
        }

        public override void Render()
        {
            float scale;
            if (MoneyValue < ValueMin)
                scale = ScaleMin;
            else if (MoneyValue > ValueMax)
                scale = ScaleMax;
            else
                scale = ScaleMin + (ScaleMax - ScaleMin) * (MoneyValue - ValueMin) / (ValueMax - ValueMin);

            transform.localScale = Vector3.one * scale;
            outline.color = TeamColorIndex < 0 ? Color.white : BattleManager.ColorSets.colors[TeamColorIndex];
        }

        // Called by the owning OnlineShopController when the shop backing it is broken.
        public void Release()
        {
            if (!HasStateAuthority)
                return;

            TeamColorIndex = -1;
            IsGrowing = false;
        }

        public bool IsGetable(int pickerTeamIndex)
        {
            if (TeamColorIndex < 0)
                return true;

            return TeamColorIndex == pickerTeamIndex;
        }

        public void TryPickup(int pickerTeamIndex)
        {
            if (IsConsumed)
                return;

            if (!IsGetable(pickerTeamIndex))
                return;

            RPC_RequestConsume(Runner.LocalPlayer);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestConsume(PlayerRef requester)
        {
            if (IsConsumed)
                return;

            IsConsumed = true;

            foreach (PlayerAvatar avatar in BattleManager.OnlinePlayers)
            {
                if (avatar.Object.InputAuthority == requester)
                {
                    avatar.RPC_CreditMoney((int)MoneyValue);
                    break;
                }
            }

            Runner.Despawn(Object);
        }
    }
}
