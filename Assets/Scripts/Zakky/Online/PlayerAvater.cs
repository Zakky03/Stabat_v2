using Fusion;
using KoitanLib;
using UnityEngine;

namespace Koitan
{
    public class PlayerAvatar : NetworkBehaviour, IBattlePlayer
    {
        [Networked] public int PlayerIndex { get; set; }
        [Networked] public int TeamIndex { get; set; } = -1;
        [Networked] public int Money { get; set; }
        [Networked] public NetworkBool IsReady { get; set; }
        // Set on this avatar by its own owning client when its local match ends (see
        // BattleManager.OwatiAnim()). BattleManager.OwatiAnim() leaves the scene via a plain,
        // non-Fusion SceneManager.LoadScene("Result") rather than a networked scene change, so this
        // is the only signal a newly-joining client has that the room it's connecting to already
        // wrapped up its match — used to make it wait instead of spawning into a stale room.
        [Networked] public NetworkBool HasFinishedMatch { get; set; }
        [Networked] public NetworkString<_32> Username { get; set; }
        // Local (per-device) rating, not a shared/server leaderboard — see
        // BattleManager.OwatiAnim() for how it's updated at match end.
        [Networked] public int Rating { get; set; }
        [Networked] private NetworkBool FacingRight { get; set; } = true;

        public const string RatingPrefsKey = "OnlineRating";
        public const int DefaultRating = 1500;

        private Animator animator;
        private PlatformerMotor2D motor;
        private CharaColorChanger charaColorChanger;

        [SerializeField] private CharaLibrarySets librarySets;
        [SerializeField] private Transform handTf;
        public Transform HandTf => handTf;

        // FesGame18 から移植した攻撃システム。未設定なら攻撃なしのキャラとして動く。
        [SerializeField] private PlayerAttack playerAttack;

        private OnlineShopController nearShop = null;
        private OnlineBomb nearBomb = null;
        private OnlineBomb grabedBomb = null;

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

        public override void Spawned()
        {
            // moneyUis visibility is recomputed every frame from BattleManager.OnlinePlayers
            // (see BattleManager.Update()), so adding to that list is all that's needed here.
            BattleManager.OnlinePlayers.Add(this);

            if (HasStateAuthority)
                Rating = PlayerPrefs.GetInt(RatingPrefsKey, DefaultRating);

            // Same weight/radius as the offline camera setup — offline players are pre-placed scene
            // objects, but online avatars are spawned dynamically by Fusion, so they must register
            // with the follow camera's target group themselves instead of being wired in the Editor.
            if (BattleManager.TargetGroup != null)
                BattleManager.TargetGroup.AddMember(transform, 1f, 3f);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            BattleManager.OnlinePlayers.Remove(this);

            if (BattleManager.TargetGroup != null)
                BattleManager.TargetGroup.RemoveMember(transform);
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

            if (!IsReady && input.Buttons.WasPressed(previousButtons, PlayerButtons.Ready))
            {
                IsReady = true;
            }

            // Before the battle actually starts (still waiting on Ready / playing the "3, 2, 1,
            // Go!" intro), players shouldn't be able to move or act at all — only pressing Ready
            // should do anything during this phase.
            if (!BattleManager.IsBattleStarted)
            {
                motor.normalizedXMovement = 0f;
                motor.normalizedYMovement = 0f;
                previousButtons = input.Buttons;
                return;
            }

            float deltaTime = Runner.DeltaTime;

            // 攻撃の進行。硬直中も進める必要があるので操作不能の判定より前で回す。
            // ここは HasStateAuthority のクライアントでしか回らないため、他プレイヤーの画面では
            // 攻撃モーションが進まない点に注意（当たり判定自体は攻撃側が持つので判定はズレない）。
            if (playerAttack != null)
            {
                playerAttack.Tick(deltaTime);
            }

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
                    nearShop.TryBuildShop(TeamIndex);   // チームindexはまだ
                }
                else if (grabedBomb != null)
                {
                    animator.Play("Throw");
                    SetInoperableTime(0.25f);
                }
                else if (nearBomb != null)
                {
                    nearBomb.Pick(this);
                    grabedBomb = nearBomb;
                    nearBomb = null;
                }
            }

            // 攻撃（X=近接、Y=飛び道具）。オフラインの PlayerController と同じ割り当て。
            if (playerAttack != null)
            {
                if (input.Buttons.WasPressed(previousButtons, PlayerButtons.Attack1))
                {
                    StartAttack(0);
                }
                else if (input.Buttons.WasPressed(previousButtons, PlayerButtons.Attack2))
                {
                    StartAttack(1);
                }
            }

            previousButtons = input.Buttons;
        }

        /// <summary>
        /// 攻撃を開始する。攻撃中は動けないよう、全体時間ぶんの硬直を入れる
        /// （硬直の解除は FixedUpdateNetwork の操作不能処理に任せる）。
        /// </summary>
        private void StartAttack(int index)
        {
            if (playerAttack.TryAttack(index))
            {
                SetInoperableTime(playerAttack.GetTotalTime(index));
            }
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

        // Only ever called on this avatar's own owning client (e.g. from the username input field
        // during the ready/waiting phase), which is also this avatar's state authority in Shared
        // mode, so setting the networked value directly is safe here.
        public void SetUsername(string name)
        {
            if (!HasStateAuthority)
                return;

            Username = name;
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

            // 殴られたら攻撃は中断する（判定が出しっぱなしになるのを防ぐ）
            if (playerAttack != null)
                playerAttack.Cancel();

            if (animator != null)
                animator.Play("Damage");
        }

        // IBattlePlayer の実装。PlayerIndex / TeamIndex は上の [Networked] プロパティがそのまま満たす。
        /// <summary>
        /// 向き。オンラインでは localScale ではなく [Networked] の FacingRight が正。
        /// （localScale は Render() で FacingRight から反映しているだけなので、
        /// FixedUpdateNetwork のタイミングでは1フレーム古い可能性がある）
        /// </summary>
        public int FacingSign => FacingRight ? 1 : -1;

        public Transform Transform => transform;

        /// <summary>
        /// ヒット判定は攻撃側の StateAuthority を持つクライアントだけが行う。
        /// こうしないと全クライアントで同じ攻撃が多重に判定されてしまう。
        /// </summary>
        public bool HasAttackAuthority => HasStateAuthority;

        /// <summary>
        /// 攻撃側のクライアントから呼ばれる。自分が殴られる側なので、
        /// 実際の適用は自分の StateAuthority 宛の RPC に委譲する。
        /// </summary>
        public void ApplyDamage(Vector2 knockback, float inoperableTime)
        {
            RPC_ApplyDamage(knockback, inoperableTime);
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
                        nearBomb = collision.transform.parent.GetComponent<OnlineBomb>();
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

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!HasStateAuthority)
                return;

            // Money uses a trigger (not solid OnCollisionEnter2D) because this avatar's own
            // Rigidbody2D is always Kinematic (PC2D's motor) and, on any client that isn't the
            // money's state authority, so is the money's — Unity 2D physics never raises collision
            // callbacks for a Kinematic-vs-Kinematic pair, only for trigger overlaps.
            if (collision.CompareTag("Money"))
            {
                OnlineMoney money = collision.GetComponentInParent<OnlineMoney>();

                if (money != null)
                {
                    money.TryPickup(TeamIndex);
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ApplyDamage(Vector2 vec, float time)
        {
            SetDamage(vec, time);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_CreditMoney(int amount)
        {
            Money += amount;
        }
    }
}