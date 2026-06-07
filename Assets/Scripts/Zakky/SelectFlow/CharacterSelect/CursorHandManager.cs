using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class CursorHandManager : MonoBehaviour
{
    private static List<CursorHand> cursorHandList = new List<CursorHand>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public static void RegisterCursorHand(CursorHand cursorHand)
    {
        cursorHandList.Add(cursorHand);
    }

    public static List<CursorHand> GetCursorHandList()
    {
        return cursorHandList;
    }
}
