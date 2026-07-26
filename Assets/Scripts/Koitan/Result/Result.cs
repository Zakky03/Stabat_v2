using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koitan
{
    public class Result : MonoBehaviour
    {
        public static int playerCount;
        public static int[] playerMoneys = new int[BattleGlobal.MaxPlayerNum];

        // Online only. Every client computes the same projected post-match rating for every
        // player (deterministic given the synced pre-match ratings and money-based ranks), so the
        // Result screen can show everyone's change — even though only the local player's own
        // client actually persists its own value (see BattleManager.OwatiAnim()).
        public static bool hasRatings;
        public static int[] playerRatingsBefore = new int[BattleGlobal.MaxPlayerNum];
        public static int[] playerRatingsAfter = new int[BattleGlobal.MaxPlayerNum];
    }
}
