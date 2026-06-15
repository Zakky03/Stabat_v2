using UnityEngine;
using UnityEngine.SceneManagement;

namespace KoitanLib
{
    /// <summary>
    /// Awake前にManagerSceneを自動でロードするクラス
    /// </summary>
    public class ManagerSceneAutoLoader
    {

        //ゲーム開始時(シーン読み込み前)に実行される
        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadManagerScene()
        {
#if KOITAN_DEBUG
            //DebugSceneを最初に読み込む
            string debugSceneName = "DebugScene";
            if (!SceneManager.GetSceneByName(debugSceneName).IsValid())
            {
                SceneManager.LoadScene(debugSceneName, LoadSceneMode.Additive);
            }
#endif

#if KOITAN_MANAGER
            //ManagerSceneが有効でない時(まだ読み込んでいない時)だけ追加ロードするように
            string managerSceneName = "ManagerScene";
            if (!SceneManager.GetSceneByName(managerSceneName).IsValid())
            {
                SceneManager.LoadScene(managerSceneName, LoadSceneMode.Additive);
            }
#endif
        }
    }
}
