using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KoitanLib;
using Cinemachine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Android;
using Fusion;

namespace Koitan
{
    public class BattleManager : MonoBehaviour
    {
        List<PlayerController> players = new List<PlayerController>();
        List<PlayerAvatar> onlinePlayers = new List<PlayerAvatar>();
        public static List<PlayerController> Players => instance.players;
        public static List<PlayerAvatar> OnlinePlayers => instance.onlinePlayers;
        int[] moneys = new int[BattleGlobal.MaxPlayerNum];
        public static int[] Moneys => instance.moneys;
        [SerializeField]
        CinemachineTargetGroup targetGroup;
        [SerializeField]
        Money moneyPrefab;
        //���̏��������֗�
        public static CinemachineTargetGroup TargetGroup => instance.targetGroup;
        public static BattleManager instance { private set; get; }
        [SerializeField]
        ColorSets colorSets;
        public static ColorSets ColorSets => instance.colorSets;
        [SerializeField]
        PlayerController charaPrefab;
        [SerializeField]
        ControllerInput ai;
        [SerializeField]
        Transform[] initPositions;
        [SerializeField]
        GameObject[] items;
        [SerializeField]
        float stageWidth;
        [SerializeField]
        float stageHeight;
        [SerializeField]
        float intervalTime;
        float itemCreateTime;
        [SerializeField]
        ShopController[] shops;
        [SerializeField]
        TextMeshProUGUI[] moneyTexts;
        [SerializeField]
        GameObject[] moneyUis;
        [SerializeField]
        TextMeshProUGUI timerText;
        [SerializeField]
        float limitSeconds;
        [SerializeField]
        GameObject owariText;
        [SerializeField]
        GameObject hagimariText;
        [SerializeField]
        bool isOnlineBattle;
        NetworkRunner runner;
        TextMeshProUGUI hagimariTMP;
        string hagimariOriginalText;
        bool hagimariStarted;
        BattleProgress battleProgress = BattleProgress.BeforeBattle;
        public static ShopController[] Shops => instance.shops;
        public static List<Money> moneyInstances = new List<Money>();
        // Start is called before the first frame update
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                /*
                for (int i = 0; i < BattleGlobal.MaxPlayerNum; i++)
                {
                    if (BattleSetting.ControllPlayers[i] == 0) continue;
                    PlayerController player = Instantiate(charaPrefab, initPositions[i].position, Quaternion.identity);
                    player.ChangeColor(BattleSetting.playerIndexes[i], BattleSetting.teamColorIndexes[i]);
                    if (BattleSetting.ControllPlayers[i] == 2)
                    {
                        Instantiate(ai);
                    }
                    players.Add(player);
                    targetGroup.AddMember(player.transform, 1f, 3f);

                    moneys[i] = 0;
                }
                */

