using UnityEditor;
using UnityEngine;

namespace Koitan.EditorTools
{
    /// <summary>
    /// 移植したキャラの大きさと原点を、既存キャラ(kawaztan = Player0)に合わせるツール。
    ///
    /// コライダーの寸法で合わせると揃わなかった。コライダーと絵の比率がキャラごとに
    /// 違うため。なので実際に描かれている範囲(SpriteRenderer の bounds)を測って合わせる。
    ///
    /// あわせて原点も直す。kawaztan はパーツが Y 0〜3.8 付近に並ぶ「足元が原点」の作りだが、
    /// 移植キャラは Y -2.2〜+2.5 と「腰が原点」なので、そのままだと地面に沈む。
    /// bone と mesh をまとめて持ち上げて足元を原点に揃える。
    /// </summary>
    public static class CharaSizeMatcher
    {
        static readonly string[] Charas = { "boy_1", "boy_2", "girl_2", "girl_3" };
        const string ReferencePath = "Assets/Prefabs/Charas/Player0.prefab";

        [MenuItem("KoitanLib/キャラ移植/操作可能にする/5. 大きさと原点をkawaztanに合わせる")]
        public static void MatchAll()
        {
            // 基準となる kawaztan の描画範囲を測る
            GameObject reference = PrefabUtility.LoadPrefabContents(ReferencePath);

            if (!TryGetVisualBounds(reference, out Bounds refBounds))
            {
                Debug.LogError("[CharaSizeMatcher] Player0 の描画範囲を測れませんでした。");
                PrefabUtility.UnloadPrefabContents(reference);
                return;
            }

            float targetHeight = refBounds.size.y;
            float targetBottom = refBounds.min.y;   // 足元が原点からどれだけ下か
            PrefabUtility.UnloadPrefabContents(reference);

            Debug.Log($"[CharaSizeMatcher] 基準(kawaztan): 高さ {targetHeight:F3}、" +
                      $"足元 Y {targetBottom:F3}、頭 Y {refBounds.max.y:F3}");

            foreach (string chara in Charas)
            {
                string path = $"Assets/Prefabs/Charas/{chara}_rig.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[CharaSizeMatcher] プレハブがありません: {path}");
                    continue;
                }

                GameObject rig = PrefabUtility.LoadPrefabContents(path);

                // いったん等倍・無シフトに戻してから測る（何度実行しても同じ結果になるように）
                rig.transform.localScale = Vector3.one;

                Transform bone = rig.transform.Find("bone");
                Transform mesh = rig.transform.Find("mesh");

                if (bone != null) bone.localPosition = Vector3.zero;
                if (mesh != null) mesh.localPosition = Vector3.zero;

                if (!TryGetVisualBounds(rig, out Bounds b) || b.size.y <= 0.0001f)
                {
                    Debug.LogWarning($"[CharaSizeMatcher] {chara}: 描画範囲を測れませんでした。");
                    PrefabUtility.UnloadPrefabContents(rig);
                    continue;
                }

                // 等倍での高さから、基準に合わせる倍率を出す
                float scale = targetHeight / b.size.y;
                rig.transform.localScale = new Vector3(scale, scale, 1f);

                // 倍率をかけた後の足元を、基準の足元に合わせるぶんだけ bone と mesh を持ち上げる
                float shift = targetBottom - b.min.y * scale;
                float shiftLocal = shift / scale;   // bone/mesh はルートの子なので倍率で割り戻す

                if (bone != null) bone.localPosition = new Vector3(0f, shiftLocal, 0f);
                if (mesh != null) mesh.localPosition = new Vector3(0f, shiftLocal, 0f);

                // 本体コライダーも同じだけずらす（当たり判定が体からずれないように）
                if (rig.TryGetComponent(out BoxCollider2D col))
                {
                    col.offset = new Vector2(col.offset.x, col.offset.y + shiftLocal);
                }

                PrefabUtility.SaveAsPrefabAsset(rig, path);
                PrefabUtility.UnloadPrefabContents(rig);

                Debug.Log($"[CharaSizeMatcher] {chara}: 等倍高さ {b.size.y:F3} → 倍率 {scale:F3}、" +
                          $"足元合わせに {shiftLocal:F3} 持ち上げ");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[CharaSizeMatcher] 完了。");
        }

        /// <summary>
        /// 配下の SpriteRenderer をすべて含む描画範囲を返す。
        /// 攻撃判定など見えないものは除きたいので、無効なものと武器は数えない。
        /// </summary>
        static bool TryGetVisualBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            bool first = true;

            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(false))
            {
                if (sr.sprite == null) continue;

                // 武器は構えで大きく張り出すので体の大きさの基準から外す
                if (sr.name == "pipe_isu" || sr.name == "kousenju") continue;

                if (first) { bounds = sr.bounds; first = false; }
                else bounds.Encapsulate(sr.bounds);
            }

            return !first;
        }
    }
}
