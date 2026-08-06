using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Koitan.EditorTools
{
    /// <summary>
    /// FesGame18 の boy_1 のリグを、このプロジェクト（Unity 6）に組み直すための一回きりのツール。
    ///
    /// 移植元は Anima2D（Unity 2018 専用）でスキニングしていたが、Unity 6 には存在しない。
    /// そこでスキニングを使わない「カットアウト方式」で組み直す：
    /// 体のパーツ 1 枚ごとに SpriteRenderer を作り、対応するボーンの子にぶら下げる。
    /// こうするとウェイト塗りが一切不要で、かつ移植元の .anim（ボーンの Transform を
    /// 動かしているだけ）がそのまま使える。
    ///
    /// ボーンの名前・階層・並び順は移植元と完全に一致させてある。クリップのカーブが
    /// "bone/hips/spine_low/..." というパスで解決されるため、1 文字でも変えると動かなくなる。
    /// 左肩の "shouder_L"（l 抜けの綴りミス）も移植元のまま再現している。
    /// </summary>
    public static class Boy1RigBuilder
    {
        const string RootName = "boy_1_rig";
        const string PrefabPath = "Assets/Prefabs/Charas/boy_1_rig.prefab";
        const string ControllerPath = "Assets/Animations/Boy1/basis.controller";

        /// <summary>移植元プレハブのルートのスケール。</summary>
        const float RootScale = 0.55f;

        /// <summary>path, localPosition.x, y, localRotation.z, w, uniformScale(xy)</summary>
        static readonly object[][] Bones =
        {
            new object[] { "bone", 0f, 0f, 0f, 1f, 1f },
            new object[] { "bone/hips", 0.25024432f, -0.6401962f, 0.7384228f, 0.67433804f, 1f },
            new object[] { "bone/hips/spine_low", 0.5298879f, 0f, -0.016416881f, 0.99986523f, 1f },
            new object[] { "bone/hips/spine_low/spine_high", 0.35482943f, 0f, 0.08036464f, 0.99676555f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck", 0.43657836f, 0f, -0.020284904f, 0.99979424f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head", 0.2873276f, 0f, -0.089172475f, 0.9960162f, 1f },

            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs1-1", 1.6986749f, -0.47839943f, 0.94827724f, -0.3174436f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs1-1/bangs1-2", 0.543434f, 0f, -0.22273849f, 0.97487825f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs1-1/bangs1-2/bangs1-3", 0.5350399f, 0f, -0.25202882f, 0.96771973f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs2-1", 1.5930561f, -0.19359657f, 0.96319556f, 0.2688016f, 1.0000005f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs2-1/bangs2-2", 0.48131222f, 0f, 0.093489975f, 0.99562025f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs2-1/bangs2-2/bangs2-3", 0.47587034f, 0f, 0.04273406f, 0.9990865f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs3-1", 1.7215915f, 0.19705276f, 0.91228783f, 0.4095497f, 1.0000008f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs3-1/bangs3-2", 0.48474568f, 0f, 0.22817194f, 0.97362095f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/bangs3-1/bangs3-2/bangs3-3", 0.51492876f, 0f, 0.20255296f, 0.9792713f, 1f },

            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair1-1", 1.8056538f, 0.46517557f, 0.9585073f, 0.28506815f, 0.99999976f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair1-1/hair1-2", 0.6871247f, 0f, 0.21302508f, 0.9770467f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair1-1/hair1-2/hair1-3", 0.5582614f, 0f, 0.25494605f, 0.9669553f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair2-1", 1.8058358f, 0.03932178f, 0.99993f, 0.0118288845f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair2-1/hair2-2", 0.6871247f, 0f, 0.018228946f, 0.9998339f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair2-1/hair2-2/hair2-3", 0.5582614f, 0f, -0.013874871f, 0.9999038f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair3-1", 1.806021f, -0.39329183f, 0.9575531f, -0.28825697f, 1.0000007f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair3-1/hair3-2", 0.6871247f, 0f, -0.20735854f, 0.97826505f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/neck/head/hair3-1/hair3-2/hair3-3", 0.5582614f, 0f, -0.13891025f, 0.990305f, 1f },

            new object[] { "bone/hips/spine_low/spine_high/shoulder_R", 0.40228367f, 0.15647039f, 0.7246017f, 0.6891679f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shoulder_R/arm_upper_R", 0.211054f, 0f, 0.48558927f, 0.8741871f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shoulder_R/arm_upper_R/arm_lower_R", 0.82595253f, 0f, 0.033807084f, 0.9994284f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shoulder_R/arm_upper_R/arm_lower_R/hand_R", 0.65543616f, 0f, 0.098645665f, 0.9951227f, 1f },

            // 綴りミスは移植元のまま（クリップのパスが "shouder_L" に依存している）
            new object[] { "bone/hips/spine_low/spine_high/shouder_L", 0.33973724f, -0.21060741f, 0.91422904f, -0.40519792f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shouder_L/arm_upper_L", 0.20096125f, 0f, -0.40180588f, 0.9157249f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shouder_L/arm_upper_L/arm_lower_L", 0.82595253f, 0f, 0.014373229f, 0.9998967f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shouder_L/arm_upper_L/arm_lower_L/hand_L", 0.5635981f, 0f, -0.11616371f, 0.9932301f, 1f },

            new object[] { "bone/hips/leg_upper_R", -0.2468811f, 0.31077558f, 0.98899907f, 0.14792183f, 1f },
            new object[] { "bone/hips/leg_upper_R/leg_lower_R", 1.1103598f, 0f, -0.041992895f, 0.9991179f, 1f },
            new object[] { "bone/hips/leg_upper_R/leg_lower_R/foot_R", 1.2014266f, 0f, 0.1725855f, 0.9849946f, 1f },
            new object[] { "bone/hips/leg_upper_L", -0.3100067f, -0.20351449f, 0.99966115f, 0.02603018f, 1.0000002f },
            new object[] { "bone/hips/leg_upper_L/leg_lower_L", 1.0776477f, 0f, -0.03429459f, 0.99941176f, 1f },
            new object[] { "bone/hips/leg_upper_L/leg_lower_L/foot_L", 1.2014266f, 0f, 0.55062413f, 0.8347533f, 1f },

            // IK ターゲット。全クリップがこの 4 つのパスにカーブを持っているので必須
            new object[] { "IKs", 0f, 0f, 0f, 1f, 1f },
            new object[] { "IKs/arm_L", 0.8155677f, -0.8306657f, 0f, 1f, 1f },
            new object[] { "IKs/arm_R", -0.639441f, -0.8871278f, 0f, 1f, 1f },
            new object[] { "IKs/leg_L", 0.48652494f, -3.208236f, 0f, 1f, 1f },
            new object[] { "IKs/leg_R", -0.60819554f, -3.152185f, 0f, 1f, 1f },

            // 足元・手・銃口などのアンカー（移植元では bone の中に埋まっていたものを含む）
            new object[] { "asimoto", 0f, -3.54f, 0f, 1f, 1f },
            new object[] { "bone/hips/spine_low/spine_high/shoulder_R/arm_upper_R/arm_lower_R/hand_R/muzzle", 0.46745032f, 0.27250704f, -0.33532897f, 0.9421011f, 1f },
        };

        /// <summary>
        /// mesh 配下のパーツ。移植元の SpriteMeshInstance が主ボーンとして挙げていたものに
        /// ぶら下げる。要素は spriteName, 親ボーンのパス, localPosition.x, y, sortingOrder。
        /// 位置は移植元では mesh 直下（＝ルート基準）だったので、親を変えた後に
        /// ワールド位置が変わらないよう、生成時にワールド座標で置き直している。
        /// </summary>
        static readonly object[][] Parts =
        {
            new object[] { "chara_boy_1_21", "bone/hips/spine_low/spine_high/neck/head", 0.014f, 1.67f, 10 },
            new object[] { "chara_boy_1_20", "bone/hips/spine_low/spine_high/neck/head", 0.008f, 2.032f, 4 },
            new object[] { "chara_boy_1_7",  "bone/hips/spine_low/spine_high/neck/head/hair1-1", 0.019f, 1.72f, 0 },
            new object[] { "chara_boy_1_14", "bone/hips/spine_low/spine_high/neck/head/bangs1-1", 0.079f, 1.83f, 16 },
            new object[] { "chara_boy_1_1",  "bone/hips/spine_low/spine_high/neck/head", 0.045f, 2.535f, 18 },
            new object[] { "chara_boy_1_13", "bone/hips/spine_low/spine_high/neck", 0.09000767f, 0.7001001f, 6 },
            new object[] { "chara_boy_1_2",  "bone/hips/spine_low/spine_high/shoulder_R", -0.3457f, 0.3347f, 21 },
            new object[] { "chara_boy_1_4",  "bone/hips/spine_low/spine_high/shouder_L", 0.53304964f, 0.35530356f, 5 },
            new object[] { "chara_boy_1_6",  "bone/hips", 0.5044f, -0.13f, 19 },
            new object[] { "chara_boy_1_5",  "bone/hips", -0.15f, -0.15f, 20 },
            new object[] { "chara_boy_1_3",  "bone/hips/spine_low/spine_high", 0.195f, -0.095f, 10 },
            new object[] { "chara_boy_1_9",  "bone/hips", 0.18f, -1.07f, 3 },
            new object[] { "chara_boy_1_11", "bone/hips/leg_upper_R", -0.26f, -2.226f, 5 },
            new object[] { "chara_boy_1_12", "bone/hips/leg_upper_L", 0.471f, -2.165f, 4 },
            new object[] { "chara_boy_1_8",  "bone/hips/spine_low/spine_high/shoulder_R/arm_upper_R", -0.5375076f, -0.631f, 21 },
            new object[] { "arm_L",          "bone/hips/spine_low/spine_high/shouder_L/arm_upper_L", 0.739f, -0.534f, 4 },
            new object[] { "eye_R",          "bone/hips/spine_low/spine_high/neck/head", -0.228f, 1.563f, 15 },
            new object[] { "eye_L",          "bone/hips/spine_low/spine_high/neck/head", 0.531f, 1.561f, 15 },
            new object[] { "eyebrows_R",     "bone/hips/spine_low/spine_high/neck/head", -0.248f, 1.9368862f, 15 },
            new object[] { "eyebrows_L",     "bone/hips/spine_low/spine_high/neck/head", 0.6017077f, 1.932f, 15 },
            new object[] { "mouth",          "bone/hips/spine_low/spine_high/neck/head", 0.1774f, 1.031f, 15 },
        };

        // 移植元では mesh の子の名前がスプライト名と一致しないものがある（arm_L / eye_R など）。
        // その分だけアトラス内のスプライト名に読み替える。
        static readonly Dictionary<string, string> PartSpriteOverride = new Dictionary<string, string>
        {
            { "arm_L", "chara_boy_1_15" },
            { "eye_R", "chara_boy_1_23" },
            { "eye_L", "chara_boy_1_24" },
            { "eyebrows_R", "chara_boy_1_19" },
            { "eyebrows_L", "chara_boy_1_20" },
            { "mouth", "chara_boy_1_27" },
        };

        [MenuItem("KoitanLib/boy_1 リグを生成")]
        public static void Build()
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null) root = new GameObject(RootName);

            Undo.RegisterFullObjectHierarchyUndo(root, "Build boy_1 rig");

            // 移植元のプレハブのルートは 0.55 倍だった。パーツの座標はこの倍率を前提に
            // 打ってあるので、ここを 1 のままにすると既存キャラより明らかに大きくなる。
            // 位置は置き場所の都合があるので触らない。
            root.transform.localScale = new Vector3(RootScale, RootScale, 1f);

            // 1) ボーンとアンカー
            foreach (object[] b in Bones)
            {
                Transform t = EnsurePath(root.transform, (string)b[0]);
                t.localPosition = new Vector3((float)b[1], (float)b[2], 0f);
                t.localRotation = new Quaternion(0f, 0f, (float)b[3], (float)b[4]);
                float s = (float)b[5];
                t.localScale = new Vector3(s, s, 1f);
            }

            // 2) 体のパーツ
            Sprite[] sprites = LoadAtlasSprites();

            int placed = 0, missing = 0;

            foreach (object[] p in Parts)
            {
                string objName = (string)p[0];
                Transform parent = root.transform.Find((string)p[1]);

                if (parent == null)
                {
                    Debug.LogWarning($"[Boy1RigBuilder] 親ボーンが見つかりません: {p[1]}");
                    missing++;
                    continue;
                }

                Transform part = parent.Find(objName);

                if (part == null)
                {
                    part = new GameObject(objName).transform;
                    part.SetParent(parent, false);
                }

                // 移植元ではパーツは mesh（原点・無回転のただの入れ物）の子で、
                // スプライト自身は常に無回転だった（ボーンが回っても Anima2D がメッシュを
                // 変形させるだけで、パーツの Transform は動かない）。
                // なので初期姿勢では「ワールドで無回転・等倍」にしておく必要がある。
                // ここを localRotation = identity にすると親ボーンの回転（hips は約 95 度、
                // 脚は 163/177 度）をそのまま被って、初期姿勢からして全部傾いてしまう。
                part.position = root.transform.TransformPoint(new Vector3((float)p[2], (float)p[3], 0f));
                part.rotation = root.transform.rotation;
                part.localScale = InverseParentScale(part.parent, root.transform);

                if (!part.TryGetComponent(out SpriteRenderer sr)) sr = part.gameObject.AddComponent<SpriteRenderer>();

                string spriteName = PartSpriteOverride.TryGetValue(objName, out string ov) ? ov : objName;
                Sprite sprite = FindSprite(sprites, spriteName);

                if (sprite != null)
                {
                    sr.sprite = sprite;
                    placed++;
                }
                else
                {
                    Debug.LogWarning($"[Boy1RigBuilder] スプライトが見つかりません: {spriteName}");
                    missing++;
                }

                sr.sortingOrder = (int)p[4];
            }

            // 3) Animator。移植元の basis.controller をそのまま使う。
            //    クリップのカーブは "bone/hips/..." で解決されるので、上で組んだ階層に
            //    名前が一致していればそのまま再生できる。
            if (!root.TryGetComponent(out Animator animator)) animator = root.AddComponent<Animator>();

            RuntimeAnimatorController ctrl =
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            if (ctrl != null)
            {
                animator.runtimeAnimatorController = ctrl;
                animator.applyRootMotion = false;
                // 移植元と同じ Animate Physics。PC2D のモーターが FixedUpdate で動くため
                animator.updateMode = AnimatorUpdateMode.Fixed;
            }
            else
            {
                Debug.LogWarning($"[Boy1RigBuilder] コントローラが見つかりません: {ControllerPath}");
                missing++;
            }

            Selection.activeGameObject = root;

            Debug.Log($"[Boy1RigBuilder] 完了: ボーン {Bones.Length} 個、パーツ {placed} 枚を配置" +
                      $"、Animator={(ctrl != null ? ctrl.name : "なし")}" +
                      (missing > 0 ? $"（未解決 {missing} 件。アトラスの取り込みを確認してください）" : ""));
        }

        /// <summary>
        /// 移植した攻撃システム（PlayerAttack / HitBox / SubHitBox）を boy_1 に配線する。
        /// 当たり判定の位置と大きさは移植元の Hit Box 1 / Hit Box 2 の値をそのまま使う。
        ///
        /// 移植元は当たり判定を素の衝突（OnCollisionEnter2D）で取っていたが、こちらは
        /// PC2D のモーターが Rigidbody2D を Kinematic で使うため Kinematic 同士では
        /// 衝突コールバックが飛ばない。よって isTrigger にしてある（SubHitBox 側もトリガー前提）。
        /// </summary>
        [MenuItem("KoitanLib/boy_1 に攻撃判定を追加")]
        public static void BuildAttack()
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                Debug.LogError($"[Boy1RigBuilder] シーンに {RootName} がありません。先に「boy_1 リグを生成」を実行してください。");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Build boy_1 attack");

            // パイプ（近接武器）の付け根。移植元では arm_lower_L の子だった。
            Transform pipe = EnsurePath(root.transform,
                "bone/hips/spine_low/spine_high/shouder_L/arm_upper_L/arm_lower_L/pipe_isu");
            pipe.localPosition = new Vector3(0.47375268f, 2.1345508f, 0f);
            pipe.localRotation = new Quaternion(0f, 0f, -0.24728261f, -0.9689434f);
            pipe.localScale = new Vector3(0.60992813f, 0.60992813f, 1f);

            // HitBox は攻撃中だけ有効化されるので、その入れ物を pipe の子に作る
            Transform hitRoot = EnsurePath(pipe, "AttackHitBox");
            hitRoot.localPosition = Vector3.zero;
            hitRoot.localRotation = Quaternion.identity;
            hitRoot.localScale = Vector3.one;

            if (!hitRoot.TryGetComponent(out HitBox hitBox)) hitBox = hitRoot.gameObject.AddComponent<HitBox>();

            // 実際の判定コライダー。移植元は 2 つ（根元側と先端側）だった
            CreateSubHitBox(hitRoot, "Hit Box 1", new Vector3(0.331f, -1.044f, 0f), new Vector3(3.8916025f, 5.2508655f, 1f));
            CreateSubHitBox(hitRoot, "Hit Box 2", new Vector3(0.331f, 2.65f, 0f), new Vector3(3.891605f, 2.1825957f, 1f));

            hitRoot.gameObject.SetActive(false);

            // PlayerAttack。攻撃 0 番＝パイプの近接攻撃として登録する
            if (!root.TryGetComponent(out PlayerAttack attack)) attack = root.AddComponent<PlayerAttack>();

            SerializedObject so = new SerializedObject(attack);
            so.FindProperty("animator").objectReferenceValue = root.GetComponent<Animator>();

            SerializedProperty attacks = so.FindProperty("attacks");
            attacks.arraySize = 1;

            SerializedProperty a0 = attacks.GetArrayElementAtIndex(0);
            a0.FindPropertyRelative("name").stringValue = "パイプ";
            a0.FindPropertyRelative("hitBox").objectReferenceValue = hitBox;
            a0.FindPropertyRelative("launcher").objectReferenceValue = null;
            // 移植元の pipe_attack.anim はヒット判定リセットが 0.183 秒、発射が 0.333 秒。
            // それに合わせて発生 0.18 / 持続 0.15 / 硬直 0.27（全体 0.6 秒）とする。
            a0.FindPropertyRelative("animationStateName").stringValue = "attack_pipe";
            a0.FindPropertyRelative("startupTime").floatValue = 0.18f;
            a0.FindPropertyRelative("activeTime").floatValue = 0.15f;
            a0.FindPropertyRelative("recoveryTime").floatValue = 0.27f;

            so.ApplyModifiedProperties();

            Selection.activeGameObject = root;
            Debug.Log("[Boy1RigBuilder] 攻撃判定を追加しました: PlayerAttack 1 種類、SubHitBox 2 個（すべて isTrigger）");
        }

        static void CreateSubHitBox(Transform parent, string name, Vector3 localPos, Vector3 localScale)
        {
            Transform t = EnsurePath(parent, name);
            t.localPosition = localPos;
            t.localRotation = Quaternion.identity;
            t.localScale = localScale;

            if (!t.TryGetComponent(out BoxCollider2D col)) col = t.gameObject.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;   // 実寸は localScale 側で持つ（移植元と同じ作り）
            col.offset = Vector2.zero;
            col.isTrigger = true;     // Kinematic 同士でも当たるようにトリガーにする

            if (!t.TryGetComponent(out SubHitBox _)) t.gameObject.AddComponent<SubHitBox>();
        }

        [MenuItem("KoitanLib/boy_1 リグをプレハブ化")]
        public static void SaveAsPrefab()
        {
            GameObject root = GameObject.Find(RootName);

            if (root == null)
            {
                Debug.LogError($"[Boy1RigBuilder] シーンに {RootName} がありません。先に「boy_1 リグを生成」を実行してください。");
                return;
            }

            string dir = System.IO.Path.GetDirectoryName(PrefabPath);

            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogError($"[Boy1RigBuilder] フォルダがありません: {dir}");
                return;
            }

            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.UserAction);
            Debug.Log($"[Boy1RigBuilder] プレハブを保存しました: {PrefabPath}");
        }

        /// <summary>
        /// 親の合成スケールを打ち消して、ワールドでのスケールがルートと同じになる localScale を返す。
        /// ボーンのスケールはほぼ 1 なので実質 1 になるが、念のため。
        /// </summary>
        static Vector3 InverseParentScale(Transform parent, Transform root)
        {
            Vector3 p = parent.lossyScale;
            Vector3 r = root.lossyScale;

            return new Vector3(
                Mathf.Approximately(p.x, 0f) ? 1f : r.x / p.x,
                Mathf.Approximately(p.y, 0f) ? 1f : r.y / p.y,
                Mathf.Approximately(p.z, 0f) ? 1f : r.z / p.z);
        }

        static Transform EnsurePath(Transform root, string path)
        {
            Transform current = root;

            foreach (string name in path.Split('/'))
            {
                Transform next = current.Find(name);

                if (next == null)
                {
                    next = new GameObject(name).transform;
                    next.SetParent(current, false);
                }

                current = next;
            }

            return current;
        }

        /// <summary>取り込んだ chara_boy_1.png の中の全スプライトを取ってくる。</summary>
        static Sprite[] LoadAtlasSprites()
        {
            foreach (string guid in AssetDatabase.FindAssets("chara_boy_1 t:Texture2D"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith("chara_boy_1.png")) continue;

                List<Sprite> list = new List<Sprite>();

                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (o is Sprite s) list.Add(s);
                }

                if (list.Count > 0) return list.ToArray();
            }

            Debug.LogWarning("[Boy1RigBuilder] chara_boy_1.png が見つかりません。" +
                             "先にアトラスを Assets 以下に取り込み、Sprite Mode を Multiple にしてください。");
            return new Sprite[0];
        }

        static Sprite FindSprite(Sprite[] sprites, string name)
        {
            foreach (Sprite s in sprites)
            {
                if (s.name == name) return s;
            }

            return null;
        }
    }
}
