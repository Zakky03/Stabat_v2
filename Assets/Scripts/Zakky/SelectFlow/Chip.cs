using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Koitan;
using UnityEngine.SceneManagement;

public class Chip : MonoBehaviour
{
    // Start is called before the first frame update
    
    [SerializeField]
    CursorHand cursorHand;
    [SerializeField]
    StageSelectHand stageSelectHand;
    [SerializeField]
    PlayerController playerController;
    [SerializeField]
    SpriteRenderer backGround;
    public CursorHand CursorHand { get; private set; }
    public StageSelectHand StageSelectHand { get; private set; }
    public bool IsDecided { get; private set; }
    public bool hadCoin;

    Vector3 iniScale;
    CircleCollider2D circleCollider2D;

    static List<Chip> AllChips = new List<Chip>();
    public static bool CanStartBattle()
    {
        foreach(Chip c in AllChips)
        {
            //NoneまたはkがDicidedである
            if (BattleSetting.ControllPlayers[c.CursorHand.ID] != (int)CursorHand.PlayerKind.None && !c.IsDecided) return false;
        }
        return true;
    }

    public static void DeleteChipsList()
    {
        AllChips.Clear();
    }

    void Start()
    {
        AllChips.Add(this);
        CursorHand = cursorHand;
        StageSelectHand = stageSelectHand;
        iniScale = transform.localScale;
        circleCollider2D = GetComponent<CircleCollider2D>();
    }

    // Update is called once per frame
    void Update()
    {
        IsCollision();

        circleCollider2D.isTrigger = hadCoin;

        Scaler();
    }

    void IsCollision()
    {
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, circleCollider2D.radius);

        IsDecided = false;

        foreach (Collider2D col in collisions)
        {

            //アイコンに触れてるなら，さらにカーソルおいてるなら
            if (col.tag == "CharaIcon" && !cursorHand.Havecoin)
            {
                //キャラクター変える
                int tmp = col.GetComponent<CharaIcon>().CharaID;
                //表示されたキャラの色変える
                if (playerController != null) playerController.ChangeColor(tmp, tmp);
                //内部での色も変える
                BattleSetting.charaColorIndexes[CursorHand.ID] = tmp;

                IsDecided = true;
            }
            else if (col.tag == "StageIcon" && !stageSelectHand.Havecoin)
            {
                DeleteChipsList();

                BattleSetting.battleStageIndex = col.GetComponent<CharaIcon>().CharaID;

                Debug.Log(BattleSetting.battleStageIndex);

                SceneManager.LoadScene("ZakkyScene");

                //IsDecided = true;
            }
            if (col.tag == "StageIcon")
            {
                backGround.sprite = col.transform.GetChild(0).transform.GetChild(0).GetComponent<SpriteRenderer>().sprite;
            }
        }
    }

    void Scaler()
    {
        if (hadCoin) transform.DOScale(1.3f, 0.1f);
        else transform.DOScale(1f, 0.1f);
    }
}
