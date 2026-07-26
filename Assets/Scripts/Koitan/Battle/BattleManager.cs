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
        TextMeshProUGUI readyStatusText;
        [SerializeField]
        TMP_InputField usernameInputField;
        [SerializeField]
        bool isOnlineBattle;
        NetworkRunner runner;
        bool hagimariStarted;
        BattleProgress battleProgress = BattleProgress.BeforeBattle;
        // Lets PlayerAvatar block movement/actions on its own avatar until the battle has actually
        // started, without needing the private BattleProgress enum exposed outside this class.
        public static bool IsBattleStarted => instance != null && instance.battleProgress == BattleProgress.Battle;
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
            // Online: moneyUis visibility is recomputed from onlinePlayers every frame in Update()
            // instead, since scene objects default to active and no single one-time toggle here
            // could reliably account for avatars that spawn (or leave) after this point.

            // Online: hagimariText is the "3, 2, 1, Go!" countdown banner played once everyone is
            // ready (see HagimariAnim()) — it must stay hidden during the waiting phase itself,
            // which uses the separate readyStatusText instead.
            if (isOnlineBattle)
            {
                if (readyStatusText != null)
                    readyStatusText.gameObject.SetActive(true);

                if (usernameInputField != null)
                {
                    usernameInputField.gameObject.SetActive(true);
                    usernameInputField.onEndEdit.AddListener(OnUsernameEndEdit);
                }
            }
            else
            {
                StartCoroutine(HagimariAnim());
            }
        }

        // Only ever applies to this client's own avatar — every other client's copy of that
        // avatar already gets the name via normal Networked-property replication.
        void OnUsernameEndEdit(string value)
        {
            PlayerAvatar localAvatar = onlinePlayers.Find(a => a.HasInputAuthority);

            if (localAvatar != null)
                localAvatar.SetUsername(value);
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
                // Recomputed from onlinePlayers every frame (not just toggled once from
                // Spawned()/Despawned()) so a slot's visibility can never end up depending on
                // whether Start()'s initial "hide all" happened to run before or after a given
                // avatar's Spawned() on this particular client — which previously showed every
                // slot correctly on one side but only the local one on a client that joined later.
                bool[] hasPlayer = new bool[BattleGlobal.MaxPlayerNum];

                for (int i = 0; i < onlinePlayers.Count; i++)
                {
                    PlayerAvatar avatar = onlinePlayers[i];
                    hasPlayer[avatar.PlayerIndex] = true;

                    TextMeshProUGUI text = moneyTexts[avatar.PlayerIndex];
                    // Keep the string short (auto-sizing only has so much room to shrink into) —
                    // mark "yours" with color instead of appending text like "(YOU)".
                    string label = avatar.Username.Length > 0 ? avatar.Username.Value : $"P{avatar.PlayerIndex + 1}";
                    text.text = $"{label}: {avatar.Money}G";
                    text.color = avatar.HasInputAuthority ? Color.yellow : Color.white;
                }

                for (int i = 0; i < BattleGlobal.MaxPlayerNum; i++)
                {
                    moneyUis[i].SetActive(hasPlayer[i]);
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

            // Items (bombs) must only spawn once the battle has actually started — not during the
            // online ready/waiting lobby, which (unlike offline's fixed ~4s intro) can last however
            // long it takes everyone to press Ready.
            if (battleProgress == BattleProgress.Battle)
            {
                itemCreateTime += Time.deltaTime;
                if (itemCreateTime > intervalTime)
                {
                    itemCreateTime = 0;
                    CreateItem();
                }
            }

        }

        // Set by GameLauncher while it's deliberately holding off spawning the local avatar
        // (room already finished its match — see HasFinishedMatch) so it can own readyStatusText
        // itself without UpdateReadyStatus() fighting it for the same label every frame.
        public static bool WaitingForMatchEnd;

        // A newly-joining client's own BattleManager always starts fresh at BeforeBattle, so it
        // can't tell from its own state whether the room it just connected to already played out
        // a match — it has to ask the avatars that replicated in from other, longer-connected
        // clients instead.
        public static bool HasAnyPlayerFinishedMatch()
        {
            if (instance == null)
                return false;

            for (int i = 0; i < instance.onlinePlayers.Count; i++)
            {
                if (instance.onlinePlayers[i].HasFinishedMatch)
                    return true;
            }

            return false;
        }

        public void ShowWaitingForMatchEndMessage()
        {
            if (readyStatusText == null)
                return;

            readyStatusText.gameObject.SetActive(true);
            readyStatusText.text = "他のプレイヤーの対戦が終わるのを待っています...";
        }

        void UpdateReadyStatus()
        {
            if (WaitingForMatchEnd)
                return;

            int readyCount = 0;
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                if (onlinePlayers[i].IsReady)
                    readyCount++;
            }

            int waitingCount = onlinePlayers.Count - readyCount;

            if (readyStatusText != null)
                readyStatusText.text = $"準備中: {waitingCount}人 ({readyCount}/{onlinePlayers.Count})\n(Press Start)";

            if (onlinePlayers.Count > 0 && readyCount == onlinePlayers.Count)
            {
                hagimariStarted = true;
                if (readyStatusText != null)
                    readyStatusText.gameObject.SetActive(false);
                if (usernameInputField != null)
                    usernameInputField.gameObject.SetActive(false);
                StartCoroutine(HagimariAnim());
            }
        }

        void CreateItem()
        {
            if (isOnlineBattle)
            {
                if (runner == null || !runner.IsSceneAuthority)
                    return;
            }

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
                        {
                            runner.Spawn(networkItem, pos, Quaternion.identity);
                        }
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

            // Flag this client's own avatar so a client that joins the room after this point (see
            // HasAnyPlayerFinishedMatch()) knows the match already ended here, instead of spawning
            // into a scene everyone else has already locally left for Result.
            if (isOnlineBattle)
            {
                PlayerAvatar localAvatar = onlinePlayers.Find(a => a.HasInputAuthority);
                if (localAvatar != null)
                    localAvatar.HasFinishedMatch = true;
            }

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

                // Properly remove this client's own avatar from the Fusion session before leaving,
                // instead of letting the upcoming scene unload silently destroy it outside Fusion's
                // bookkeeping. Without this, other clients (including a new joiner checking
                // HasAnyPlayerFinishedMatch()) would keep a stale copy that never actually goes
                // away, so the "waiting for match to end" gate would never clear.
                PlayerAvatar localAvatar = onlinePlayers.Find(a => a.HasInputAuthority);
                if (localAvatar != null && runner != null)
                    runner.Despawn(localAvatar.Object);
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
