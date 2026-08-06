using UnityEngine;

namespace Koitan
{
    /// <summary>
    /// 飛び道具の発射口。プレイヤーの子オブジェクト(銃口の位置)に付ける。
    /// 移植元(FesGame18)の InstantiateMissile + Weapon.ShotMissile をまとめたもの。
    ///
    /// 現状はオフライン専用。オンライン(Fusion)では弾を Runner.Spawn する必要があり、
    /// 弾プレハブへの NetworkObject 付与と NetworkProjectConfig への登録という
    /// エディタ作業が要るため、そちらは未対応(<see cref="Shot"/> のコメント参照)。
    /// 近接攻撃はスポーンを伴わないためオンラインでもそのまま動く。
    /// </summary>
    public class MissileLauncher : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("撃ち出す弾のプレハブ。Missile コンポーネントが必要")]
        Missile missilePrefab;

        [SerializeField]
        float speed = 20f;

        [SerializeField]
        [Tooltip("撃ち出す角度(度)。0 で真横、正の値で上向き。左向き時は自動で反転する")]
        float angle = 0f;

        [SerializeField]
        AudioClip shotSound;

        IAttackOwner owner;

        void Awake()
        {
            owner = GetComponentInParent<IAttackOwner>();

            if (owner == null)
            {
                Debug.LogError($"[MissileLauncher] 親に IAttackOwner が見つかりません: {name}", this);
            }
        }

        /// <summary>
        /// 弾を 1 発撃つ。<see cref="PlayerAttack"/> が発生フレームで呼ぶ。
        /// </summary>
        public void Shot()
        {
            if (owner == null || missilePrefab == null) return;

            // オンラインの場合、ここで普通に Instantiate すると撃った本人にしか弾が見えない。
            // 相手にも見せるには Runner.Spawn が必要で、それには弾プレハブの
            // NetworkObject 化(エディタ作業)が要るため、今は未対応であることを明示しておく。
            if (owner is PlayerAvatar)
            {
                Debug.LogWarning(
                    "[MissileLauncher] オンラインの飛び道具は未対応です。" +
                    "弾プレハブを NetworkObject 化して Runner.Spawn する対応が必要です。", this);
                return;
            }

            if (!owner.HasAttackAuthority) return;

            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(
                Mathf.Cos(radians) * owner.FacingSign,
                Mathf.Sin(radians));

            Missile missile = Instantiate(missilePrefab, transform.position, Quaternion.identity);

            // 弾の見た目を進行方向に向ける
            missile.transform.right = direction;

            missile.Initialize(owner, direction * speed);

            if (shotSound != null)
            {
                AudioSource.PlayClipAtPoint(shotSound, transform.position);
            }
        }
    }
}
