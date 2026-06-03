using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace Koitan
{
    public class CharaColorChanger : MonoBehaviour
    {
        [SerializeField]
        GameObject mesh;
        [SerializeField]
        CharaLibrarySets charaColorSets;
        [SerializeField]
        ColorSets outlineSets;
        float flashTime = 0f;

        private void Update()
        {
            if (flashTime > 0f)
            {
                flashTime -= Time.deltaTime;
                int flash = (int)(flashTime / 0.1f) % 2;
                mesh.SetActive(flash != 1);
            }
        }

        public void ChangeColor(int charaColorIndex, int outlineColorIndex)
        {
            UnityEngine.U2D.Animation.SpriteLibrary sl = mesh.transform.Find("body").GetComponent<UnityEngine.U2D.Animation.SpriteLibrary>();
            sl.spriteLibraryAsset = charaColorSets.librarys[charaColorIndex];
            foreach (SpriteRenderer sr in mesh.transform.Find("outline").GetComponentsInChildren<SpriteRenderer>())
            {
                sr.color = outlineSets.colors[outlineColorIndex];
            }
        }

        public void SetFlashTime(float time)
        {
            flashTime = time;
        }
    }
}
