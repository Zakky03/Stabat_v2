using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectedCharacter : MonoBehaviour
{
    [SerializeField]
    CursorHand cursorHand;
    public CursorHand CursorHand { get; private set; }
    // Start is called before the first frame update
    void Start()
    {
        CursorHand = cursorHand;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
