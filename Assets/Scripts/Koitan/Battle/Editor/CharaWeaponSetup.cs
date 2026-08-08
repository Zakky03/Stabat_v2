using UnityEditor;
using UnityEngine;

namespace Koitan.EditorTools
{
    /// <summary>
    /// 移植したキャラに武器と攻撃判定を取り付けるツール。
    ///
    /// 位置・大きさは移植元プレハブ(boy_1)の値をそのまま使う。
    ///   パイプ  : bone/.../arm_lower_L の子。近接攻撃。判定は根元側と先端側の 2 つ
    ///   光線銃  : bone/.../hand_R の子。飛び道具の発射口を 3 つ持つ(3 方向に散る)
    ///
    /// 移植元は当たり判定を素の衝突(OnCollisionEnter2D)で取っていたが、
    /// こちらは PC2D のモーターが Rigidbody2D を Kinematic で使うため、
    /// Kinematic 同士では衝突コールバックが飛ばない。よって isTrigger にしてある
    /// (SubHitBox 側もトリガー前提で書いてある)。
    /// </summary>
    public static class CharaWeaponSetup
    {
        static readonly string[] Charas = { "boy_1", "boy_2", "girl_2", "girl_3" };

        const string ArmL = "bone/hips/spine_low/spine_high/shouder_L/arm_upper_L/arm_lower_L";
        const string HandR = "bone/hips/spine_low/spine_high/shoulder_R/arm_upper_R/arm_lower_R/hand_R";

        const string BulletPath = "Assets/Prefabs/Battle/Missile.prefab";

        /// <summary>
        /// 光線銃の弾のプレハブを作る。移植元の bullet.prefab を、
        /// こちらの Missile / HitBox / SubHitBox で組み直したもの。
        /// 絵と当たり判定の大きさ(0.59 x 0.23)は移植元のまま。
        /// </summary>
        /// <summary>
        /// 既にプレハブ化した 4 体のルートスケールだけを直す。
        /// リグを作り直すと武器や攻撃の設定が消えるので、スケールだけ差し替える。
        /// </summary>
        [MenuItem("KoitanLib/キャラ移植/操作可能にする/4. 大きさを既存キャラに合わせる")]
        public static void FixScale()
        {
            int done = 0;

            foreach (string chara in Charas)
            {
                string path = $"Assets/Prefabs/Charas/{chara}_rig.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    Debug.LogWarning($"[CharaWeaponSetup] プレハブがありません: {path}");
                    continue;
                }

                GameObject rig = PrefabUtility.LoadPrefabContents(path);

                Vector3 before = rig.transform.localScale;
                rig.transform.localScale = new Vector3(CharaRigPorter.RootScale, CharaRigPorter.RootScale, 1f);

                PrefabUtility.SaveAsPrefabAsset(rig, path);
                PrefabUtility.UnloadPrefabContents(rig);

                done++;
                Debug.Log($"[CharaWeaponSetup] {chara}: スケール {before.x:F3} → {CharaRigPorter.RootScale:F3}");
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[CharaWeaponSetup] {done} 体の大きさを直しました" +
                      $"（本体コライダー 6.693 x {CharaRigPorter.RootScale:F2} ≒ " +
                      $"{6.693f * CharaRigPorter.RootScale:F2}、kawaztan は 3.08）。");
        }

        [MenuItem("KoitanLib/キャラ移植/操作可能にする/3a. 弾のプレハブを作る")]
        public static void BuildBullet()
        {
            GameObject go = new GameObject("Missile");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Assets/Sprites/Weapons/tama.png");
            sr.sortingOrder = 30;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;   // 移植元も重力 0
            rb.gravityScale = 0f;

            go.AddComponent<Missile>();

            // 当たり判定。撃った人の情報は Missile が子の HitBox に流し込む
            GameObject hit = new GameObject("HitBox");
            hit.transform.SetParent(go.transform, false);
            hit.AddComponent<HitBox>();

            GameObject sub = new GameObject("Hit Box 1");
            sub.transform.SetParent(hit.transform, false);

            BoxCollider2D col = sub.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.59f, 0.23f);
            col.isTrigger = true;
            sub.AddComponent<SubHitBox>();

            string dir = System.IO.Path.GetDirectoryName(BulletPath);

            if (!AssetDatabase.IsValidFolder(dir))
            {
                Debug.LogError($"[CharaWeaponSetup] フォルダがありません: {dir}");
                Object.DestroyImmediate(go);
                return;
            }

