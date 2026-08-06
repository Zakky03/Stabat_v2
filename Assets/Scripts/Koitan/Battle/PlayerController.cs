using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KoitanLib;
//using UnityEngine.Experimental.U2D.Animation;
using Cinemachine;


namespace Koitan
{
    public class PlayerController : MonoBehaviour, IBattlePlayer
    {
        public int playerIndex;
        public int teamIndex = -1;
        Animator animator;
        PlatformerMotor2D motor;
        //[SerializeField]
        //GameObject mesh;
        CharaColorChanger charaColorChanger;
        /// <summary>
        /// 攻撃の進行管理。FesGame18から移植した攻撃システム。
        /// 未設定なら攻撃なしのキャラとして動く。
        /// </summary>
        [SerializeField]
        PlayerAttack playerAttack;
        [SerializeField]
        CharaLibrarySets librarySets;
        ShopController nearShop = null;
        Bomb nearBomb = null;
        Bomb grabedBomb = null;
        [SerializeField]
        Transform handTf;
        /// <summary>
        /// 硬直時間
        /// </summary>
        float inoperableTime = 0f;
        /// <summary>
        /// 無敵時間
        /// </summary>
        float invincibleTime = 0f;
        // Start is called before the first frame update
        void Awake()
        {
            TryGetComponent(out animator);
            TryGetComponent(out motor);
            TryGetComponent(out charaColorChanger);
            /*
            mesh.SetActive(false);
            motor.enabled = false;
            KoitanInput.actionListWhenPlayerJoin[playerIndex] += ActionWhenPlayerJoin;
            */
        }

        // Update is called once per frame
        void Update()
        {
            //非表示なら動かさない
            //if (!mesh.activeSelf) return;

            animator.SetBool("Fall", motor.IsFalling());
            animator.SetBool("Ground", motor.IsGrounded());
            animator.SetBool("Damage", IsInoperable() && IsInvincible());

            //無敵時間
            if (invincibleTime > 0f)
            {
                invincibleTime -= Time.deltaTime;

            }

            //操作不能
            //攻撃の進行。硬直中も進める必要があるので操作不能の判定より前で回す
            if (playerAttack != null)
            {
                playerAttack.Tick(Time.deltaTime);
            }

            if (inoperableTime > 0f)
            {
                inoperableTime -= Time.deltaTime;
                motor.normalizedXMovement = 0;
                motor.normalizedYMovement = 0;
                return;
            }

            if (KoitanInput.GetStick(playerIndex).x > 0.1f)
            {
                animator.SetBool("Run", true);
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (KoitanInput.GetStick(playerIndex).x < -0.1f)
            {
                animator.SetBool("Run", true);
                transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                animator.SetBool("Run", false);
            }

            motor.normalizedXMovement = KoitanInput.GetStick(playerIndex).x;
            motor.normalizedYMovement = KoitanInput.GetStick(playerIndex).y;

            if (KoitanInput.GetDown(ButtonCode.B, playerIndex) && motor.IsGrounded())
            {
                animator.Play("Jump");
                motor.Jump();
            }

            motor.jumpingHeld = KoitanInput.Get(ButtonCode.B, playerIndex);


            if (KoitanInput.GetDown(ButtonCode.A, playerIndex))
            {
                if (nearShop != null)
                {
                    //ショップ建設
                    nearShop.BuildShop(teamIndex);
                }
                else if (grabedBomb != null)
                {
                    animator.Play("Throw");
                    SetInoperableTime(0.25f);
                }
                else if (nearBomb != null)
                {
                    //Bomb
                    nearBomb.Pick(handTf, playerIndex);
                    grabedBomb = nearBomb;
                    nearBomb = null;
                }
            }

            //攻撃(X=近接、Y=飛び道具)
            //移植元はY=パイプ、B=光線銃だったが、こちらはBがジャンプなのでX/Yに割り当て直している
            if (playerAttack != null)
            {
                if (KoitanInput.GetDown(ButtonCode.X, playerIndex))
                {
                    StartAttack(0);
                }
                else if (KoitanInput.GetDown(ButtonCode.Y, playerIndex))
                {
                    StartAttack(1);
                }
            }
        }

        /// <summary>
        /// 攻撃を開始する。攻撃中は動けないよう、全体時間ぶんの硬直を入れる
        /// (硬直の解除はUpdateの操作不能処理に任せる)。
        /// </summary>
        void StartAttack(int index)
        {
            if (playerAttack.TryAttack(index))
            {
                SetInoperableTime(playerAttack.GetTotalTime(index));
            }
        }

        /// <summary>
        /// 操作不能時間
        /// </summary>
        /// <param name="time"></param>
        public void SetInoperableTime(float time)
        {
            inoperableTime = time;
        }

        /// <summary>
        /// 無敵時間
        /// </summary>
        /// <param name="time"></param>
        public void SetInvincibleTime(float time)
        {
            if (time > invincibleTime)
            {
                invincibleTime = time;
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
            this.playerIndex = playerIndex;
            this.teamIndex = teamIndex;
            charaColorChanger.ChangeColor(playerIndex, teamIndex);
        }

        //IBattlePlayerの実装。HitBoxが「誰の攻撃か」「誰に当たったか」を判別するのに使う
        public int PlayerIndex => playerIndex;
        public int TeamIndex => teamIndex;
        /// <summary>
        /// 向き。このプロジェクトではlocalScale.xの符号で左右を表している(Update内で反転させている)。
        /// </summary>
        public int FacingSign => transform.localScale.x < 0f ? -1 : 1;
        public Transform Transform => transform;
        /// <summary>オフラインなので判定は常に自分で行う。</summary>
        public bool HasAttackAuthority => true;

        public void ApplyDamage(Vector2 knockback, float inoperableTime)
        {
            SetDamage(knockback, inoperableTime);
        }

        public void AddPowerVec(Vector2 vec)
        {
            motor.velocity = vec;
        }

        public void SetDamage(Vector2 vec, float time)
        {
            // 無敵であれば無視
            if (IsInvincible()) return;
            AddPowerVec(vec);
            SetInoperableTime(time);
            //殴られたら攻撃は中断する(判定が出しっぱなしになるのを防ぐ)
            if (playerAttack != null) playerAttack.Cancel();
            //とりあえず二倍の無敵時間
            SetInvincibleTime(time * 2f);
            //とりあえずアニメーション
            animator.Play("Damage");
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            switch (collision.tag)
            {
                case "Land":
                    if (nearShop == null)
                    {
                        nearShop = collision.transform.parent.GetComponent<ShopController>();
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
            if (collision.collider.tag == "Money")
            {
                Money money = collision.collider.GetComponentInParent<Money>();
                if (money.IsGetable(teamIndex))
                {
                    BattleManager.Moneys[playerIndex] += (int)money.GetMoney();
                }
            }
        }
    }
}