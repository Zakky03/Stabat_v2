using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Koitan.EditorTools
{
    /// <summary>
    /// 移植したキャラを実際に操作できる状態にするツール。
    ///
    /// やること:
    ///  1. 移植元のクリップを使いつつ、このプロジェクトの流儀に合わせた
    ///     AnimatorController を生成する
    ///  2. リグに Rigidbody2D / BoxCollider2D / PlatformerMotor2D / PlayerController を付ける
    ///
    /// 移植元の basis.controller はパラメータが run / jump / ground / selected という
    /// 独自の並びで、こちらの PlayerController が期待する Run / Ground / Fall / Damage と
    /// 噛み合わない。ステート名も小文字なので animator.Play("Jump") も刺さらない。
    /// そこでクリップだけ流用して、kawaztan.controller と同じ形の物を作り直す。
    /// </summary>
    public static class CharaPlayableSetup
    {
        const string ClipDir = "Assets/Animations/Boy1";
        const string ControllerPath = "Assets/Animations/Boy1/fes_chara.controller";

        static readonly string[] Charas = { "boy_1", "boy_2", "girl_2", "girl_3" };

        [MenuItem("KoitanLib/キャラ移植/操作可能にする/1. Animatorコントローラを生成")]
        public static void BuildController()
        {
            AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            ctrl.AddParameter("Run", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Ground", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Fall", AnimatorControllerParameterType.Bool);
            ctrl.AddParameter("Damage", AnimatorControllerParameterType.Bool);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            // ステート名は PlayerController が animator.Play() で直接指定するものに合わせる
            AnimatorState idle   = AddState(sm, "Idle",   "idle");
            AnimatorState run    = AddState(sm, "Run",    "run");
            AnimatorState jump   = AddState(sm, "Jump",   "flying");
            AnimatorState fall   = AddState(sm, "Fall",   "flying");
            AnimatorState damage = AddState(sm, "Damage", "damage");
            AnimatorState throwS = AddState(sm, "Throw",  "throw");

            // 攻撃はまだ PlayerController から呼んでいないが、後で使えるよう用意しておく
            AddState(sm, "AttackPipe", "pipe_attack");
            AddState(sm, "AttackGun",  "attack_gun");

            sm.defaultState = idle;

            // 走る／止まる
            Bool(idle.AddTransition(run),  "Run", true);
            Bool(run.AddTransition(idle),  "Run", false);

            // 落下は接地していないときだけ
            AnimatorStateTransition t;
            t = idle.AddTransition(fall); Bool(t, "Fall", true); Cond(t, "Ground", false);
            t = run.AddTransition(fall);  Bool(t, "Fall", true); Cond(t, "Ground", false);
            Bool(fall.AddTransition(idle), "Ground", true);

            // ダメージから復帰
            Bool(damage.AddTransition(idle), "Damage", false);

            // 投げ・ジャンプはワンショットなので終わったら待機へ
            Exit(throwS.AddTransition(idle));
            Exit(jump.AddTransition(fall));

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();

            Debug.Log($"[CharaPlayableSetup] {ControllerPath} を生成しました" +
                      $"（ステート {sm.states.Length} 個、パラメータ 4 個）。");
        }

        [MenuItem("KoitanLib/キャラ移植/操作可能にする/2. 4体に操作用コンポーネントを付ける")]
        public static void SetupAll()
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            if (ctrl == null)
            {
                Debug.LogError($"[CharaPlayableSetup] {ControllerPath} がありません。先に 1 を実行してください。");
                return;
            }

            int done = 0;

            foreach (string chara in Charas)
            {
                GameObject rig = GameObject.Find(chara + "_rig");

                if (rig == null)
                {
                    Debug.LogWarning($"[CharaPlayableSetup] シーンに {chara}_rig がありません。飛ばします。");
                    continue;
                }

                Undo.RegisterFullObjectHierarchyUndo(rig, "Setup playable");

                rig.tag = "Player";

                // Animator は移植したコントローラに差し替える
                if (!rig.TryGetComponent(out Animator anim)) anim = rig.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.updateMode = AnimatorUpdateMode.Fixed;

                // PC2D のモーターは Kinematic な Rigidbody2D + BoxCollider2D が前提
                if (!rig.TryGetComponent(out Rigidbody2D rb)) rb = rig.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = true;

                if (!rig.TryGetComponent(out BoxCollider2D col)) col = rig.AddComponent<BoxCollider2D>();
                // 移植元プレハブの当たり判定（ルートが 0.55 倍なのでその前提の値）
                col.size = new Vector2(1f, 6.693011f);
                col.offset = new Vector2(0f, -0.12252116f);
                col.isTrigger = false;

                if (!rig.TryGetComponent(out PlatformerMotor2D motor)) motor = rig.AddComponent<PlatformerMotor2D>();

                // PC2D のモーターは接地判定をレイヤーマスクで行う。既定値のままだと
                // 何とも当たらず地面をすり抜けるので、既存の Player0 と同じ値を入れる。
                //   staticEnvLayerMask      = レイヤー8 Static Environment
                //   movingPlatformLayerMask = レイヤー9 Moving Platforms
                SerializedObject mso = new SerializedObject(motor);
                SetMask(mso, "staticEnvLayerMask", 1 << LayerMask.NameToLayer("Static Environment"));
                SetMask(mso, "movingPlatformLayerMask", 1 << LayerMask.NameToLayer("Moving Platforms"));
                mso.ApplyModifiedProperties();

                if (!rig.TryGetComponent(out PlayerController pc)) pc = rig.AddComponent<PlayerController>();

                // 物を持つ位置は左手のボーン（移植元も hand_L に持たせていた）
                Transform hand = FindDeep(rig.transform, "hand_L");

                SerializedObject so = new SerializedObject(pc);
                SerializedProperty handProp = so.FindProperty("handTf");

                if (handProp != null && hand != null) handProp.objectReferenceValue = hand;

                so.ApplyModifiedProperties();

                done++;
                Debug.Log($"[CharaPlayableSetup] {chara}: 操作用コンポーネントを設定しました" +
                          $"（handTf={(hand != null ? hand.name : "なし")}）。");
            }

            Debug.Log($"[CharaPlayableSetup] {done} 体ぶん完了。");
        }

        static void SetMask(SerializedObject so, string field, int mask)
        {
            SerializedProperty p = so.FindProperty(field);

            if (p == null) { Debug.LogWarning($"[CharaPlayableSetup] {field} が見つかりません。"); return; }

            p.intValue = mask;
        }

        static AnimatorState AddState(AnimatorStateMachine sm, string name, string clipName)
        {
            AnimatorState s = sm.AddState(name);
            s.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipDir}/{clipName}.anim");

            if (s.motion == null) Debug.LogWarning($"[CharaPlayableSetup] クリップが見つかりません: {clipName}.anim");

            return s;
        }

        static void Bool(AnimatorStateTransition t, string param, bool value)
        {
            t.hasExitTime = false;
            t.duration = 0.05f;
            Cond(t, param, value);
        }

        static void Cond(AnimatorStateTransition t, string param, bool value)
        {
            t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, param);
        }

        static void Exit(AnimatorStateTransition t)
        {
            t.hasExitTime = true;
            t.exitTime = 0.9f;
            t.duration = 0.05f;
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;

            foreach (Transform c in root)
            {
                Transform r = FindDeep(c, name);
                if (r != null) return r;
            }

            return null;
        }
    }
}
