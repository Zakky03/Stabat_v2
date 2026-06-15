using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;

namespace KoitanLib
{
    public class KoitanDebug : MonoBehaviour
    {
        private static KoitanDebug instance;
        [SerializeField]
        private GameObject debugCanvas;
        [SerializeField]
        private DebugTextManager dtm;
        // Start is called before the first frame update
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);
                DontDestroyOnLoad(debugCanvas);
                DontDestroyOnLoad(dtm);
            }
            else
            {
                Destroy(this);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                debugCanvas.SetActive(!debugCanvas.activeSelf);
            }
        }

        [Conditional("KOITAN_DEBUG")]
        public static void Display(string str)
        {
            //instance.dtm.Display(str);
        }

        [Conditional("KOITAN_DEBUG")]
        public static void DisplayBox(string str, MonoBehaviour mono)
        {
            instance.dtm.DisplayBox(str, mono);
        }
    }

}