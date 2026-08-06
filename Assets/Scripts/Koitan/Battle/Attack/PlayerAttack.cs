using UnityEngine;

namespace Koitan
{
    /// <summary>
    /// 攻撃 1 種類ぶんの設定。近接(hitBox)と飛び道具(launcher)のどちらか、または両方を持てる。
    /// </summary>
    [System.Serializable]
    public class AttackDefinition
    {
        [Tooltip("インスペクタで見分けるためだけの名前")]
        public string name = "Attack";

        [Tooltip("近接攻撃の当たり判定。持続中だけ有効になる。飛び道具のみの攻撃なら空でよい")]
        public HitBox hitBox;

        [Tooltip("飛び道具。発生時に一度だけ撃つ。近接のみの攻撃なら空でよい")]
        public MissileLauncher launcher;

        [Tooltip("再生する Animator のステート名。存在しない場合は自動で無視される")]
        public string animationStateName = "";

        [Tooltip("入力から判定が出るまでの秒数")]
        public float startupTime = 0.1f;

        [Tooltip("判定が出ている秒数")]
        public float activeTime = 0.15f;

        [Tooltip("判定が消えてから動けるようになるまでの秒数")]
        public float recoveryTime = 0.25f;

        public float TotalTime => startupTime + activeTime + recoveryTime;
    }

    /// <summary>
    /// プレイヤーの攻撃の進行管理。オフライン(<see cref="PlayerController"/>)と
    /// オンライン(<see cref="PlayerAvatar"/>)の両方から使う。
    ///
    /// 移植元(FesGame18)は判定の出し入れを全てアニメーションイベントで行っていたが、
    /// 移植先の Animator Controller には攻撃ステートがまだ無く、そのまま持ってくると
    /// 判定が一生出ないため、発生／持続／硬直をこちらのタイマーで駆動する方式にしてある。
    /// アニメーションが用意できたら <see cref="AttackDefinition.animationStateName"/> を
    /// 設定するだけで再生されるようになる。
    ///
    /// 自分では Update しない。オフラインは Time.deltaTime、オンラインは Runner.DeltaTime と
    /// 時間の進め方が違うため、持ち主から <see cref="Tick"/> を呼んでもらう。
    /// </summary>
    public class PlayerAttack : MonoBehaviour
    {
        [SerializeField]
        AttackDefinition[] attacks = new AttackDefinition[0];

        [SerializeField]
        Animator animator;

        int currentIndex = -1;
        float elapsed;
        bool hitBoxActive;

        /// <summary>攻撃中(硬直も含む)か。</summary>
        public bool IsAttacking => currentIndex >= 0;

        public int AttackCount => attacks.Length;

        void Awake()
        {
            if (animator == null) TryGetComponent(out animator);

            // 判定は攻撃の持続中だけ出す。編集中に付けっぱなしでも事故らないよう最初に全部落とす。
            foreach (AttackDefinition attack in attacks)
            {
                if (attack.hitBox != null) attack.hitBox.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 攻撃を開始する。攻撃中は受け付けない。
        /// </summary>
        /// <returns>実際に攻撃を開始したら true</returns>
        public bool TryAttack(int index)
        {
            if (IsAttacking) return false;
            if (index < 0 || index >= attacks.Length) return false;

            currentIndex = index;
            elapsed = 0f;
            hitBoxActive = false;

            AttackDefinition attack = attacks[index];

            if (animator != null && !string.IsNullOrEmpty(attack.animationStateName))
            {
                int hash = Animator.StringToHash(attack.animationStateName);

                // ステートが無いまま Play すると無言で何も起きず原因が分かりにくいので、
                // 存在確認してから再生する(攻撃アニメが未実装でも攻撃自体は成立させる)。
                if (animator.HasState(0, hash))
                {
                    animator.Play(hash, 0, 0f);
                }
            }

            return true;
        }

        /// <summary>
        /// 攻撃の進行を進める。持ち主から毎フレーム呼ぶこと。
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsAttacking) return;

            AttackDefinition attack = attacks[currentIndex];
            float previous = elapsed;
            elapsed += deltaTime;

            // 発生の瞬間をまたいだら飛び道具を撃つ
            if (attack.launcher != null
                && previous < attack.startupTime
                && elapsed >= attack.startupTime)
            {
                attack.launcher.Shot();
            }

            bool shouldBeActive = elapsed >= attack.startupTime
                && elapsed < attack.startupTime + attack.activeTime;

            if (shouldBeActive != hitBoxActive)
            {
                hitBoxActive = shouldBeActive;

                if (attack.hitBox != null)
                {
                    // 有効化のたびに HitBox 側で当たったリストがリセットされるので、
                    // 同じ相手に次の攻撃で当て直せる。
                    attack.hitBox.gameObject.SetActive(shouldBeActive);
                }
            }

            if (elapsed >= attack.TotalTime)
            {
                Cancel();
            }
        }

        /// <summary>
        /// 攻撃を強制終了する。ダメージを受けたときなどに呼ぶ。
        /// </summary>
        public void Cancel()
        {
            if (currentIndex >= 0)
            {
                AttackDefinition attack = attacks[currentIndex];

                if (attack.hitBox != null) attack.hitBox.gameObject.SetActive(false);
            }

            currentIndex = -1;
            elapsed = 0f;
            hitBoxActive = false;
        }

        /// <summary>指定した攻撃の全体時間。硬直時間の設定に使う。</summary>
        public float GetTotalTime(int index)
        {
            if (index < 0 || index >= attacks.Length) return 0f;

            return attacks[index].TotalTime;
        }
    }
}
