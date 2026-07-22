using Fusion;
using UnityEngine;

namespace Koitan
{
    public class OnlineBomb : NetworkBehaviour
    {
        Rigidbody2D rb;
        [SerializeField] GameObject body;
        [SerializeField] GameObject itemArea;
        [SerializeField] GameObject eff;
        [SerializeField] GameObject explosionArea;

        [Networked] public NetworkBool IsPicked { get; set; }
        [Networked] public NetworkBool IsThrown { get; set; }
        [Networked] public NetworkBool IsFired { get; set; }
        [Networked] public int HolderPlayerIndex { get; set; } = -1;
        [Networked] private TickTimer despawnTimer { get; set; }

        // Mirrors offline Bomb's "isIgnited": stays true from the moment it's first picked up
        // (physics collisions can't fire while held anyway, since rb.simulated is false then).
        bool ignited;

        public override void Spawned()
        {
            TryGetComponent(out rb);
            Debug.Log($"[OnlineBomb] Spawned name={name} HasStateAuthority={HasStateAuthority} IsProxy={Object.IsProxy} Id={Object.Id}");
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (IsPicked)
            {
                Transform holderHand = FindHolderHandTf();
                if (holderHand != null)
                    transform.position = holderHand.position;
            }

            if (despawnTimer.Expired(Runner))
            {
                despawnTimer = TickTimer.None;
                Runner.Despawn(Object);
            }
        }

        Transform FindHolderHandTf()
        {
            foreach (PlayerAvatar avatar in BattleManager.OnlinePlayers)
            {
                if (avatar.PlayerIndex == HolderPlayerIndex)
                    return avatar.HandTf;
            }
            return null;
        }

        // Bombs never transfer state authority (avoids depending on Fusion's "Allow State Authority
        // Override" NetworkObject setting): the spawner keeps authority for the object's whole lifetime,
        // and every other client routes pick/throw through an RPC to whoever that is.
        public void Pick(PlayerAvatar picker)
        {
            if (IsPicked)
                return;

            if (HasStateAuthority)
                DoPick(picker.PlayerIndex);
            else
                RPC_RequestPick(picker.PlayerIndex);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestPick(int pickerPlayerIndex)
        {
            DoPick(pickerPlayerIndex);
        }

        void DoPick(int holderPlayerIndex)
        {
            if (IsPicked)
                return;

            ignited = true;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;

            HolderPlayerIndex = holderPlayerIndex;
            IsPicked = true;
            IsThrown = false;
        }

        public void Throw(Vector3 speed)
        {
            if (HasStateAuthority)
                DoThrow(speed);
            else
                RPC_RequestThrow(speed);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestThrow(Vector3 speed)
        {
            DoThrow(speed);
        }

        void DoThrow(Vector3 speed)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.linearVelocity = speed;

            IsPicked = false;
            IsThrown = true;
        }

        public void Explosion()
        {
            if (!HasStateAuthority)
                return;

            if (IsFired)
                return;

            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            IsFired = true;
            despawnTimer = TickTimer.CreateFromSeconds(Runner, 1f);
        }

        public override void Render()
        {
            body.SetActive(!IsFired);
            itemArea.SetActive(!IsFired);
            eff.SetActive(IsFired);
            explosionArea.SetActive(IsFired);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!HasStateAuthority)
                return;

            if (!ignited)
                return;

            if (collision.collider.CompareTag("Player"))
            {
                PlayerAvatar avatar = collision.collider.GetComponent<PlayerAvatar>();
                if (avatar != null && avatar.PlayerIndex == HolderPlayerIndex)
                    return;
            }

            Explosion();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!HasStateAuthority)
                return;

            if (!IsFired)
                return;

            switch (collision.tag)
            {
                case "Player":
                    PlayerAvatar avatar = collision.GetComponent<PlayerAvatar>();
                    if (avatar == null || avatar.PlayerIndex == HolderPlayerIndex)
                        return;

                    Vector2 dir = new Vector2(Mathf.Sign(avatar.transform.position.x - transform.position.x), 1) * 35f;
                    avatar.RPC_ApplyDamage(dir, 1f);
                    break;

                case "Shop":
                    OnlineShopController shop = collision.transform.parent.GetComponent<OnlineShopController>();
                    if (shop != null)
                        shop.TryBrokenShop();
                    break;

                case "Bomb":
                    if (collision.isTrigger)
                        return;

                    OnlineBomb otherBomb = collision.transform.parent.GetComponent<OnlineBomb>();
                    if (otherBomb != null)
                        otherBomb.Explosion();
                    break;
            }
        }
    }
}