            PrefabUtility.SaveAsPrefabAsset(go, BulletPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[CharaWeaponSetup] 弾のプレハブを作りました: {BulletPath}");
        }

        [MenuItem("KoitanLib/キャラ移植/操作可能にする/3. 武器と攻撃判定を付ける")]
        public static void SetupAll()
        {
            Sprite pipe = Load("Assets/Sprites/Weapons/pipe_isu.png");
            Sprite gun = Load("Assets/Sprites/Weapons/kousenju.png");
            Missile bullet = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath)?.GetComponent<Missile>();

            if (bullet == null)
                Debug.LogWarning($"[CharaWeaponSetup] 弾のプレハブがありません({BulletPath})。" +
                                 "先に「3a. 弾のプレハブを作る」を実行すると光線銃が撃てるようになります。");

            int done = 0;

            foreach (string chara in Charas)
            {
                // リグはプレハブ化済みなので、シーンに出さずプレハブを直接いじる
                string prefabPath = $"Assets/Prefabs/Charas/{chara}_rig.prefab";

                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
                {
                    Debug.LogWarning($"[CharaWeaponSetup] プレハブがありません: {prefabPath}。飛ばします。");
                    continue;
                }

                GameObject rig = PrefabUtility.LoadPrefabContents(prefabPath);

                Transform armL = rig.transform.Find(ArmL);
                Transform handR = rig.transform.Find(HandR);

                if (armL == null || handR == null)
                {
                    Debug.LogWarning($"[CharaWeaponSetup] {chara}: 腕のボーンが見つかりません " +
                                     $"(arm_lower_L={armL != null}, hand_R={handR != null})。飛ばします。");
                    PrefabUtility.UnloadPrefabContents(rig);
                    continue;
                }

                // --- パイプ(近接) ---
                Transform pipeTf = Ensure(armL, "pipe_isu");
                pipeTf.localPosition = new Vector3(0.47375268f, 2.1345508f, 0f);
                pipeTf.localRotation = new Quaternion(0f, 0f, -0.24728261f, -0.9689434f);
                pipeTf.localScale = new Vector3(0.60992813f, 0.60992813f, 1f);

                SpriteRenderer psr = Get<SpriteRenderer>(pipeTf);
                psr.sprite = pipe;
                psr.sortingOrder = 22;   // 手より手前に出す

                // 当たり判定の入れ物。攻撃中だけ有効化されるので普段は消しておく
                Transform hitRoot = Ensure(pipeTf, "AttackHitBox");
                hitRoot.localPosition = Vector3.zero;
                hitRoot.localRotation = Quaternion.identity;
                hitRoot.localScale = Vector3.one;

                HitBox hitBox = Get<HitBox>(hitRoot);

                // 実寸はコライダーではなく localScale 側で持つ(移植元と同じ作り)
                MakeSub(hitRoot, "Hit Box 1", new Vector3(0.331f, -1.044f, 0f), new Vector3(3.8916025f, 5.2508655f, 1f));
                MakeSub(hitRoot, "Hit Box 2", new Vector3(0.331f, 2.65f, 0f), new Vector3(3.891605f, 2.1825957f, 1f));

                hitRoot.gameObject.SetActive(false);

                // --- 光線銃(飛び道具) ---
                Transform gunTf = Ensure(handR, "kousenju");
                gunTf.localPosition = new Vector3(0.46745032f, 0.27250704f, 0f);
                gunTf.localRotation = new Quaternion(0f, 0f, -0.33532897f, 0.9421011f);
                // 移植元は X が負(左右反転)。銃口の向きに効くので符号ごと再現する
                gunTf.localScale = new Vector3(-0.19423676f, 0.19423676f, 1f);

                SpriteRenderer gsr = Get<SpriteRenderer>(gunTf);
                gsr.sprite = gun;
                gsr.sortingOrder = 6;

                MissileLauncher launcher = MakeMuzzle(gunTf, "muzzle", 0f, bullet);
                MakeMuzzle(gunTf, "muzzle (1)", -50.161f, bullet);
                MakeMuzzle(gunTf, "muzzle (2)", 9.839f, bullet);

                // --- PlayerAttack に登録 ---
                PlayerAttack attack = Get<PlayerAttack>(rig.transform);

                SerializedObject so = new SerializedObject(attack);
                so.FindProperty("animator").objectReferenceValue = rig.GetComponent<Animator>();

                SerializedProperty arr = so.FindProperty("attacks");
                arr.arraySize = 2;

                // 0 番: パイプの近接。移植元 pipe_attack.anim は判定リセット 0.183 秒、
                //        効果音 0.217 秒、発射 0.333 秒だったのでそれに寄せた尺にする
                SerializedProperty a0 = arr.GetArrayElementAtIndex(0);
                a0.FindPropertyRelative("name").stringValue = "パイプ";
                a0.FindPropertyRelative("hitBox").objectReferenceValue = hitBox;
                a0.FindPropertyRelative("launcher").objectReferenceValue = null;
                a0.FindPropertyRelative("animationStateName").stringValue = "AttackPipe";
                a0.FindPropertyRelative("startupTime").floatValue = 0.18f;
                a0.FindPropertyRelative("activeTime").floatValue = 0.15f;
                a0.FindPropertyRelative("recoveryTime").floatValue = 0.27f;

                // 1 番: 光線銃。移植元 attack_gun.anim は 0.4 秒で発射
                SerializedProperty a1 = arr.GetArrayElementAtIndex(1);
                a1.FindPropertyRelative("name").stringValue = "光線銃";
                a1.FindPropertyRelative("hitBox").objectReferenceValue = null;
                a1.FindPropertyRelative("launcher").objectReferenceValue = launcher;
                a1.FindPropertyRelative("animationStateName").stringValue = "AttackGun";
                a1.FindPropertyRelative("startupTime").floatValue = 0.4f;
                a1.FindPropertyRelative("activeTime").floatValue = 0.05f;
                a1.FindPropertyRelative("recoveryTime").floatValue = 0.14f;

                so.ApplyModifiedProperties();

                // PlayerController 側に PlayerAttack を差す
                PlayerController pc = rig.GetComponent<PlayerController>();

                if (pc != null)
                {
                    SerializedObject pso = new SerializedObject(pc);
                    SerializedProperty pa = pso.FindProperty("playerAttack");

                    if (pa != null) pa.objectReferenceValue = attack;

                    pso.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(rig, prefabPath);
                PrefabUtility.UnloadPrefabContents(rig);

                done++;
                Debug.Log($"[CharaWeaponSetup] {chara}: パイプ・光線銃・攻撃判定を設定しました。");
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[CharaWeaponSetup] {done} 体ぶん完了。攻撃は X=パイプ / Y=光線銃 です" +
                      $"（弾={(bullet != null ? "設定済み" : "未設定")}）。");
        }

