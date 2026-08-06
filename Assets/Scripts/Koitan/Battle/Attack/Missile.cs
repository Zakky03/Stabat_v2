using UnityEngine;

namespace Koitan
{
    /// <summary>
    /// 撃ち出された飛び道具(移植元 FesGame18 の光線銃の弾に相当)。
    ///
    /// 撃った時点でプレイヤーの子から外れるため、当たり判定の持ち主をヒエラルキーからたどれない。
    /// そこで自分が <see cref="IAttackOwner"/> になり、撃った人の情報を
    /// <see cref="MissileLauncher"/> から引き継いで子の <see cref="HitBox"/> に渡す。
    ///
    /// <see cref="IBattlePlayer"/> は実装しないこと。実装すると他人の攻撃がこの弾を
    /// プレイヤーと誤認してヒット扱いしてしまう。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Missile : MonoBehaviour, IAttackOwner
    {
        [SerializeField]
        [Tooltip("この秒数で自動的に消える")]
        float lifeTime = 3f;

        [SerializeField]
        [Tooltip("地形に当たったら消えるか")]
        bool destroyOnStage = true;

        [SerializeField]
        GameObject vanishEffect;

        public int PlayerIndex { get; private set; }
        public int TeamIndex { get; private set; } = -1;
        public int FacingSign { get; private set; } = 1;

        // 弾は撃った本人のクライアントにしか存在しない前提なので、常に判定してよい。
        public bool HasAttackAuthority => true;

        Rigidbody2D rb;
        float elapsed;

        void Awake()
        {
            TryGetComponent(out rb);
        }

        /// <summary>
        /// 撃った直後に <see cref="MissileLauncher"/> から呼ばれる。
        /// </summary>
        public void Initialize(IAttackOwner shooter, Vector2 velocity)
        {
            PlayerIndex = shooter.PlayerIndex;
            TeamIndex = shooter.TeamIndex;
            FacingSign = shooter.FacingSign;

            if (rb != null) rb.linearVelocity = velocity;

            // 子に付いている当たり判定に「誰の攻撃か」を伝える。
            foreach (HitBox hitBox in GetComponentsInChildren<HitBox>(true))
            {
                hitBox.SetOwner(this);
            }
        }

        void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed >= lifeTime) Vanish();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!destroyOnStage) return;

            // プレイヤーや他の弾はすり抜けさせ、地形に当たったときだけ消す。
            // 既存の地形判定に合わせてレイヤー名で見ている。
            int layer = other.gameObject.layer;

            if (layer == LayerMask.NameToLayer("Static Environment")
                || layer == LayerMask.NameToLayer("Moving Platforms"))
            {
                Vanish();
            }
        }

        void Vanish()
        {
            if (vanishEffect != null)
            {
                Instantiate(vanishEffect, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
