using UnityEngine;

namespace Koitan
{
    /// <summary>
    /// 攻撃を出した側。プレイヤー本人だけでなく、撃ち出されて親から離れた飛び道具
    /// (<see cref="Missile"/>)も「誰の攻撃か」を伝えるためにこれを実装する。
    /// </summary>
    public interface IAttackOwner
    {
        int PlayerIndex { get; }
        int TeamIndex { get; }

        /// <summary>
        /// 向き。右向きなら +1、左向きなら -1。移植元(FesGame18)の AnimaController.muki に相当。
        /// ノックバックの X 成分を攻撃側の向きに合わせて反転させるのに使う。
        /// </summary>
        int FacingSign { get; }

        /// <summary>
        /// この個体のヒット判定を実際に処理してよいか。
        /// オフラインは常に true。オンラインでは StateAuthority を持つクライアントのみ true になり、
        /// 同じ攻撃が全クライアントで多重にヒット処理されるのを防ぐ。
        /// </summary>
        bool HasAttackAuthority { get; }
    }

    /// <summary>
    /// 攻撃を受けられるプレイヤー。
    /// オフラインの <see cref="PlayerController"/> とオンラインの <see cref="PlayerAvatar"/> が実装する。
    ///
    /// 移植元の HitBox は "Player" タグ + Player クラス直参照で相手を判別していたが、
    /// こちらはオフライン／オンラインでプレイヤーのクラスが別なので、
    /// タグではなくこのインターフェースで解決する。
    ///
    /// 飛び道具は <see cref="IAttackOwner"/> だけを実装しこちらは実装しない。
    /// そうしないと「自分の弾を自分の攻撃が殴る」形で誤ヒットしてしまうため。
    /// </summary>
    public interface IBattlePlayer : IAttackOwner
    {
        Transform Transform { get; }

        /// <summary>
        /// ノックバックと硬直を適用する。
        /// オンライン実装では内部で StateAuthority 宛の RPC に委譲するので、
        /// 攻撃側クライアントから直接呼んでよい。
        /// </summary>
        void ApplyDamage(Vector2 knockback, float inoperableTime);
    }
}
