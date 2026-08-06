using System.Collections.Generic;
using UnityEngine;

namespace Koitan
{
    /// <summary>
    /// 何に当たるか。移植元(FesGame18)の HitBitsInfo に相当するが、
    /// あちらは独立した MonoBehaviour だったのをこちらでは HitBox のフィールドに畳んである
    /// (コンポーネントを 1 つ減らし、付け忘れによる不具合を避けるため)。
    /// </summary>
    [System.Serializable]
    public struct HitTargets
    {
        [Tooltip("自分自身(デバッグ用)")]
        public bool mySelf;
        [Tooltip("味方(teamIndex が同じプレイヤー)")]
        public bool myFriend;
        [Tooltip("敵(teamIndex が違うプレイヤー)")]
        public bool enemy;
        [Tooltip("建設済みのショップ")]
        public bool shop;
    }

    /// <summary>
    /// 攻撃の当たり判定のまとめ役。武器などプレイヤーの子オブジェクトに付ける。
    /// 実際のコライダーは子の <see cref="SubHitBox"/> が持ち、当たった通知をここに転送してくる。
    ///
    /// 移植元との違い:
    /// - 移植元は OnCollisionEnter2D だったが、こちらは PC2D の PlatformerMotor2D が
    ///   Rigidbody2D を Kinematic で使うため Kinematic 同士では衝突コールバックが発生しない。
    ///   よってトリガー方式に変更している(OnlineMoney / PlayerAvatar でも同じ回避をしている)。
    /// - 相手の判別を "Player" タグ + Player クラスではなく <see cref="IBattlePlayer"/> で行い、
    ///   オフライン・オンラインどちらのプレイヤーにも同じコードで当たるようにしている。
    /// </summary>
    public class HitBox : MonoBehaviour
    {
        [SerializeField]
        HitTargets targets = new HitTargets { enemy = true, shop = true };

        /// <summary>
        /// 一度当たった相手には、リストがリセットされるまで再度当たらない。
        /// リセットはアニメーションイベント(<see cref="AttackEventReceiver.ResetHitList"/>)か、
        /// この HitBox の有効化時に行われる。
        /// </summary>
        readonly List<GameObject> hitObjects = new List<GameObject>();

        IAttackOwner owner;
        bool ownerWarned;

        public IAttackOwner Owner => owner;

        void Awake()
        {
            // 飛び道具の HitBox は親から離れて飛んでいくので、その場合は
            // 撃った側から SetOwner() で後付けされる(ここで見つからなくても異常ではない)。
            owner = GetComponentInParent<IAttackOwner>();
        }

        /// <summary>
        /// 持ち主を明示的に設定する。撃ち出された飛び道具のようにヒエラルキーから
        /// 持ち主をたどれない場合に使う。
        /// </summary>
        public void SetOwner(IAttackOwner value)
        {
            owner = value;
        }

        void OnEnable()
        {
            // 攻撃モーションのたびに当たり判定が有効化される想定なので、ここでもリセットしておく。
            // アニメーションイベントの貼り忘れで「二度と当たらない」状態になるのを防ぐ保険。
            hitObjects.Clear();
        }

        /// <summary>子の <see cref="SubHitBox"/> から呼ばれる。</summary>
        public void OnHit(Collider2D other, SubHitBox subHitBox)
        {
            if (owner == null)
            {
                // 判定が無言で出ないのが一番デバッグしづらいので一度だけ知らせる。
                if (!ownerWarned)
                {
                    ownerWarned = true;
                    Debug.LogError($"[HitBox] 持ち主が設定されていないため当たり判定が働きません: {name}", this);
                }

                return;
            }

            // オンラインでは攻撃側の StateAuthority を持つクライアントだけが判定する。
            // (ダメージ適用自体は相手の StateAuthority 宛の RPC に委譲される)
            if (!owner.HasAttackAuthority) return;

            IBattlePlayer target = other.GetComponentInParent<IBattlePlayer>();

            if (target != null)
            {
                if (!IsValidTarget(target)) return;

                GameObject targetObj = target.Transform.gameObject;
                if (hitObjects.Contains(targetObj)) return;
                hitObjects.Add(targetObj);

                // ノックバックの左右は攻撃側の向きに合わせる(移植元の anim.muki と同じ扱い)。
                Vector2 knockback = new Vector2(
                    subHitBox.Knockback.x * owner.FacingSign,
                    subHitBox.Knockback.y);

                target.ApplyDamage(knockback, subHitBox.InoperableTime);
                subHitBox.PlayHitFeedback(other.ClosestPoint(subHitBox.transform.position));
                return;
            }

            if (!targets.shop) return;

            // ショップはオフラインとオンラインで別クラスなので両方見る。
            // (2 種類しかないためインターフェースは切らず、Bomb.cs と同じく直接参照している)
            ShopController shop = other.GetComponentInParent<ShopController>();

            if (shop != null && shop.isBuild)
            {
                if (hitObjects.Contains(shop.gameObject)) return;
                hitObjects.Add(shop.gameObject);

                shop.BrokenShop();
                subHitBox.PlayHitFeedback(other.ClosestPoint(subHitBox.transform.position));
                return;
            }

            OnlineShopController onlineShop = other.GetComponentInParent<OnlineShopController>();

            if (onlineShop != null && onlineShop.IsBuild && !onlineShop.IsBroken)
            {
                if (hitObjects.Contains(onlineShop.gameObject)) return;
                hitObjects.Add(onlineShop.gameObject);

                onlineShop.TryBrokenShop();
                subHitBox.PlayHitFeedback(other.ClosestPoint(subHitBox.transform.position));
            }
        }

        bool IsValidTarget(IBattlePlayer target)
        {
            if (target.PlayerIndex == owner.PlayerIndex) return targets.mySelf;

            // teamIndex が -1(チーム未割り当て)同士を味方扱いすると個人戦で誰も殴れなくなるので、
            // -1 の場合は常に敵として扱う。
            bool sameTeam = target.TeamIndex >= 0
                && owner.TeamIndex >= 0
                && target.TeamIndex == owner.TeamIndex;

            return sameTeam ? targets.myFriend : targets.enemy;
        }

        /// <summary>アニメーションイベントなどから呼ぶ。当たった相手のリストを空にする。</summary>
        public void ResetHitList()
        {
            hitObjects.Clear();
        }
    }
}
