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

        // Local-only (not networked): set on the requesting client while waiting for
        // Object.RequestStateAuthority() to actually grant authority, since that's a round trip.
        bool pendingPickup;
        PlayerAvatar pendingPicker;

        // Only meaningful on whichever client currently HasStateAuthority; who the bomb should
        // follow this tick while held. Read directly (no network lookup) once authority is transferred.
        PlayerAvatar heldBy;

        public override void Spawned()
        {
            TryGetComponent(out rb);
            Debug.Log($"[OnlineBomb] Spawned name={name} HasStateAuthority={HasStateAuthority} IsProxy={Object.IsProxy} Id={Object.Id}");
        }

        public override void FixedUpdateNetwork()
        {
            if (pendingPickup && HasStateAuthority)
            {
                DoPick(pendingPicker);
                pendingPickup = false;
                pendingPicker = null;
            }

            if (!HasStateAuthority)
                return;

            if (IsPicked && heldBy != null)
            {
                transform.position = heldBy.HandTf.position;
            }

            if (despawnTimer.Expired(Runner))
            {
                despawnTimer = TickTimer.None;
                Runner.Despawn(Object);
            }
        }

        public void Pick(PlayerAvatar picker)
        {
            if (IsPicked)
                return;

            if (HasStateAuthority)
            {
                DoPick(picker);
            }
            else
            {
                pendingPickup = true;
                pendingPicker = picker;
                Object.RequestStateAuthority();
            }
        }

        void DoPick(PlayerAvatar picker)
        {
            if (IsPicked)
                return;

            ignited = true;
            heldBy = picker;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;

            HolderPlayerIndex = picker.PlayerIndex;
            IsPicked = true;
            IsThrown = false;
        }

        public void Throw(Vector3 speed)
        {
            if (!HasStateAuthority)
                return;

            heldBy = null;
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
