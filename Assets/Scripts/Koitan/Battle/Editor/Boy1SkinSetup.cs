using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace Koitan.EditorTools
{
    /// <summary>
    /// boy_1 の全身スプライト（chara_boy_1_0）に、移植元と同じボーンを書き込むツール。
    ///
    /// 移植元(FesGame18)の体は、個別スライスの寄せ集めではなく
    /// 「全身 1 枚絵(270x697)をメッシュ領域に切り分けてスキニングしたもの」だった。
    /// これは Unity 6 の 2D Animation と同じ構造なので、そのまま移行できる。
    ///
    /// ボーンの配置は手作業だと 37 個ぶん地獄なので、ここでスクリプトから流し込む。
    /// ウェイトは Skinning Editor の Auto Weights に任せる（手塗り不要）。
    /// </summary>
    public static class Boy1SkinSetup
    {
        const string TexturePath = "Assets/Sprites/Charas/boy_1/chara_boy_1.png";
        const string TargetSpriteName = "chara_boy_1_0";
        const string RigRootName = "boy_1_rig";

        /// <summary>
        /// 全身スプライトのピボットが、リグのルート座標系のどこに置かれていたか。
        /// 移植元プレハブの template（全身絵を表示していた下絵オブジェクト）の
        /// localPosition がこれ。ボーン座標をスプライト座標系に移すのに使う。
        /// </summary>
        static readonly Vector2 SpritePivotInRigSpace = new Vector2(0.04399991f, -0.21499997f);

        [MenuItem("KoitanLib/boy_1 スプライトにボーンを書き込む")]
        public static void WriteBones()
        {
            GameObject rig = GameObject.Find(RigRootName);

            if (rig == null)
            {
                Debug.LogError($"[Boy1SkinSetup] シーンに {RigRootName} がありません。" +
                               "先に「boy_1 リグを生成」を実行してください（ボーンの座標をそこから読みます）。");
                return;
            }

            Transform boneRoot = rig.transform.Find("bone");

            if (boneRoot == null)
            {
                Debug.LogError("[Boy1SkinSetup] boy_1_rig の下に bone が見つかりません。");
                return;
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();

            var importer = AssetImporter.GetAtPath(TexturePath);

            if (importer == null)
            {
                Debug.LogError($"[Boy1SkinSetup] テクスチャが見つかりません: {TexturePath}");
                return;
            }

            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);

            if (provider == null)
            {
                Debug.LogError("[Boy1SkinSetup] SpriteEditorDataProvider を取得できませんでした。");
                return;
            }

            provider.InitSpriteEditorDataProvider();

            SpriteRect target = null;

            foreach (SpriteRect r in provider.GetSpriteRects())
            {
                if (r.name == TargetSpriteName) { target = r; break; }
            }

            if (target == null)
            {
                Debug.LogError($"[Boy1SkinSetup] スプライト {TargetSpriteName} が見つかりません。" +
                               "テクスチャの Sprite Mode が Multiple になっているか確認してください。");
                return;
            }

            // リグのボーン Transform を、親→子の順に並べる（SpriteBone は親が先に来る必要がある）
            List<Transform> ordered = new List<Transform>();
            CollectBones(boneRoot, ordered);

            // SpriteBone の座標は「スプライトのピクセル座標系」で持つ必要がある。
            // .meta の vertices が実際ピクセル値（0〜697 など）で入っていることを確認済み。
            // リグ側はワールド単位なので、PixelsPerUnit を掛けて変換する。
            // これを忘れると全ボーンが原点付近の極小の点に潰れ、Skinning Editor で見えなくなる。
            float ppu = (importer as UnityEditor.TextureImporter)?.spritePixelsPerUnit ?? 100f;

            // ピクセル座標の原点はスプライト矩形の左下。ルートボーンはピボット分ずらす。
            Vector2 pivotPx = new Vector2(
                target.rect.width * target.pivot.x,
                target.rect.height * target.pivot.y);

            List<SpriteBone> bones = new List<SpriteBone>(ordered.Count);

            for (int i = 0; i < ordered.Count; i++)
            {
                Transform t = ordered[i];

                // SpriteBone の position / rotation は親ボーンからの相対。
                // ルートボーンだけはスプライト座標系（原点＝スプライトのピボット）基準になる。
                bool isRoot = t.parent == boneRoot;

                Vector3 pos;
                Quaternion rot;

                if (isRoot)
                {
                    // リグのルート座標系での位置 → スプライトのピボット基準 → ピクセルへ
                    Vector3 inRig = rig.transform.InverseTransformPoint(t.position);
                    Vector2 fromPivot = new Vector2(
                        inRig.x - SpritePivotInRigSpace.x,
                        inRig.y - SpritePivotInRigSpace.y);

                    pos = new Vector3(
                        fromPivot.x * ppu + pivotPx.x,
                        fromPivot.y * ppu + pivotPx.y,
                        0f);

                    rot = t.rotation * Quaternion.Inverse(rig.transform.rotation);
                }
                else
                {
                    // 子は親からの相対なのでピボットのオフセットは不要。倍率だけ掛ける
                    pos = t.localPosition * ppu;
                    rot = t.localRotation;
                }

                bones.Add(new SpriteBone
                {
                    name = t.name,
                    position = pos,
                    rotation = rot,
                    // 長さも同じくピクセル単位
                    length = (t.childCount > 0 ? t.GetChild(0).localPosition.magnitude : 0.25f) * ppu,
                    parentId = isRoot ? -1 : ordered.IndexOf(t.parent),
                });
            }

            var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();

            if (boneProvider == null)
            {
                Debug.LogError("[Boy1SkinSetup] ISpriteBoneDataProvider を取得できませんでした。" +
                               "2D Animation パッケージが入っているか確認してください。");
                return;
            }

            boneProvider.SetBones(target.spriteID, bones);
            provider.Apply();

            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[Boy1SkinSetup] {TargetSpriteName} に {bones.Count} 本のボーンを書き込みました。" +
                      "次に Sprite Editor > Skinning Editor を開き、Auto Geometry → Auto Weights を実行してください。");
        }

        /// <summary>
        /// スキニング済みの全身スプライトを SpriteSkin として組み込む。
        /// カットアウト方式で作った体パーツ（ボーンの子の SpriteRenderer）は不要になるので消す。
        ///
        /// ボーンの GameObject は Boy1RigBuilder が作ったものをそのまま使う。
        /// クリップのカーブが "bone/hips/..." というパスで解決されるため、
        /// SpriteSkin 用に別のボーンを生成し直してはいけない。
        /// </summary>
        [MenuItem("KoitanLib/boy_1 をSpriteSkin化")]
        public static void SetupSpriteSkin()
        {
            GameObject rig = GameObject.Find(RigRootName);

            if (rig == null)
            {
                Debug.LogError($"[Boy1SkinSetup] シーンに {RigRootName} がありません。");
                return;
            }

            Transform boneRoot = rig.transform.Find("bone");

            if (boneRoot == null)
            {
                Debug.LogError("[Boy1SkinSetup] bone が見つかりません。");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(rig, "Setup SpriteSkin");

            // 1) カットアウトの体パーツを削除（ボーン配下の SpriteRenderer 付きオブジェクト）
            List<GameObject> toDelete = new List<GameObject>();

            foreach (SpriteRenderer sr in boneRoot.GetComponentsInChildren<SpriteRenderer>(true))
            {
                toDelete.Add(sr.gameObject);
            }

            foreach (GameObject go in toDelete) Object.DestroyImmediate(go);

            // 2) 全身スプライトを取得
            Sprite body = null;

            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(TexturePath))
            {
                if (o is Sprite s && s.name == TargetSpriteName) { body = s; break; }
            }

            if (body == null)
            {
                Debug.LogError($"[Boy1SkinSetup] スプライト {TargetSpriteName} を読めませんでした。");
                return;
            }

            // 3) 見た目用のオブジェクト（kawaztan で言う mesh に相当）
            Transform meshTf = rig.transform.Find("mesh");

            if (meshTf == null)
            {
                meshTf = new GameObject("mesh").transform;
                meshTf.SetParent(rig.transform, false);
            }

            meshTf.localPosition = Vector3.zero;
            meshTf.localRotation = Quaternion.identity;
            meshTf.localScale = Vector3.one;

            if (!meshTf.TryGetComponent(out SpriteRenderer renderer))
                renderer = meshTf.gameObject.AddComponent<SpriteRenderer>();

            renderer.sprite = body;

            if (!meshTf.TryGetComponent(out SpriteSkin skin))
                skin = meshTf.gameObject.AddComponent<SpriteSkin>();

            // 4) ボーンを割り当てる。順番はスプライトに書き込んだときと同じでなければならない
            List<Transform> ordered = new List<Transform>();
            CollectBones(boneRoot, ordered);

            // rootBone / boneTransforms は読み取り専用プロパティなので、
            // シリアライズされたフィールドに直接書く。
            SerializedObject so = new SerializedObject(skin);
            so.FindProperty("m_RootBone").objectReferenceValue = ordered.Count > 0 ? ordered[0] : null;

            SerializedProperty boneProp = so.FindProperty("m_BoneTransforms");
            boneProp.arraySize = ordered.Count;

            for (int i = 0; i < ordered.Count; i++)
            {
                boneProp.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];
            }

            so.ApplyModifiedProperties();

            Selection.activeGameObject = rig;

            Debug.Log($"[Boy1SkinSetup] SpriteSkin を設定しました。" +
                      $"削除したカットアウトのパーツ {toDelete.Count} 個、割り当てたボーン {ordered.Count} 本、" +
                      $"rootBone={(ordered.Count > 0 ? ordered[0].name : "なし")}");
        }

        /// <summary>bone の下のボーンを、親が先に来る順（深さ優先）で集める。</summary>
        static void CollectBones(Transform boneRoot, List<Transform> result)
        {
            foreach (Transform child in boneRoot)
            {
                Collect(child, result);
            }
        }

        static void Collect(Transform t, List<Transform> result)
        {
            // 武器やアンカーはボーンではないので除外する
            if (t.GetComponent<SpriteRenderer>() != null) return;

            result.Add(t);

            foreach (Transform child in t)
            {
                Collect(child, result);
            }
        }
    }
}