                /*
                for (int i = 0; i < KoitanInput.GetControllerNum(); i++)
                {
                    PlayerController player = Instantiate(charaPrefab);
                    player.ChangeColor(i, i);
                    players.Add(player.gameObject);
                    targetGroup.AddMember(player.transform, 1f, 3f);
                }
                */
            }
            else
            {
                Destroy(this);
            }
        }

        public Transform GetInitPosition(int index)
        {
            return initPositions[index];
        }

        public void SetRunner(NetworkRunner runner)
        {
            this.runner = runner;
        }

        private void Start()
        {
            // Online: each PlayerAvatar toggles its own moneyUis slot on Spawned/Despawned instead.
            if (!isOnlineBattle)
            {
                for (int i = 0; i < BattleGlobal.MaxPlayerNum; i++)
                {
                    if (i < players.Count)
                    {
                        moneyUis[i].SetActive(true);
                        moneyUis[i].transform.localPosition = new Vector3(1920 / players.Count * (i + 0.5f) - 960, -420);
                    }
                    else
                    {
                        moneyUis[i].SetActive(false);
                    }
                }
            }

            if (isOnlineBattle)
            {
                hagimariTMP = hagimariText.GetComponentInChildren<TextMeshProUGUI>(true);
                if (hagimariTMP != null)
                    hagimariOriginalText = hagimariTMP.text;
                hagimariText.SetActive(true);
            }
            else
            {
                StartCoroutine(HagimariAnim());
            }
        }

        public void SetMoneyUIActive(int index, bool flag)
        {
            moneyUis[index].SetActive(flag);
        }

        private void OnDestroy()
        {
            instance = null;
        }

        // Update is called once per frame
        void Update()
        {
            if (isOnlineBattle && !hagimariStarted && battleProgress == BattleProgress.BeforeBattle)
            {
                UpdateReadyStatus();
            }

            // �^�C�}�[����
            if (battleProgress == BattleProgress.Battle)
            {
                limitSeconds -= Time.deltaTime;
                if (limitSeconds <= 0)
                {
                    limitSeconds = 0;
                    StartCoroutine(OwatiAnim());
                }
                int mm = (int)(limitSeconds / 60);
                int ss = (int)limitSeconds - mm * 60;
                int dd = (int)((limitSeconds - (int)limitSeconds) * 100);
                timerText.text = $"{mm}:{ss:D2}.{dd:D2}";
            }

            if (isOnlineBattle)
            {
                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    PlayerAvatar avatar = onlinePlayers[i];
                    moneyTexts[avatar.PlayerIndex].text = $"{avatar.Money}G";
                }
            }
            else
            {
                for (int i = 0; i < players.Count; i++)
                {
                    moneyTexts[i].text = $"{moneys[i]}G";
                }
            }
            KoitanDebug.Display($"MoneyInstances.Count = {moneyInstances.Count}");
            itemCreateTime += Time.deltaTime;
            if (itemCreateTime > intervalTime)
            {
                itemCreateTime = 0;
                CreateItem();
            }

        }

        void UpdateReadyStatus()
        {
            int readyCount = 0;
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                if (onlinePlayers[i].IsReady)
                    readyCount++;
            }

            if (hagimariTMP != null)
                hagimariTMP.text = $"Ready {readyCount}/{onlinePlayers.Count}\n(Press Start)";

            if (onlinePlayers.Count > 0 && readyCount == onlinePlayers.Count)
            {
                hagimariStarted = true;
                if (hagimariTMP != null)
                    hagimariTMP.text = hagimariOriginalText;
                StartCoroutine(HagimariAnim());
            }
        }

        void CreateItem()
        {
            if (isOnlineBattle && (runner == null || !runner.IsSceneAuthority))
                return;

            GameObject item = items[Random.Range(0, items.Length)];
            //100��őł��؂�
            for (int i = 0; i < 100; i++)
            {
                Vector2 pos = new Vector2(Random.Range(-stageWidth / 2, stageWidth / 2), Random.Range(-stageHeight / 2, stageHeight / 2));
                RaycastHit2D hit;
                hit = Physics2D.BoxCast(pos, Vector2.one, 0, Vector2.zero);
                if (!hit)
                {
                    if (isOnlineBattle)
                    {
                        NetworkObject networkItem = item.GetComponent<NetworkObject>();
                        if (networkItem != null)
                            runner.Spawn(networkItem, pos, Quaternion.identity);
                    }
                    else
                    {
                        Instantiate(item, pos, Quaternion.identity);
                    }
                    break;
                }
            }
        }

        IEnumerator HagimariAnim()
        {
            hagimariText.SetActive(true);
            yield return new WaitForSeconds(4f);
            hagimariText.SetActive(false);
            battleProgress = BattleProgress.Battle;
        }

        IEnumerator OwatiAnim()
        {
            owariText.SetActive(true);
            battleProgress = BattleProgress.AfterBattle;
            yield return new WaitForSeconds(2f);
            // ���U���g�ɏ���n��
            if (isOnlineBattle)
            {
                Result.playerCount = onlinePlayers.Count;
                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    PlayerAvatar avatar = onlinePlayers[i];
                    Result.playerMoneys[avatar.PlayerIndex] = avatar.Money;
                }
            }
            else
            {
                Result.playerCount = players.Count;
                for (int i = 0; i < players.Count; i++)
                {
                    Result.playerMoneys[i] = moneys[i];
                }
            }
            SceneManager.LoadScene("Result");
        }

        /// <summary>
        /// �����𐶐�����
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static Money CreateMoney(Vector3 pos)
        {
            Money tmp = Instantiate(instance.moneyPrefab, pos, Quaternion.identity);
            moneyInstances.Add(tmp);
            return tmp;
        }

        /// <summary>
        /// ������j������
        /// </summary>
        /// <param name="money"></param>
        public static void DestroyMoney(Money money)
        {
            moneyInstances.Remove(money);
        }

        public static void StartBattle()
        {
            KoitanInput.ClearAllCPU();
            SceneManager.LoadScene(BattleGlobal.stageSceneNames[BattleSetting.battleStageIndex]);
        }

        private void OnDrawGizmosSelected()
        {
            GizmosExtensions2D.DrawWireRect2D(Vector3.zero, stageWidth, stageHeight);
        }

        enum BattleProgress
        {
            BeforeBattle,
            Battle,
            AfterBattle
        }
    }
}
