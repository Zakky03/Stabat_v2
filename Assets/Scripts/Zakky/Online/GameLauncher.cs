using Fusion;
using Fusion.Sockets;
using Koitan;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        // Retry (see ResultManager.OnRetryClicked()) calls runner.LoadScene() on the still-running
        // runner from before, which reloads this scene and recreates this GameLauncher from
        // scratch — reuse the existing runner instead of instantiating and starting a second one
        // alongside it, which would try to open a second simultaneous Photon connection.
        NetworkRunner existingRunner = FindAnyObjectByType<NetworkRunner>();

        if (existingRunner != null && existingRunner.IsRunning)
        {
            runner = existingRunner;
        }
        else
        {
            runner = Instantiate(networkRunnerPrefab);
            DontDestroyOnLoad(runner.gameObject);

            runner.ProvideInput = true;

            var sceneManager = runner.GetComponent<NetworkSceneManagerDefault>();
            if (sceneManager == null)
                sceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            var result = await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SceneManager = sceneManager,
                PlayerCount = BattleGlobal.MaxPlayerNum
            });

            Debug.Log($"[GameLauncher] StartGame result={result.Ok}, reason={result.ShutdownReason}");

            if (!result.Ok)
                return;
        }

        // The pre-reload GameLauncher (if any) was just destroyed along with the rest of the old
        // scene, so this instance needs to register itself as the one Fusion routes callbacks to.
        runner.AddCallbacks(this);

        if (runner.IsSceneAuthority)
        {
            Debug.Log($"[GameLauncher] LoadScene index={onlineBattleSceneBuildIndex}");
            runner.LoadScene(SceneRef.FromIndex(onlineBattleSceneBuildIndex), LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        if (runner != null)
            runner.RemoveCallbacks(this);
    }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[GameLauncher] OnSceneLoadDone");

        // Must happen here, not right after StartGame(): runner.LoadScene() reloads the whole
        // scene (GameLauncher lives inside it), which destroys whatever BattleManager existed
        // before the reload. This callback only fires once the reload has actually settled, so
        // BattleManager.instance here is guaranteed to be the surviving one.
        BattleManager.instance.SetRunner(runner);

        PlayerRef player = runner.LocalPlayer;

        // PlayerRef.PlayerId is Photon's raw ActorNumber: it keeps climbing with every join this
        // room has ever seen (across the room's whole lifetime), it does not reflect how many
        // players are *currently* connected. Using it directly as a seat index meant a handful of
        // repeated Play/Stop cycles against the same lingering room could push it past
        // MaxPlayerNum even with only one real player connected, tripping the room-full guard below
        // and silently spawning no avatar at all. Rank among currently-active players instead.
        int avatarIndex = 0;
        foreach (PlayerRef activePlayer in runner.ActivePlayers.OrderBy(p => p.PlayerId))
        {
            if (activePlayer == player)
                break;

            avatarIndex++;
        }

        if (avatarIndex < 0 || avatarIndex >= BattleGlobal.MaxPlayerNum)
        {
            Debug.LogWarning($"[GameLauncher] Room full, not spawning avatar. avatarIndex={avatarIndex}");
            return;
        }

        if (playerSpawned)
            return;

        StartCoroutine(SpawnWhenRoomReady(avatarIndex));
    }

    // Joining a room whose match already finished for its other players (see
    // BattleManager.HasAnyPlayerFinishedMatch()) must not spawn an avatar straight away: that
    // match's own end-of-round flow left the scene locally on every other client via a plain,
    // non-Fusion SceneManager.LoadScene("Result"), so this room's simulation is effectively
    // abandoned from their side even though Fusion itself has no idea. Wait instead, so the
    // joining client doesn't spawn into a scene nobody else is actually watching anymore.
    private IEnumerator SpawnWhenRoomReady(int avatarIndex)
    {
        // Give newly-replicated avatars (and their HasFinishedMatch state) a moment to arrive
        // before the first check, rather than trusting whatever's visible on the very first frame.
        yield return new WaitForSeconds(0.3f);

        bool waited = false;

        while (BattleManager.HasAnyPlayerFinishedMatch())
        {
            waited = true;
            BattleManager.WaitingForMatchEnd = true;
            BattleManager.instance.ShowWaitingForMatchEndMessage();
            yield return null;
        }

        if (waited)
            BattleManager.WaitingForMatchEnd = false;

        if (playerSpawned)
            yield break;

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
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
}