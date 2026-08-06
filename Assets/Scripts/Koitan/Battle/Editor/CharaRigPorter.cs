using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace Koitan.EditorTools
{
    /// <summary>
    /// FesGame18 のキャラ（boy_1 / girl_2 など）を、このプロジェクトに移すためのツール。
    ///
    /// boy_1 でやった手順を一般化したもの。キャラごとのボーン階層は
    /// Assets/Sprites/Charas/&lt;chara&gt;/rig.json に書き出してあり、それを読んで組む。
    /// （rig.json は移植元プレハブの YAML から機械的に抽出したもので、
    ///   ボーン名・階層・座標は移植元と完全に一致している）
    ///
    /// 対応しているのは「全身 1 枚絵をメッシュ分割してスキニングした」タイプのキャラのみ。
    /// boy_1 と girl_2 がこれに当たる。boy_2 と girl_3 はパーツごとに別スプライトを
    /// 持つ別構造なので、このツールでは扱えない。
    ///
    /// 使い方（キャラごとに）:
    ///   1. KoitanLib/キャラ移植/&lt;chara&gt;/1. リグを生成
    ///   2. KoitanLib/キャラ移植/&lt;chara&gt;/2. スプライトにボーンを書き込む
    ///   3. Sprite Editor &gt; Skinning Editor で Auto Geometry → Auto Weights → Apply（手作業）
    ///   4. KoitanLib/キャラ移植/&lt;chara&gt;/3. SpriteSkin化
    /// </summary>
    public static class CharaRigPorter
    {
        [System.Serializable]
        public class BoneRow
        {
            public string path;
            public float px, py, qz, qw, scale;
        }

        [System.Serializable]
        public class RigData
        {
            public float rootScale = 1f;
            public float templateX, templateY;
            public BoneRow[] bones;
        }

        // ---- boy_1 ----
        [MenuItem("KoitanLib/キャラ移植/boy_1/1. リグを生成")]
        static void Boy1Rig() => BuildBones("boy_1");
        [MenuItem("KoitanLib/キャラ移植/boy_1/2. スプライトにボーンを書き込む")]
        static void Boy1Bones() => WriteSpriteBones("boy_1", "chara_boy_1.png", "chara_boy_1_0");
        [MenuItem("KoitanLib/キャラ移植/boy_1/3. SpriteSkin化")]
        static void Boy1Skin() => SetupSkin("boy_1", "chara_boy_1.png", "chara_boy_1_0");

        // ---- girl_2（アトラス名が chara_girl_1 なので注意）----
        //
        // girl_2 は boy_1 と違い、全身の絵がスライスされていなかった。
        // アトラス左側の x=26,y=13 から 259x689 の領域が全身の絵で、そこには
        // スプライトが一つも定義されていない（アルファ値を走査して特定した）。
        // なので先にその領域をスプライトとして切ってから、そこにボーンを書き込む。
        //
        // 元の chara_girl_1_0 は位置的に脚のパーツで、全身ではない。
        // Anima2D はメッシュ側に UV を焼き込むため、SpriteMesh の m_Sprite 参照は
        // 実際に描かれる絵と一致しておらず、そこに引っかかった。
        const string Girl2Body = "chara_girl_1_body";
        static readonly Rect Girl2BodyRect = new Rect(26, 13, 259, 689);

        [MenuItem("KoitanLib/キャラ移植/girl_2/1. リグを生成")]
        static void Girl2Rig() => BuildBones("girl_2");
        [MenuItem("KoitanLib/キャラ移植/girl_2/2. スプライトにボーンを書き込む")]
        static void Girl2Bones()
        {
            if (EnsureSprite("girl_2", "chara_girl_1.png", Girl2Body, Girl2BodyRect))
                WriteSpriteBones("girl_2", "chara_girl_1.png", Girl2Body);
        }
        [MenuItem("KoitanLib/キャラ移植/girl_2/3. SpriteSkin化")]
        static void Girl2Skin() => SetupSkin("girl_2", "chara_girl_1.png", Girl2Body);

        /// <summary>
        /// 指定した名前のスプライトが無ければ、その矩形で新しく切る。
        /// ピボットは中央（boy_1 の全身スプライトと同じ）。
        /// </summary>
        static bool EnsureSprite(string chara, string pngName, string spriteName, Rect rect)
        {
            string texPath = Dir(chara) + "/" + pngName;
            AssetImporter importer = AssetImporter.GetAtPath(texPath);

            if (importer == null) { Debug.LogError($"[CharaRigPorter] テクスチャがありません: {texPath}"); return false; }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            List<SpriteRect> rects = new List<SpriteRect>(provider.GetSpriteRects());

            foreach (SpriteRect r in rects)
            {
                if (r.name == spriteName)
                {
                    Debug.Log($"[CharaRigPorter] {spriteName} は既にあります。");
                    return true;
                }
            }

            SpriteRect add = new SpriteRect
            {
                name = spriteName,
                rect = rect,
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                border = Vector4.zero,
                spriteID = GUID.Generate(),
            };

            rects.Add(add);
            provider.SetSpriteRects(rects.ToArray());
            provider.Apply();
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[CharaRigPorter] スプライト {spriteName} を追加しました " +
                      $"(x={rect.x}, y={rect.y}, {rect.width}x{rect.height})。");
            return true;
        }

        // ---- boy_2 / girl_3 ----
        // どちらも boy_1 と同じで、インデックス 0 のスプライトが全身の絵になっている
        // （boy_2: 256x701、girl_3: 265x717）。girl_2 だけが未スライスの例外だった。
        [MenuItem("KoitanLib/キャラ移植/boy_2/1. リグを生成")]
        static void Boy2Rig() => BuildBones("boy_2");
        [MenuItem("KoitanLib/キャラ移植/boy_2/2. スプライトにボーンを書き込む")]
        static void Boy2Bones() => WriteSpriteBones("boy_2", "chara_boy_2.png", "chara_boy_2_0");
        [MenuItem("KoitanLib/キャラ移植/boy_2/3. SpriteSkin化")]
        static void Boy2Skin() => SetupSkin("boy_2", "chara_boy_2.png", "chara_boy_2_0");

        [MenuItem("KoitanLib/キャラ移植/girl_3/1. リグを生成")]
        static void Girl3Rig() => BuildBones("girl_3");
        [MenuItem("KoitanLib/キャラ移植/girl_3/2. スプライトにボーンを書き込む")]
        static void Girl3Bones() => WriteSpriteBones("girl_3", "girl_3.png", "girl_3_0");
        [MenuItem("KoitanLib/キャラ移植/girl_3/3. SpriteSkin化")]
        static void Girl3Skin() => SetupSkin("girl_3", "girl_3.png", "girl_3_0");

        static string RigRoot(string chara) => chara + "_rig";
        static string Dir(string chara) => "Assets/Sprites/Charas/" + chara;

        /// <summary>移植元から持ってきた Animator Controller。4 体で共有している。</summary>
        const string ControllerPath = "Assets/Animations/Boy1/basis.controller";

        static RigData LoadRig(string chara)
        {
            string path = Dir(chara) + "/rig.json";
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            if (json == null)
            {
                Debug.LogError($"[CharaRigPorter] rig.json がありません: {path}");
                return null;
            }

            RigData data = JsonUtility.FromJson<RigData>(json.text);

            if (data == null || data.bones == null || data.bones.Length == 0)
            {
                Debug.LogError($"[CharaRigPorter] rig.json を読めませんでした: {path}");
                return null;
            }

            return data;
        }

        /// <summary>1) ボーンの GameObject 階層を作る。</summary>
        static void BuildBones(string chara)
        {
            RigData data = LoadRig(chara);
            if (data == null) return;

            GameObject root = GameObject.Find(RigRoot(chara)) ?? new GameObject(RigRoot(chara));
            Undo.RegisterFullObjectHierarchyUndo(root, "Build rig");

            root.transform.localScale = new Vector3(data.rootScale, data.rootScale, 1f);

            // bone 自体（クリップのパスが "bone/..." で始まるので必須）
            EnsurePath(root.transform, "bone");

            foreach (BoneRow b in data.bones)
            {
                Transform t = EnsurePath(root.transform, b.path);
                t.localPosition = new Vector3(b.px, b.py, 0f);
                t.localRotation = new Quaternion(0f, 0f, b.qz, b.qw);
                float s = Mathf.Approximately(b.scale, 0f) ? 1f : b.scale;
                t.localScale = new Vector3(s, s, 1f);
            }

            // 全クリップが IKs/arm_L などのパスにカーブを持つので、無いと警告が出る
            foreach (string ik in new[] { "IKs", "IKs/arm_L", "IKs/arm_R", "IKs/leg_L", "IKs/leg_R" })
            {
                EnsurePath(root.transform, ik);
            }

            // Animator。移植元の basis.controller を 4 体とも共有している
            // （クリップのカーブは "bone/hips/..." で解決されるので、上で組んだ階層に乗る）
            if (!root.TryGetComponent(out Animator animator)) animator = root.AddComponent<Animator>();

            RuntimeAnimatorController ctrl =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            if (ctrl != null)
            {
                animator.runtimeAnimatorController = ctrl;
                animator.applyRootMotion = false;
                // PC2D のモーターが FixedUpdate で動くので移植元と同じ Animate Physics
                animator.updateMode = AnimatorUpdateMode.Fixed;
            }
            else
            {
                Debug.LogWarning($"[CharaRigPorter] コントローラが見つかりません: {ControllerPath}");
            }

            Selection.activeGameObject = root;
            Debug.Log($"[CharaRigPorter] {chara}: ボーン {data.bones.Length} 本を生成、" +
                      $"Animator={(ctrl != null ? ctrl.name : "なし")}");
        }

        /// <summary>2) スプライトにボーン定義を書き込む。</summary>
        static void WriteSpriteBones(string chara, string pngName, string bodySpriteName)
        {
            RigData data = LoadRig(chara);
            if (data == null) return;

            GameObject rig = GameObject.Find(RigRoot(chara));

            if (rig == null)
            {
                Debug.LogError($"[CharaRigPorter] シーンに {RigRoot(chara)} がありません。先に 1 を実行してください。");
                return;
            }

            Transform boneRoot = rig.transform.Find("bone");
            if (boneRoot == null) { Debug.LogError("[CharaRigPorter] bone がありません。"); return; }

            string texPath = Dir(chara) + "/" + pngName;
            AssetImporter importer = AssetImporter.GetAtPath(texPath);

            if (importer == null) { Debug.LogError($"[CharaRigPorter] テクスチャがありません: {texPath}"); return; }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            SpriteRect target = null;

            foreach (SpriteRect r in provider.GetSpriteRects())
            {
                if (r.name == bodySpriteName) { target = r; break; }
            }

            if (target == null)
            {
                Debug.LogError($"[CharaRigPorter] スプライト {bodySpriteName} が見つかりません。");
                return;
            }

            List<Transform> ordered = new List<Transform>();
            Collect(boneRoot, ordered);

            // SpriteBone の座標はスプライトのピクセル座標系。リグはワールド単位なので変換する。
            float ppu = (importer as TextureImporter)?.spritePixelsPerUnit ?? 100f;
            Vector2 pivotPx = new Vector2(target.rect.width * target.pivot.x, target.rect.height * target.pivot.y);

            List<SpriteBone> bones = new List<SpriteBone>(ordered.Count);

            foreach (Transform t in ordered)
            {
                bool isRoot = t.parent == boneRoot;
                Vector3 pos;

                if (isRoot)
                {
                    Vector3 inRig = rig.transform.InverseTransformPoint(t.position);
                    pos = new Vector3(
                        (inRig.x - data.templateX) * ppu + pivotPx.x,
                        (inRig.y - data.templateY) * ppu + pivotPx.y,
                        0f);
                }
                else
                {
                    pos = t.localPosition * ppu;
                }

                bones.Add(new SpriteBone
                {
                    name = t.name,
                    position = pos,
                    rotation = isRoot ? t.rotation * Quaternion.Inverse(rig.transform.rotation) : t.localRotation,
                    length = (t.childCount > 0 ? t.GetChild(0).localPosition.magnitude : 0.25f) * ppu,
                    parentId = isRoot ? -1 : ordered.IndexOf(t.parent),
                });
            }

            provider.GetDataProvider<ISpriteBoneDataProvider>().SetBones(target.spriteID, bones);
            provider.Apply();
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            Debug.Log($"[CharaRigPorter] {chara}: {bodySpriteName} に {bones.Count} 本のボーンを書き込みました。" +
                      "次に Skinning Editor で Auto Geometry → Auto Weights → Apply を実行してください。");
        }

        /// <summary>3) スキニング済みスプライトを SpriteSkin として組み込む。</summary>
        static void SetupSkin(string chara, string pngName, string bodySpriteName)
        {
            GameObject rig = GameObject.Find(RigRoot(chara));

            if (rig == null) { Debug.LogError($"[CharaRigPorter] {RigRoot(chara)} がありません。"); return; }

            Transform boneRoot = rig.transform.Find("bone");
            if (boneRoot == null) { Debug.LogError("[CharaRigPorter] bone がありません。"); return; }

            Undo.RegisterFullObjectHierarchyUndo(rig, "Setup SpriteSkin");

            Sprite body = null;

            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(Dir(chara) + "/" + pngName))
            {
                if (o is Sprite s && s.name == bodySpriteName) { body = s; break; }
            }

            if (body == null) { Debug.LogError($"[CharaRigPorter] {bodySpriteName} を読めませんでした。"); return; }

            Transform meshTf = rig.transform.Find("mesh");

            if (meshTf == null)
            {
                meshTf = new GameObject("mesh").transform;
                meshTf.SetParent(rig.transform, false);
            }

            meshTf.localPosition = Vector3.zero;
            meshTf.localRotation = Quaternion.identity;
            meshTf.localScale = Vector3.one;

            if (!meshTf.TryGetComponent(out SpriteRenderer sr)) sr = meshTf.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = body;

            if (!meshTf.TryGetComponent(out SpriteSkin skin)) skin = meshTf.gameObject.AddComponent<SpriteSkin>();

            List<Transform> ordered = new List<Transform>();
            Collect(boneRoot, ordered);

            // rootBone / boneTransforms は読み取り専用プロパティなので直接フィールドに書く
            SerializedObject so = new SerializedObject(skin);
            so.FindProperty("m_RootBone").objectReferenceValue = ordered.Count > 0 ? ordered[0] : null;

            SerializedProperty prop = so.FindProperty("m_BoneTransforms");
            prop.arraySize = ordered.Count;

            for (int i = 0; i < ordered.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = ordered[i];

            so.ApplyModifiedProperties();

            Selection.activeGameObject = rig;
            Debug.Log($"[CharaRigPorter] {chara}: SpriteSkin 設定完了。ボーン {ordered.Count} 本、" +
                      $"rootBone={(ordered.Count > 0 ? ordered[0].name : "なし")}");
        }

        /// <summary>
        /// 4 体ぶんまとめてプレハブ化し、シーンからは消す。
        /// 作業用にシーンへ置いたリグを片付けるための仕上げ。
        /// </summary>
        [MenuItem("KoitanLib/キャラ移植/4体まとめてプレハブ化してシーンから消す")]
        static void SaveAllAsPrefabs()
        {
            const string dir = "Assets/Prefabs/Charas";

            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogError($"[CharaRigPorter] フォルダがありません: {dir}");
                return;
            }

            int saved = 0;

            foreach (string chara in new[] { "boy_1", "boy_2", "girl_2", "girl_3" })
            {
                GameObject go = GameObject.Find(RigRoot(chara));

                if (go == null)
                {
                    Debug.LogWarning($"[CharaRigPorter] シーンに {RigRoot(chara)} がありません。飛ばします。");
                    continue;
                }

                string path = $"{dir}/{RigRoot(chara)}.prefab";
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                saved++;

                Debug.Log($"[CharaRigPorter] {chara}: {path} に保存し、シーンから削除しました。");
            }

            Debug.Log($"[CharaRigPorter] プレハブ化 {saved} 体ぶん完了。");
        }

        static Transform EnsurePath(Transform root, string path)
        {
            Transform cur = root;

            foreach (string n in path.Split('/'))
            {
                Transform next = cur.Find(n);

                if (next == null)
                {
                    next = new GameObject(n).transform;
                    next.SetParent(cur, false);
                }

                cur = next;
            }

            return cur;
        }

        /// <summary>bone 配下を親が先に来る順で集める。見た目用のオブジェクトは除く。</summary>
        static void Collect(Transform boneRoot, List<Transform> result)
        {
            foreach (Transform c in boneRoot) Walk(c, result);
        }

        static void Walk(Transform t, List<Transform> result)
        {
            if (t.GetComponent<SpriteRenderer>() != null) return;

            result.Add(t);

            foreach (Transform c in t) Walk(c, result);
        }
    }
}
