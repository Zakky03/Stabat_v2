using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

namespace Koitan.EditorTools
{
    /// <summary>
    /// FesGame18 のキャラを「パーツごと」に移植するツール。kawaztan と同じ構造にする。
    ///
    /// 移植元(Anima2D)は、体のパーツ 1 枚ごとに
    ///   ・メッシュの頂点（アトラスのピクセル座標）
    ///   ・三角形
    ///   ・作者が手で塗ったボーンウェイト
    ///   ・どのボーンに紐づくかの名前
    /// を持っている。これを parts.json に抜き出してあるので、そのまま Unity 6 の
    /// スプライトに書き込む。Auto Geometry も Auto Weights も使わないので、
    /// 見た目は移植元と同一になり、関節が膨らむこともない。
    ///
    /// パーツごとに SpriteRenderer + SpriteSkin を持たせるため、
    /// 持ち物（爆弾など）を腕と体の間に挟むような重ね順も作れる。
    /// </summary>
    public static class CharaPartsPorter
    {
        const float PPU = 100f;

        [System.Serializable]
        public class PartData
        {
            public string obj;
            public float posX, posY;
            public int order;
            public float pivotX, pivotY;   // アトラスのピクセル座標でのアンカー
            public string[] bones;         // boneIndex -> ボーン名
            public float[] verts;          // x,y,x,y,... アトラスのピクセル座標
            public float[] weights;        // w0,w1,w2,w3,b0,b1,b2,b3 の 8 個ずつ
            public int[] tris;
        }

        [System.Serializable]
        public class PartsFile
        {
            public string chara;
            public float templateX, templateY;
            public PartData[] parts;
        }

        [MenuItem("KoitanLib/キャラ移植/パーツ方式/boy_1")]
        static void P1() => Port("boy_1", "chara_boy_1.png");
        [MenuItem("KoitanLib/キャラ移植/パーツ方式/boy_2")]
        static void P2() => Port("boy_2", "chara_boy_2.png");
        [MenuItem("KoitanLib/キャラ移植/パーツ方式/girl_2")]
        static void P3() => Port("girl_2", "chara_girl_1.png");
        [MenuItem("KoitanLib/キャラ移植/パーツ方式/girl_3")]
        static void P4() => Port("girl_3", "girl_3.png");

        [MenuItem("KoitanLib/キャラ移植/パーツ方式/4体まとめて")]
        static void PAll()
        {
            Port("boy_1", "chara_boy_1.png");
            Port("boy_2", "chara_boy_2.png");
            Port("girl_2", "chara_girl_1.png");
            Port("girl_3", "girl_3.png");
        }

