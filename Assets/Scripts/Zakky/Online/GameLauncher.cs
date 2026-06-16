using Fusion;
using Fusion.Sockets;
using Koitan;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private NetworkPrefabRef playerAvatarPrefab;
    [SerializeField] private int onlineBattleSceneBuildIndex = 0;

    private NetworkRunner runner;
    private bool playerSpawned;

    private async void Start()
    {
        runner = Instantiate(networkRunnerPrefab);
        DontDestroyOnLoad(runner.gameObject);

        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        var sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SceneManager = sceneManager
        });

        Debug.Log($"[GameLauncher] StartGame result={result.Ok}, reason={result.ShutdownReason}");

        if (!result.Ok)
            return;

        if (runner.IsSceneAuthority)
        {
            Debug.Log($"[GameLauncher] LoadScene index={onlineBattleSceneBuildIndex}");
            runner.LoadScene(SceneRef.FromIndex(onlineBattleSceneBuildIndex), LoadSceneMode.Single);
        }
    }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[GameLauncher] OnSceneLoadDone");

        PlayerRef player = runner.LocalPlayer;

        int avatarIndex = player.PlayerId - 1;

        if (playerSpawned)
            return;

        playerSpawned = true;

        var spawnPosition = BattleManager.instance.GetInitPosition(avatarIndex).position;

        NetworkObject obj = runner.Spawn(
            playerAvatarPrefab,
            spawnPosition,
            Quaternion.identity,
            inputAuthority: runner.LocalPlayer
        );

        PlayerAvatar avatar = obj.GetComponent<PlayerAvatar>();

        avatar.ChangeColor(avatarIndex, avatarIndex);
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = default;

        if (LocalInputReader.Instance != null)
            data = LocalInputReader.Instance.ConsumeFusionInput();

        input.Set(data);
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        //int avatarIndex = player.PlayerId - 1;
        //
        //Debug.Log($"[Fusion] PlayerJoined player={player}, PlayerId={player.PlayerId}, avatarIndex={avatarIndex}");
        //
        //if (player == runner.LocalPlayer)
        //{
        //    var spawnPosition = BattleManager.instance.GetInitPosition(avatarIndex).position;
        //
        //    NetworkObject obj = runner.Spawn(
        //        playerAvatarPrefab,
        //        spawnPosition,
        //        Quaternion.identity,
        //        inputAuthority: player
        //    );
        //
        //    PlayerAvatar avatar = obj.GetComponent<PlayerAvatar>();
        //    avatar.ChangeColor(avatarIndex, avatarIndex);
        //
        //    Debug.Log($"[Fusion] Spawn Avatar index={avatarIndex}, name={obj.name}");
        //}
    }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
}