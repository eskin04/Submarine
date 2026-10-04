using System.Collections.Generic;
using PurrNet;
using PurrNet.StateMachine;
using PurrLobby;
using UnityEngine;
using System.Collections;

public class PlayerSpawningState : StateNode
{
    [Header("Prefabs")]
    [SerializeField] private PlayerInventory engineerPrefab;
    [SerializeField] private PlayerInventory technicianPrefab;
    [SerializeField] private PlayerInventory defaultPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform engineerSpawnPoint;
    [SerializeField] private Transform technicianSpawnPoint;
    [SerializeField] private List<Transform> fallbackSpawnPoints = new List<Transform>();

    private const int RequiredPlayerCount = 2;
    private bool _playersSpawned;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;

        TrySpawnPlayers();
    }

    public override void StateUpdate(bool asServer)
    {
        base.StateUpdate(asServer);
        if (!asServer) return;

        TrySpawnPlayers();
    }

    private void TrySpawnPlayers()
    {
        if (_playersSpawned)
            return;

#if UNITY_EDITOR
        if (IsSoloHostEditor())
        {
            if (!TrySpawnSoloHost())
                return;
        }
        else
#endif
        {
            if (networkManager.playerCount < RequiredPlayerCount)
                return;

            // Keep this guard across state re-entry and set it before any spawn callbacks can run.
            _playersSpawned = true;
            SpawnPlayerSimple();
        }
        SetView();
        SetLevelView();
        machine.Next();
    }

#if UNITY_EDITOR
    private static bool IsSoloHostEditor()
    {
        if (!Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor)
            return false;

        foreach (var tag in Unity.Multiplayer.PlayMode.CurrentPlayer.Tags)
        {
            if (tag == "SoloHost")
                return true;
        }
        return false;
    }

    private bool TrySpawnSoloHost()
    {
        if (!networkManager.isLocalPlayerReady)
            return false;

        var hostPlayer = networkManager.localPlayer;
        for (int i = 0; i < networkManager.players.Count; i++)
        {
            if (networkManager.players[i] != hostPlayer)
                continue;

            var role = PlayerRole.Engineer;
            var dataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (dataHolder != null && dataHolder.CurrentLobby.IsValid &&
                dataHolder.CurrentLobby.Members != null && dataHolder.CurrentLobby.Members.Count > i)
            {
                var lobbyRole = dataHolder.CurrentLobby.Members[i].Role;
                if (lobbyRole == PlayerRole.Engineer || lobbyRole == PlayerRole.Technician)
                    role = lobbyRole;
            }

            _playersSpawned = true;
            SpawnByRole(hostPlayer, role);
            return true;
        }
        return false;
    }
#endif

    [ObserversRpc(runLocally: true)]
    private void SetView()
    {
        machine.StartCoroutine(SpawnView());
    }

    [ObserversRpc(runLocally: true)]
    private void SetLevelView()
    {
        machine.StartCoroutine(SetLevelViewCoroutine());
    }
    private IEnumerator SpawnView()
    {
        InstanceHandler.GetInstance<GameViewManager>().ShowView<PlayerSpawnView>(hideOthers: false);
        yield return new WaitForSeconds(5);
        InstanceHandler.GetInstance<GameViewManager>().HideView<PlayerSpawnView>();
    }

    private IEnumerator SetLevelViewCoroutine()
    {
        InstanceHandler.GetInstance<GameViewManager>()?.ShowView<LevelView>(hideOthers: false);
        yield return new WaitForSeconds(5);
        InstanceHandler.GetInstance<GameViewManager>()?.HideView<LevelView>();
    }


    private void SpawnPlayerSimple()
    {
        Debug.Log($"[PlayerSpawningState] Spawning players for {networkManager.playerCount} players.");
        var dataHolder = FindFirstObjectByType<LobbyDataHolder>();

        if (dataHolder == null || !dataHolder.CurrentLobby.IsValid ||
            dataHolder.CurrentLobby.Members == null ||
            dataHolder.CurrentLobby.Members.Count < RequiredPlayerCount)
        {
            SpawnDefault();
            return;
        }

        if (networkManager.playerCount > 0)
        {
            Debug.Log($"Spawning player {networkManager.players[0]} with role {dataHolder.CurrentLobby.Members[0].Role}");
            PlayerRole hostRole = dataHolder.CurrentLobby.Members[0].Role;
            SpawnByRole(networkManager.players[0], hostRole);

        }
        if (networkManager.playerCount > 1)
        {
            PlayerRole clientRole = dataHolder.CurrentLobby.Members[1].Role;
            SpawnByRole(networkManager.players[1], clientRole);

        }



    }

    private void SpawnByRole(PlayerID ownerPlayer, PlayerRole role)
    {
        PlayerInventory prefabToSpawn = defaultPrefab;
        Transform spawnPoint = null;

        switch (role)
        {
            case PlayerRole.Engineer:
                prefabToSpawn = engineerPrefab;
                spawnPoint = engineerSpawnPoint;
                break;
            case PlayerRole.Technician:
                prefabToSpawn = technicianPrefab;
                spawnPoint = technicianSpawnPoint;
                break;
        }

        if (spawnPoint == null)
        {
            spawnPoint = fallbackSpawnPoints.Count > 0 ? fallbackSpawnPoints[0] : transform;
        }

        var instance = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

        instance.GiveOwnership(ownerPlayer);

    }

    private void SpawnDefault()
    {
        if (networkManager.playerCount > 0)
        {
            SpawnByRole(networkManager.players[0], PlayerRole.Engineer);

        }
        if (networkManager.playerCount > 1)
        {
            SpawnByRole(networkManager.players[1], PlayerRole.Technician);

        }
    }

    private void DeSpawnPlayers()
    {
        var allPlayers = GameObject.FindObjectsByType<PlayerInventory>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var player in allPlayers) Destroy(player.gameObject);
    }
}