        static Sprite Load(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (s == null) Debug.LogWarning($"[CharaWeaponSetup] スプライトを読めません: {path}");

            return s;
        }

        static Transform Ensure(Transform parent, string name)
        {
            Transform t = parent.Find(name);

            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }

            return t;
        }

        static T Get<T>(Transform t) where T : Component
        {
            return t.TryGetComponent(out T c) ? c : t.gameObject.AddComponent<T>();
        }

        static void MakeSub(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            Transform t = Ensure(parent, name);
            t.localPosition = pos;
            t.localRotation = Quaternion.identity;
            t.localScale = scale;

            BoxCollider2D col = Get<BoxCollider2D>(t);
            col.size = Vector2.one;
            col.offset = Vector2.zero;
            col.isTrigger = true;   // Kinematic 同士でも当たるように

            Get<SubHitBox>(t);
        }

        static MissileLauncher MakeMuzzle(Transform parent, string name, float angleZ, Missile bullet)
        {
            Transform t = Ensure(parent, name);
            t.localPosition = new Vector3(-4.5f, 2.82f, 0f);
            t.localRotation = Quaternion.Euler(0f, 0f, angleZ);
            t.localScale = new Vector3(-2.9328716f, 2.932874f, 1f);

            MissileLauncher l = Get<MissileLauncher>(t);

            // 弾と速度を入れる。速度は移植元の InstantiateMissile と同じ 14
            SerializedObject so = new SerializedObject(l);
            SerializedProperty mp = so.FindProperty("missilePrefab");
            SerializedProperty sp = so.FindProperty("speed");

            if (mp != null && bullet != null) mp.objectReferenceValue = bullet;
            if (sp != null) sp.floatValue = 14f;

            so.ApplyModifiedProperties();

            return l;
        }
    }
}