        static void Port(string chara, string pngName)
        {
            string dir = "Assets/Sprites/Charas/" + chara;
            string texPath = dir + "/" + pngName;

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(dir + "/parts.json");

            if (json == null) { Debug.LogError($"[CharaPartsPorter] parts.json がありません: {dir}"); return; }

            PartsFile file = JsonUtility.FromJson<PartsFile>(json.text);

            if (file == null || file.parts == null || file.parts.Length == 0)
            {
                Debug.LogError($"[CharaPartsPorter] parts.json を読めませんでした: {dir}");
                return;
            }

            GameObject rig = GameObject.Find(chara + "_rig");

            if (rig == null)
            {
                Debug.LogError($"[CharaPartsPorter] シーンに {chara}_rig がありません。" +
                               "先に「キャラ移植/<chara>/1. リグを生成」を実行してください。");
                return;
            }

            Transform boneRoot = rig.transform.Find("bone");
            if (boneRoot == null) { Debug.LogError("[CharaPartsPorter] bone がありません。"); return; }

            // 全ボーンを親が先に来る順で集める。全パーツのスプライトにこの並びで書き込むので、
            // ウェイトの boneIndex はこのリストの添字に読み替える。
            List<Transform> bones = new List<Transform>();
            CollectBones(boneRoot, bones);

            Dictionary<string, int> boneIndexByName = new Dictionary<string, int>();

            for (int i = 0; i < bones.Count; i++) boneIndexByName[bones[i].name] = i;

            AssetImporter importer = AssetImporter.GetAtPath(texPath);
            if (importer == null) { Debug.LogError($"[CharaPartsPorter] テクスチャがありません: {texPath}"); return; }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();

            var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
            var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();

            if (boneProvider == null || meshProvider == null)
            {
                Debug.LogError("[CharaPartsPorter] スプライトのボーン／メッシュ用データプロバイダが取れませんでした。");
                return;
            }

            // 矩形をテクスチャ内に収めるために寸法が要る
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            int texW = tex != null ? tex.width : int.MaxValue;
            int texH = tex != null ? tex.height : int.MaxValue;

            List<SpriteRect> rects = new List<SpriteRect>(provider.GetSpriteRects());
            Dictionary<string, SpriteRect> byName = new Dictionary<string, SpriteRect>();

            foreach (SpriteRect r in rects) byName[r.name] = r;

            // 1) パーツごとにスプライトを用意（頂点の外接矩形をそのまま使う）
            List<(PartData part, SpriteRect rect)> made = new List<(PartData, SpriteRect)>();

            foreach (PartData p in file.parts)
            {
                if (p.verts == null || p.verts.Length < 6) continue;

                float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

                for (int i = 0; i + 1 < p.verts.Length; i += 2)
                {
                    minX = Mathf.Min(minX, p.verts[i]); maxX = Mathf.Max(maxX, p.verts[i]);
                    minY = Mathf.Min(minY, p.verts[i + 1]); maxY = Mathf.Max(maxY, p.verts[i + 1]);
                }

                // 1px 余裕を持たせる（境界の頂点が矩形外に落ちないように）。
                // ただしテクスチャの外にはみ出すとスプライトが生成されないのでクランプする。
                int rx = Mathf.Max(0, Mathf.FloorToInt(minX) - 1);
                int ry = Mathf.Max(0, Mathf.FloorToInt(minY) - 1);
                int rw = Mathf.Min(texW - rx, Mathf.CeilToInt(maxX) - rx + 2);
                int rh = Mathf.Min(texH - ry, Mathf.CeilToInt(maxY) - ry + 2);

                string spriteName = chara + "_" + p.obj;
                Rect rect = new Rect(rx, ry, rw, rh);
                Vector2 pivot = new Vector2((p.pivotX - rx) / rw, (p.pivotY - ry) / rh);

                if (!byName.TryGetValue(spriteName, out SpriteRect sr))
                {
                    sr = new SpriteRect { name = spriteName, spriteID = GUID.Generate() };
                    rects.Add(sr);
                    byName[spriteName] = sr;
                }

                sr.rect = rect;
                sr.alignment = SpriteAlignment.Custom;
                sr.pivot = pivot;
                sr.border = Vector4.zero;

                made.Add((p, sr));
            }

            provider.SetSpriteRects(rects.ToArray());

            // 2) 各スプライトにボーン・頂点・三角形・ウェイトを書き込む
            foreach ((PartData p, SpriteRect sr) in made)
            {
                // ボーン座標はこのスプライトのピクセル座標系。
                // パーツの原点（GameObject の置き場所）を基準に全ボーンを並べ直す。
                Vector2 pivotPx = new Vector2(sr.rect.width * sr.pivot.x, sr.rect.height * sr.pivot.y);
                Vector2 partPos = new Vector2(p.posX, p.posY);

                List<SpriteBone> sb = new List<SpriteBone>(bones.Count);

                for (int i = 0; i < bones.Count; i++)
                {
                    Transform t = bones[i];
                    bool isRoot = t.parent == boneRoot;

                    Vector3 pos;

                    if (isRoot)
                    {
                        Vector3 inRig = rig.transform.InverseTransformPoint(t.position);
                        pos = new Vector3(
                            (inRig.x - partPos.x) * PPU + pivotPx.x,
                            (inRig.y - partPos.y) * PPU + pivotPx.y,
                            0f);
                    }
                    else
                    {
                        pos = t.localPosition * PPU;
                    }

                    sb.Add(new SpriteBone
                    {
                        name = t.name,
                        position = pos,
                        rotation = isRoot ? t.rotation * Quaternion.Inverse(rig.transform.rotation) : t.localRotation,
                        length = (t.childCount > 0 ? t.GetChild(0).localPosition.magnitude : 0.25f) * PPU,
                        parentId = isRoot ? -1 : bones.IndexOf(t.parent),
                    });
                }

                boneProvider.SetBones(sr.spriteID, sb);

                // 頂点＋ウェイト。boneIndex は移植元のローカル添字なので名前でこちらの並びに読み替える
                int vcount = p.verts.Length / 2;
                var verts = new Vertex2DMetaData[vcount];

                for (int i = 0; i < vcount; i++)
                {
                    BoneWeight bw = new BoneWeight();

                    if (p.weights != null && (i + 1) * 8 <= p.weights.Length)
                    {
                        int b = i * 8;
                        bw.weight0 = p.weights[b + 0];
                        bw.weight1 = p.weights[b + 1];
                        bw.weight2 = p.weights[b + 2];
                        bw.weight3 = p.weights[b + 3];
                        bw.boneIndex0 = Remap(p, (int)p.weights[b + 4], boneIndexByName);
                        bw.boneIndex1 = Remap(p, (int)p.weights[b + 5], boneIndexByName);
                        bw.boneIndex2 = Remap(p, (int)p.weights[b + 6], boneIndexByName);
                        bw.boneIndex3 = Remap(p, (int)p.weights[b + 7], boneIndexByName);
                    }

                    // 移植元にボーンが割り当たっていない頂点があるので、
                    // 合計 1 に正規化する。全部 0 のときはこのパーツの先頭ボーンに固定する
                    // （そのままだと Unity が「ウェイトの合計が 0」と警告し、描画が崩れる）
                    float sum = bw.weight0 + bw.weight1 + bw.weight2 + bw.weight3;

                    if (sum <= 0.0001f)
                    {
                        bw.weight0 = 1f; bw.weight1 = 0f; bw.weight2 = 0f; bw.weight3 = 0f;
                        bw.boneIndex0 = Remap(p, 0, boneIndexByName);
                        bw.boneIndex1 = bw.boneIndex2 = bw.boneIndex3 = 0;
                    }
                    else if (Mathf.Abs(sum - 1f) > 0.0001f)
                    {
                        bw.weight0 /= sum; bw.weight1 /= sum; bw.weight2 /= sum; bw.weight3 /= sum;
                    }

                    verts[i] = new Vertex2DMetaData
                    {
                        position = new Vector2(p.verts[i * 2] - sr.rect.x, p.verts[i * 2 + 1] - sr.rect.y),
                        boneWeight = bw,
                    };
                }

                meshProvider.SetVertices(sr.spriteID, verts);
                meshProvider.SetIndices(sr.spriteID, p.tris ?? new int[0]);
                meshProvider.SetEdges(sr.spriteID, new Vector2Int[0]);
            }

            provider.Apply();
            AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);

