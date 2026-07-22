using Fusion;
using UnityEngine;

public enum PlayerButtons
{
    Action = 0, // A
    Jump = 1,   // B
    Ready = 2,  // Start
}

public struct NetworkInputData : INetworkInput
{
    public Vector2 Stick;
    public NetworkButtons Buttons;
}