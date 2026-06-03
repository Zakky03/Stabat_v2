using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KoitanLib;
using DG.Tweening;
using UnityEngine.SceneManagement;

public class StageSelectHand : MonoBehaviour
{
    Rigidbody2D rigidbody2D;
    CircleCollider2D circleCollider2D;

    [SerializeField]
    float cursorVelocity = 100f;
    [SerializeField]
    Chip chip;

    public bool Havecoin { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        circleCollider2D = GetComponent<CircleCollider2D>();

        Havecoin = false;
    }

    // Update is called once per frame
    void Update()
    {
        PutChip();

        Move();
    }

    void Move()
    {
        //�ړ����͂ő��x������
        Vector2 tmpspd = Vector2.zero;
        for (int i = 0; i < 4; i++)
        {
            tmpspd += KoitanInput.GetStick(i) * Time.deltaTime;
            //rigidbody2D.velocity += cursorVelocity * KoitanInput.GetStick(i) * Time.deltaTime;
        }
        tmpspd = new Vector2(Mathf.Clamp(tmpspd.x, -Time.deltaTime, Time.deltaTime),
            Mathf.Clamp(tmpspd.y, -Time.deltaTime, Time.deltaTime));
        rigidbody2D.linearVelocity += cursorVelocity * tmpspd;

        //�R�C�������Ă锻��̂Ƃ�
        if (Havecoin)
        {
            //�w��Ƀ`�b�v�̎��ꏊ���ړ�����D
            Vector3 ofs = circleCollider2D.offset;
            chip.gameObject.transform.DOMove(transform.position + ofs, 0.05f);
        }
    }

    void PutChip()
    {
        Chip chiptmp = IsChipCollision();
        for (int i = 0; i < 4; i++)
        {
            //A�����Ă��͈͓��Ƀ`�b�v������Ƃ�
            if ((KoitanInput.GetDown(ButtonCode.A, i) && chiptmp != null))
            {
                chip = chiptmp;
                //�`�b�v�����u��
                Havecoin = !Havecoin;
                //�J�[�\���̈ړ����~�߂�
                rigidbody2D.linearVelocity = Vector2.zero;
            }
            else if (KoitanInput.GetDown(ButtonCode.B, i))
            {
                //B�������Ƃ��`�b�v����
                Havecoin = true;
            }
            Debug.Log(Havecoin);
        }
        
        chip.hadCoin = Havecoin;
    }
    Chip IsChipCollision()
    {
        Collider2D[] collisions = Physics2D.OverlapCircleAll(transform.position, circleCollider2D.radius);
        Chip chiptmp = null;
        float distance = (float)1e9;

        foreach (Collider2D col in collisions)
        {
            Chip nowchip = col.GetComponent<Chip>();
            //�����̎����Ă�`�b�v���R���s���[�^�̃`�b�v�̂Ƃ�
            if (col.tag == "Chip")
            {
                Vector3 off = circleCollider2D.offset;
                float dis = Vector2.Distance(transform.position + off, col.transform.position);
                //����chip���擾����
                if (distance > dis && !(chip.hadCoin && !nowchip.hadCoin))
                {
                    distance = dis;
                    chiptmp = nowchip;
                }
            }
        }
        return chiptmp;
    }
}