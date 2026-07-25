using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Fusion;

namespace Koitan
{
    public class ResultManager : MonoBehaviour
    {
        [SerializeField]
        TextMeshProUGUI[] moneyTexts;
        [SerializeField]
        TextMeshProUGUI[] rankTexts;
        [SerializeField]
        GameObject[] moneyUis;
        [SerializeField]
        Button retryButton;
        const string OnlineBattleScenePath = "Assets/Scenes/Zakky/OnlineBattleScene.unity";
        int moneyMax = 0;
        List<int[]> sortRank = new List<int[]>();
        // Start is called before the first frame update
        void Start()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(OnRetryClicked);

            //UI�̕\��
            for (int i = 0; i < BattleGlobal.MaxPlayerNum; i++)
            {
                if (i < Result.playerCount)
                {
                    moneyUis[i].SetActive(true);
                    moneyUis[i].transform.localPosition = new Vector3(1920 / Result.playerCount * (i + 0.5f) - 960, -420);
                    moneyTexts[i].text = $"{Result.playerMoneys[i]}G";
                    moneyMax = Mathf.Max(moneyMax, Result.playerMoneys[i]);
                    sortRank.Add(new int[] { i, Result.playerMoneys[i] });
                }
                else
                {
                    moneyUis[i].SetActive(false);
                }
            }
            // �\�[�g
            sortRank.Sort((a, b) => b[1] - a[1]);
            for (int i = 0; i < Result.playerCount; i++)
            {
                rankTexts[sortRank[i][0]].text = $"{i + 1}";
                rankTexts[sortRank[i][0]].transform.localScale = Vector3.zero;
                rankTexts[sortRank[i][0]].transform.DOScale(3f - i * 0.75f, 1).SetDelay(i * 0.25f + 2);
            }
            // ���ʂ̍��W���v�Z����
            // ���ʂ�1�ʂ̐l���
            for (int i = 0; i < Result.playerCount; i++)
            {
                moneyUis[i].transform.DOLocalMoveY((float)Result.playerMoneys[i] / moneyMax * 500f, 2f).SetRelative();
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        void OnRetryClicked()
        {
            NetworkRunner runner = FindAnyObjectByType<NetworkRunner>();

            if (runner != null && runner.IsRunning)
            {
                // Only the scene authority is allowed to move the room to a new scene; a
                // non-authority client's click is a harmless no-op here (the room's actual
                // authority still has to be the one to press Retry).
                if (runner.IsSceneAuthority)
                {
                    int buildIndex = SceneUtility.GetBuildIndexByScenePath(OnlineBattleScenePath);
                    runner.LoadScene(SceneRef.FromIndex(buildIndex), LoadSceneMode.Single);
                }
            }
            else
            {
                BattleManager.StartBattle();
            }
        }
    }
}
