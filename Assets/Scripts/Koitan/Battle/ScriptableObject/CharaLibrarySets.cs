using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Koitan
{
    [CreateAssetMenu(
        fileName = "CharaLibrarySetsData",
        menuName = "ScriptableObject/CharaLibrarySetsData",
        order = 0)
    ]
    public class CharaLibrarySets : ScriptableObject
    {
        public UnityEngine.U2D.Animation.SpriteLibraryAsset[] librarys;
    }
}