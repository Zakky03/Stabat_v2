using Fusion;
using UnityEngine;
using KoitanLib;

namespace Koitan
{
    public class PlayerAvatar : NetworkBehaviour
    {
        [Networked] public int PlayerIndex { get; set; }
        [Networked] public int TeamIndex { get; set; } = -1;
        [Networked] private NetworkBool FacingRight { get; set; }

        private Animator animator;
        private PlatformerMotor2D motor;
        private CharaColorChanger charaColorChanger;

        [SerializeField] private CharaLibrarySets librarySets;
        [SerializeField] private Transform handTf;

        private OnlineShopController nearShop = null;
        private Bomb nearBomb = null;
        private Bomb grabedBomb = null;

        private float inoperableTime = 0f;
        private float invincibleTime = 0f;

        private NetworkButtons previousButtons;

        private void Awake()
        {
            TryGetComponent(out animator);
            TryGetComponent(out motor);
            TryGetComponent(out charaColorChanger);
        }

        public override void Render()
        {
            transform.localScale = FacingRight ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
        }

        public override void FixedUpdateNetwork()
        {
            //Debug.Log($"[PlayerAvatar] FixedUpdateNetwork name={name}, HasInputAuthority={HasInputAuthority}");

            if (!HasStateAuthority)
                return;

            if (!GetInput(out NetworkInputData input))
            {
                Debug.LogWarning($"[PlayerAvatar] GetInput failed name={name}");
                return;
            }

            //Debug.Log($"[PlayerAvatar] input Stick={input.Stick}");

            float deltaTime = Runner.DeltaTime;

            animator.SetBool("Fall", motor.IsFalling());
            animator.SetBool("Ground", motor.IsGrounded());
            animator.SetBool("Damage", IsInoperable() && IsInvincible());

            if (invincibleTime > 0f)
            {
                invincibleTime -= deltaTime;
            }

            if (inoperableTime > 0f)
            {
                inoperableTime -= deltaTime;
                motor.normalizedXMovement = 0f;
                motor.normalizedYMovement = 0f;
                previousButtons = input.Buttons;
                return;
            }

            Vector2 stick = input.Stick;

            if (stick.x > 0.1f)
            {
                animator.SetBool("Run", true);
                FacingRight = true;
            }
            else if (stick.x < -0.1f)
            {
                animator.SetBool("Run", true);
                FacingRight = false;
            }
            else
            {
                animator.SetBool("Run", false);
            }

            motor.normalizedXMovement = stick.x;
            motor.normalizedYMovement = stick.y;

            bool jumpDown = input.Buttons.WasPressed(previousButtons, PlayerButtons.Jump);
            bool jumpHeld = input.Buttons.IsSet(PlayerButtons.Jump);
            bool actionDown = input.Buttons.WasPressed(previousButtons, PlayerButtons.Action);

            if (jumpDown && motor.IsGrounded())
            {
                animator.Play("Jump");
                motor.Jump();
            }

            motor.jumpingHeld = jumpHeld;

            if (actionDown)
            {
                if (nearShop != null)
                {
                    //nearShop.BuildShop(TeamIndex);
                    nearShop.TryBuildShop(TeamIndex);   // ƒ`[ƒ€index‚Í‚Ü‚¾
                }
                else if (grabedBomb != null)
                {
                    animator.Play("Throw");
                    SetInoperableTime(0.25f);
                }
                else if (nearBomb != null)
                {
                    nearBomb.Pick(handTf, PlayerIndex);
                    grabedBomb = nearBomb;
                    nearBomb = null;
                }
            }

            previousButtons = input.Buttons;
        }

        public void SetInoperableTime(float time)
        {
            inoperableTime = time;
        }

        public void SetInvincibleTime(float time)
        {
            if (time > invincibleTime)
            {
                invincibleTime = time;

                if (charaColorChanger != null)
                    charaColorChanger.SetFlashTime(time);
            }
        }

        public bool IsInoperable()
        {
            return inoperableTime > 0f;
        }

        public bool IsInvincible()
        {
            return invincibleTime > 0f;
        }

        public void ThrowBomb()
        {
            if (grabedBomb != null)
            {
                grabedBomb.Throw(new Vector3(10 * transform.localScale.x, 2, 0));
                grabedBomb = null;
            }
        }

        public void ChangeColor(int playerIndex, int teamIndex)
        {
            PlayerIndex = playerIndex;
            TeamIndex = teamIndex;

            if (charaColorChanger != null)
                charaColorChanger.ChangeColor(playerIndex, teamIndex);
        }

        public void AddPowerVec(Vector2 vec)
        {
            motor.velocity = vec;
        }

        public void SetDamage(Vector2 vec, float time)
        {
            if (IsInvincible())
                return;

            AddPowerVec(vec);
            SetInoperableTime(time);
            SetInvincibleTime(time * 2f);

            if (animator != null)
                animator.Play("Damage");
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            switch (collision.tag)
            {
                case "Land":
                    if (nearShop == null)
                    {
                        //nearShop = collision.transform.parent.GetComponent<ShopController>();
                        nearShop = collision.transform.parent.GetComponent<OnlineShopController>();
                    }
                    break;

                case "Bomb":
                    if (nearBomb == null)
                    {
                        nearBomb = collision.transform.parent.GetComponent<Bomb>();
                    }
                    break;
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            switch (collision.tag)
            {
                case "Land":
                    nearShop = null;
                    break;

                case "Bomb":
                    nearBomb = null;
                    break;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Money"))
            {
                Money money = collision.collider.GetComponentInParent<Money>();

                if (money.IsGetable(TeamIndex))
                {
                    BattleManager.Moneys[PlayerIndex] += (int)money.GetMoney();
                }
            }
        }
    }
}