            // 3) シーン側にパーツを並べる
            Transform meshTf = rig.transform.Find("mesh");

            if (meshTf != null) Object.DestroyImmediate(meshTf.gameObject);

            meshTf = new GameObject("mesh").transform;
            meshTf.SetParent(rig.transform, false);

            Object[] all = AssetDatabase.LoadAllAssetsAtPath(texPath);
            int placed = 0;

            foreach ((PartData p, SpriteRect sr) in made)
            {
                Sprite sprite = null;

                foreach (Object o in all)
                {
                    if (o is Sprite s && s.name == sr.name) { sprite = s; break; }
                }

                if (sprite == null)
                {
                    Debug.LogWarning($"[CharaPartsPorter] スプライトを読めませんでした: {sr.name}");
                    continue;
                }

                GameObject go = new GameObject(p.obj);
                go.transform.SetParent(meshTf, false);
                go.transform.localPosition = new Vector3(p.posX, p.posY, 0f);

                SpriteRenderer r = go.AddComponent<SpriteRenderer>();
                r.sprite = sprite;
                r.sortingOrder = p.order;

                SpriteSkin skin = go.AddComponent<SpriteSkin>();
                SerializedObject so = new SerializedObject(skin);
                so.FindProperty("m_RootBone").objectReferenceValue = bones.Count > 0 ? bones[0] : null;

                SerializedProperty bp = so.FindProperty("m_BoneTransforms");
                bp.arraySize = bones.Count;

                for (int i = 0; i < bones.Count; i++) bp.GetArrayElementAtIndex(i).objectReferenceValue = bones[i];

                so.ApplyModifiedProperties();
                placed++;
            }

            Selection.activeGameObject = rig;
            Debug.Log($"[CharaPartsPorter] {chara}: パーツ {placed}/{made.Count} 枚を配置しました（ボーン {bones.Count} 本）。");
        }

        /// <summary>移植元のローカル boneIndex を、こちらの全体ボーン並びの添字に読み替える。</summary>
        static int Remap(PartData p, int localIndex, Dictionary<string, int> byName)
        {
            if (localIndex < 0 || p.bones == null || localIndex >= p.bones.Length) return 0;

            return byName.TryGetValue(p.bones[localIndex], out int g) ? g : 0;
        }

        static void CollectBones(Transform boneRoot, List<Transform> result)
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
