using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koitan
{
    public class Bomb : MonoBehaviour
    {
        Rigidbody2D rb;
        [SerializeField]
        GameObject body;
        [SerializeField]
        GameObject itemArea;
        [SerializeField]
        GameObject eff;
        [SerializeField]
        GameObject explosionArea;
        bool isIgnited;
        bool isThrowed;
        bool isFired;
        public int playerIndex;
        public int teamColorIndex;
        // Start is called before the first frame update
        void Start()
        {
            TryGetComponent(out rb);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public void Pick(Transform handTf, int playerIndex)
        {
            this.playerIndex = playerIndex;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector3.zero;
            transform.SetParent(handTf);
            transform.position = handTf.position;
            isIgnited = true;
            rb.simulated = false;
        }

        public void Throw(Vector3 speed)
        {
            transform.SetParent(null);
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = speed;
            isThrowed = true;
            rb.simulated = true;
        }

        public void Explosion()
        {
            body.SetActive(false);
            itemArea.SetActive(false);
            eff.SetActive(true);
            explosionArea.SetActive(true);
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector3.zero;
            Destroy(gameObject, 1f);
            isFired = true;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isIgnited)
            {
                if (collision.collider.gameObject.tag == "Player" && collision.collider.GetComponent<PlayerController>().playerIndex == playerIndex)
                {
                    return;
                }
                Explosion();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isFired)
            {
                if (collision.tag == "Player")
                {
                    PlayerController playerController = collision.GetComponent<PlayerController>();
                    //Vector2 unitVec = (playerController.transform.position - transform.position).normalized;
                    /*
                    playerController.AddPowerVec(new Vector2(Mathf.Sign(playerController.transform.position.x - transform.position.x), 1) * 50f);
                    playerController.SetInoperableTime(1f);
                    */
                    playerController.SetDamage(new Vector2(Mathf.Sign(playerController.transform.position.x - transform.position.x), 1) * 35f, 1f);
                    return;
                }
                else if (collision.tag == "Shop")
                {
                    collision.transform.parent.GetComponent<ShopController>().BrokenShop();
                }
                else if (collision.tag == "Bomb")
                {
                    if (collision.isTrigger) return;
                    collision.transform.parent.GetComponent<Bomb>().Explosion();
                }
            }
        }
    }
}
