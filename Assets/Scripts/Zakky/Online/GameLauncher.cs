using Fusion;
using Fusion.Sockets;
using Koitan;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.Unicode;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField]
    private NetworkRunner networkRunnerPrefab;

    [SerializeField]
    private NetworkPrefabRef playerAvatarPrefab;

    private async void Start()
    {
        var networkRunner = Instantiate(networkRunnerPrefab);

        // GameLauncherを、NetworkRunnerのコールバック対象に追加する
        networkRunner.AddCallbacks(this);

        var result = await networkRunner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared
        });
    }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    // プレイヤーがセッションへ参加した時に呼ばれるコールバック
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // セッションへ参加したプレイヤーが自分自身かどうかを判定する
        if (player == runner.LocalPlayer)
        {
            // アバターの初期位置を計算する（半径5の円の内部のランダムな点）
            //var rand = UnityEngine.Random.insideUnitCircle * 5f;
            //var spawnPosition = new Vector3(rand.x, 2f, rand.y);
            // 自分自身のアバターをスポーンする
            var spawnPosition = BattleManager.instance.GetInitPosition(0).position;
            runner.Spawn(playerAvatarPrefab, spawnPosition, Quaternion.identity, player);
        }
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (LocalInputReader.Instance == null)
            return;

        input.Set(LocalInputReader.Instance.ConsumeFusionInput());
    }

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

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
}