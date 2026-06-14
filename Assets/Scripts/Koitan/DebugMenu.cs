using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KoitanLib;
using Koitan;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace KoitanLib
{
    public class DebugMenu : MonoBehaviour
    {
        //メニューを開いているか
        bool isOpen = false;
        //メニューの基本構成
        //文章
        Func<string>[] statements = new Func<string>[16];
        Func<string>[] infoMessages = new Func<string>[16];
        //アクション
        Action[] acts = new Action[16];
        int maxIndex = 16;
        int currentIndex = 0;
        Action currentAct;
        string currentMenuName;
        //履歴
        List<string> historyStrs = new List<string>();
        string historyStatement;
        Stack<Action> historyActs = new Stack<Action>();
        Stack<int> historyIndexes = new Stack<int>();

        FPSCounter fpsCounter;
        bool isShowFPS;
        int[,] resolutions = {
            {640,360},
            {720,405},
            {960,540},
            {1280,720},
            {1440,810},
            {1920,1080},
        };
        int resIndex = 3;

        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(this);
            TryGetComponent(out fpsCounter);
            isShowFPS = true;
            Screen.SetResolution(resolutions[resIndex, 0], resolutions[resIndex, 1], Screen.fullScreen);
        }

        // Update is called once per frame
        void Update()
        {
            //常時表示
            if (isShowFPS)
            {
                KoitanDebug.Display($"FPS {fpsCounter.fps}\n");
            }

            // 倍速
            if (Input.GetKey(KeyCode.I))
            {
                Time.timeScale = 10f;
            }
            else
            {
                Time.timeScale = 1f;
            }

            if (Input.GetKeyDown(KeyCode.Tab) || KoitanInput.GetDown(ButtonCode.Select))
            {
                if (isOpen)
                {
                    isOpen = false;
                }
                else
                {
                    isOpen = true;
                    //OpenRootPage(MainMenu);
                    if (historyActs.Count == 0)
                    {
                        EnterPage(MainMenu);
                    }
                }
            }

            if (!isOpen)
            {
                return;
            }

            //カーソル移動
            if (Input.GetKeyDown(KeyCode.UpArrow) || KoitanInput.GetDown(ButtonCode.Up))
            {
                currentIndex = (currentIndex - 1 + maxIndex) % maxIndex;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || KoitanInput.GetDown(ButtonCode.Down))
            {
                currentIndex = (currentIndex + 1 + maxIndex) % maxIndex;
            }
            //キャンセル
            if (Input.GetKeyDown(KeyCode.X) || KoitanInput.GetDown(ButtonCode.B))
            {
                if (historyActs.Count > 1)
                {
                    historyStrs.RemoveAt(historyStrs.Count - 1);
                    historyStatement = String.Join(" > ", historyStrs);
                    //現在のページの履歴を消す
                    historyActs.Pop();
                    currentAct = historyActs.Peek();
                    currentAct();
                    currentIndex = historyIndexes.Pop();
                }
                else
                {
                    //CloseMenu
                    isOpen = false;
                }
            }
            else
            {
                //実行　
                acts[currentIndex]();
            }

            //描画            
            if (historyStrs.Count > 0)
            {
                //毎フレーム計算するのはよくない
                //KoitanDebug.Display($"{historyStatement}\n");
                KoitanDebug.Display($"--- {currentMenuName} ---\n");
            }
            else
            {
                KoitanDebug.Display($"{currentMenuName}\n");
            }
            for (int i = 0; i < 8; i++)
            {
                if (i == currentIndex)
                {
                    KoitanDebug.Display($"●{statements[i]()}\n");
                }
                else if (i < maxIndex)
                {
                    KoitanDebug.Display($"　{statements[i]()}\n");
                }
                else
                {
                    KoitanDebug.Display($"\n");
                }
            }
            KoitanDebug.Display($"INFO...{infoMessages[currentIndex]()}\n");
        }

        /// <summary>
        /// 最初の画面
        /// </summary>
        void MainMenu()
        {
            currentMenuName = "デバッグメニュー";
            maxIndex = 5;
            statements[0] = () => $"サウンド";
            statements[1] = () => $"ディスプレイ";
            statements[2] = () => $"常時表示情報";
            statements[3] = () => $"バトル";
            statements[4] = () => $"コントローラー設定";
            acts[0] = ButtonDownAct(SoundMenu);
            acts[1] = ButtonDownAct(VideoMenu);
            acts[2] = ButtonDownAct(DisplayMenu);
            acts[3] = ButtonDownAct(BattleMenu);
            acts[4] = ButtonDownAct(ControllerMenu);
            infoMessages[0] = () => $"サウンド設定を開きます";
            infoMessages[1] = () => $"ディスプレイ設定を開きます";
            infoMessages[2] = () => $"常時表示情報設定を開きます";
            infoMessages[3] = () => $"バトル設定を開きます";
            infoMessages[4] = () => $"コントローラー設定を開きます";
            //追加できないように
            PlayerInputManager.instance.DisableJoining();
        }


        void SoundMenu()
        {
            currentMenuName = "SoundMenu";
            maxIndex = 3;
            statements[0] = () => $"全体 < 8 >";
            statements[1] = () => $"BGM < 8 >";
            statements[2] = () => $"SE < 8 >";
            acts[0] = NoneAction;
            acts[1] = ButtonDownAct(NoneAction);
            acts[2] = ButtonDownAct(NoneAction);
            infoMessages[0] = () => $"全体の音量を調整できます";
            infoMessages[1] = () => $"BGMの音量を調整できます";
            infoMessages[2] = () => $"SEの音量を調整できます";
        }

        void VideoMenu()
        {
            currentMenuName = "ディスプレイ設定";
            maxIndex = 4;
            statements[0] = () => $"フルスクリーン < {Screen.fullScreen} >";
            statements[1] = () => $"解像度 < {resolutions[resIndex, 0]} x {resolutions[resIndex, 1]} >";
            statements[2] = () => $"VSync < {QualitySettings.vSyncCount} >";
            statements[3] = () => $"ポストエフェクト < ON >";
            acts[0] = SelectFullScreen;
            acts[1] = SelectResolution;
            acts[2] = SelectVSync;
            acts[3] = ButtonDownAct(NoneAction);
            infoMessages[0] = () => $"フルスクリーンの設定";
            infoMessages[1] = () => $"解像度を変更できます";
            infoMessages[2] = () => $"Vsyncを変更できます";
            infoMessages[3] = () => $"ポストエフェクト";
        }

        void DisplayMenu()
        {
            currentMenuName = "DisplayMenu";
            maxIndex = 1;
            statements[0] = () => $"Show FPS < {(isShowFPS ? "ON" : "OFF")} >";
            acts[0] = () => SelectBool(ref isShowFPS);
            infoMessages[0] = () => $"FPSを常時表示できます";
        }

        void BattleMenu()
        {
            currentMenuName = "バトルメニュー";
            maxIndex = 4 + BattleSetting.playerCount;
            statements[0] = () => $"バトル開始";
            statements[1] = () => $"ステージ < {BattleSetting.battleStageIndex} >";
            statements[2] = () => $"人数 < {BattleSetting.playerCount} >";
            statements[3] = () => $"自動設定";
            acts[0] = ButtonDownActNoHistory(BattleStart);
            acts[1] = () =>
            {
                SelectInt(ref BattleSetting.battleStageIndex, 0, BattleGlobal.stageSceneNames.Length - 1);
                maxIndex = 4 + BattleSetting.playerCount;
            };
            acts[2] = () =>
            {
                SelectInt(ref BattleSetting.playerCount, 2, BattleGlobal.MaxPlayerNum);
                maxIndex = 4 + BattleSetting.playerCount;
            };
            acts[3] = ButtonDownAct(BattleAutoSetting);
            infoMessages[0] = () => $"バトルを開始します";
            infoMessages[1] = () => $"ステージを設定します";
            infoMessages[2] = () => $"人数を設定します";
            infoMessages[3] = () => $"現在のコントローラーの人間設定、残りをCPUで埋めます";

            for (int i = 0; i < BattleGlobal.MaxPlayerNum; i++)
            {
                int tmpI = i;
                statements[i + 4] = () => $"Player{tmpI}, {(ControllPlayer)BattleSetting.ControllPlayers[tmpI]}, {BattleSetting.teamColorIndexes[tmpI]}, {BattleSetting.playerIndexes[tmpI]}, {BattleSetting.charaColorIndexes[tmpI]}";
                acts[i + 4] = ButtonDownAct(PlayerSetting(tmpI));
                infoMessages[i + 4] = () => $"Player{tmpI}の設定を変更します";
            }
        }

        void ControllerMenu()
        {
            currentMenuName = "コントローラー設定";
            maxIndex = 1;
            statements[0] = () => $"コントローラーをつなぎ直す";
            acts[0] = ButtonDownActNoHistory(KoitanInput.ClearAllController);
            infoMessages[0] = () => $"この画面でボタンを押すとコントローラーの登録ができます";
            //コントローラーの受付
            PlayerInputManager.instance.EnableJoining();
        }

        Action PlayerSetting(int index)
        {
            return () =>
            {
                currentMenuName = $"Player{index}Setting";
                maxIndex = 4;
                statements[0] = () => $"操作者 < {(ControllPlayer)BattleSetting.ControllPlayers[index]} >";
                statements[1] = () => $"チーム < {BattleSetting.teamColorIndexes[index]} >";
                statements[2] = () => $"プレイヤー番号 < {BattleSetting.playerIndexes[index]} >";
                statements[3] = () => $"キャラカラー < {BattleSetting.charaColorIndexes[index]} >";
                acts[0] = () => SelectInt(ref BattleSetting.ControllPlayers[index], 0, 2);
                acts[1] = () => SelectInt(ref BattleSetting.teamColorIndexes[index], 0, BattleGlobal.MaxPlayerNum - 1);
                acts[2] = () => SelectInt(ref BattleSetting.playerIndexes[index], 0, BattleGlobal.MaxPlayerNum - 1);
                acts[3] = () => SelectInt(ref BattleSetting.charaColorIndexes[index], 0, BattleGlobal.MaxPlayerNum - 1);
                infoMessages[0] = () => $"操作する人を変更できます";
                infoMessages[1] = () => $"チームを変更できます";
                infoMessages[2] = () => $"プレイヤー番号を変更できます";
                infoMessages[3] = () => $"キャラカラーを変更できます";
            };
        }

        Action ButtonDownAct(Action act)
        {
            return () =>
            {
                if (Input.GetKeyDown(KeyCode.Z) || KoitanInput.GetDown(ButtonCode.A))
                {
                    EnterPage(act);
                }
            };
        }

        Action ButtonDownActNoHistory(Action act)
        {
            return () =>
            {
                if (Input.GetKeyDown(KeyCode.Z) || KoitanInput.GetDown(ButtonCode.A))
                {
                    act();
                }
            };
        }

        void BattleStart()
        {
            BattleManager.StartBattle();
        }

        void BattleAutoSetting()
        {
            for (int i = 0; i < BattleSetting.playerCount; i++)
            {
                if (i < KoitanInput.GetControllerNum())
                {
                    BattleSetting.ControllPlayers[i] = 1;
                }
                else
                {
                    BattleSetting.ControllPlayers[i] = 2;
                }
                BattleSetting.playerIndexes[i] = i;
                BattleSetting.teamColorIndexes[i] = i;
                BattleSetting.charaColorIndexes[i] = i;
            }
        }

        void SelectInt(ref int i, int min, int max)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || KoitanInput.GetDown(ButtonCode.Left))
            {
                i--;
                if (i < min)
                {
                    i = max;
                }
            }
            if (Input.GetKeyDown(KeyCode.RightArrow) || KoitanInput.GetDown(ButtonCode.Right))
            {
                i++;
                if (i > max)
                {
                    i = min;
                }
            }
        }

        void SelectBool(ref bool b)
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || KoitanInput.GetDown(ButtonCode.Left) || KoitanInput.GetDown(ButtonCode.Right))
            {
                b = !b;
            }
        }


        void EnterPage(Action act)
        {
            currentAct = act;
            act();
            //記憶しておく
            historyStrs.Add(currentMenuName);
            historyActs.Push(currentAct);
            historyIndexes.Push(currentIndex);
            //更新
            currentIndex = 0;
            historyStatement = String.Join(" > ", historyStrs);
        }

        void SelectFullScreen()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || KoitanInput.GetDown(ButtonCode.Left) || KoitanInput.GetDown(ButtonCode.Right))
            {
                Screen.fullScreen = !Screen.fullScreen;
            }
        }

        void SelectResolution()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow) || KoitanInput.GetDown(ButtonCode.Left))
            {
                resIndex--;
                if (resIndex < 0)
                {
                    resIndex = 5;
                }
                Screen.SetResolution(resolutions[resIndex, 0], resolutions[resIndex, 1], Screen.fullScreen);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || KoitanInput.GetDown(ButtonCode.Right))
            {
                resIndex++;
                if (resIndex > 5)
                {
                    resIndex = 0;
                }
                Screen.SetResolution(resolutions[resIndex, 0], resolutions[resIndex, 1], Screen.fullScreen);

            }
        }

        void SelectVSync()
        {
            int vsync = QualitySettings.vSyncCount;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || KoitanInput.GetDown(ButtonCode.Left))
            {
                vsync--;
                if (vsync < 0)
                {
                    vsync = 3;
                }
                QualitySettings.vSyncCount = vsync;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || KoitanInput.GetDown(ButtonCode.Right))
            {
                vsync++;
                if (vsync > 3)
                {
                    vsync = 0;
                }
                QualitySettings.vSyncCount = vsync;
            }
        }

        void NoneAction()
        {

        }

        void CancelPage(Action act)
        {

        }

        string boolToStr(bool b)
        {
            return b ? "ON" : "OFF";
        }

        void OpenRootPage(Action act)
        {
            currentIndex = 0;
            currentAct = act;
            act();
        }
    }
}
