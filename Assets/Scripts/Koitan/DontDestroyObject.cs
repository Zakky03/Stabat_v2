using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Koitan
{
    public class DontDestroyObject : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log(
                $"[DDOL BEFORE] name={gameObject.name}, scene={gameObject.scene.name}",
                gameObject
            );

            DontDestroyOnLoad(gameObject);

            Debug.Log(
                $"[DDOL AFTER] name={gameObject.name}, scene={gameObject.scene.name}",
                gameObject
            );
        }

        // Start is called before the first frame update
        void Start()
        {
            DontDestroyOnLoad(this);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
