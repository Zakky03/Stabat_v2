using UnityEngine;

namespace Koitan
{
    /// <summary>
    /// 当たり判定コライダー 1 つ分のデータ。<see cref="HitBox"/> の子オブジェクトに付ける。
    /// コライダーは必ず isTrigger にすること(理由は HitBox のコメント参照)。
    ///
    /// 1 つの武器に複数の判定(根元と先端で威力を変えるなど)を持たせられるよう、
    /// 移植元(FesGame18)と同じく HitBox とは別コンポーネントに分けてある。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class SubHitBox : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("ふっとばし。X は攻撃側の向きに応じて自動で反転する")]
        Vector2 knockback = new Vector2(25f, 15f);

        [SerializeField]
        [Tooltip("相手を操作不能にする秒数。既存の爆弾は 1 秒")]
        float inoperableTime = 0.5f;

        [SerializeField]
        GameObject hitEffect;

        [SerializeField]
        AudioClip hitSound;

        public Vector2 Knockback => knockback;
        public float InoperableTime => inoperableTime;

        HitBox hitBox;

        void Awake()
        {
            hitBox = GetComponentInParent<HitBox>();

            if (hitBox == null)
            {
                Debug.LogError($"[SubHitBox] 親に HitBox が見つかりません: {name}", this);
                return;
            }

            Collider2D col = GetComponent<Collider2D>();

            if (!col.isTrigger)
            {
                // 素の衝突だと Kinematic 同士でコールバックが飛ばず、無言で当たらなくなるので警告する。
                Debug.LogWarning($"[SubHitBox] コライダーが isTrigger になっていません: {name}", this);
            }
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (hitBox != null) hitBox.OnHit(other, this);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            // 攻撃判定が出た瞬間に既に相手と重なっていた場合は Enter が飛ばないため Stay も拾う。
            // 二重ヒットは HitBox 側の当たったリストで防いでいる。
            if (hitBox != null) hitBox.OnHit(other, this);
        }

        /// <summary>ヒット時の見た目と音。演出のみなので当てた側のクライアントでローカルに出す。</summary>
        public void PlayHitFeedback(Vector2 point)
        {
            if (hitEffect != null)
            {
                GameObject obj = Instantiate(hitEffect, point, Quaternion.identity);

                if (hitSound != null)
                {
                    AudioSource audioSource = obj.AddComponent<AudioSource>();
                    audioSource.PlayOneShot(hitSound);
                }
            }
            else if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, point);
            }
        }
    }
}
