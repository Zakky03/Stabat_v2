using Fusion;
using UnityEngine;

public enum PlayerButtons
{
    Action = 0,  // A
    Jump = 1,    // B
    Ready = 2,   // Start
    Attack1 = 3, // X（近接）
    Attack2 = 4, // Y（飛び道具）
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 Stick;
    public NetworkButtons Buttons;
}