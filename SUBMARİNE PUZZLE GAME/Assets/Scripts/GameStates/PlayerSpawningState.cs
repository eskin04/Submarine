using System.Collections.Generic;
using System.Linq;
using PurrNet;
using PurrNet.StateMachine;
using PurrLobby;
using UnityEngine;
using TMPro;
// Steam kütüphanesini silebilirsin, burada gerek kalmadı.

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


    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;

        DeSpawnPlayers();
        Debug.Log("Sadeleştirilmiş Spawn Mantığı Başlıyor...");

        SpawnPlayerSimple();

        machine.Next();
    }

    private void SpawnPlayerSimple()
    {
        var dataHolder = FindFirstObjectByType<LobbyDataHolder>();

        if (dataHolder == null || !dataHolder.CurrentLobby.IsValid)
        {
            Debug.LogError("Lobi verisi yok! Default spawn çalışıyor.");
            SpawnDefault();
            return;
        }

        if (networkManager.playerCount > 0)
        {
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

        Debug.Log($"Oyuncu Spawnlandı -> Rol: {role}");
    }

    private void SpawnDefault()
    {
        foreach (var networkPlayer in networkManager.players)
        {
            var instance = Instantiate(defaultPrefab, transform.position, Quaternion.identity);
            instance.GiveOwnership(networkPlayer);
        }
    }

    private void DeSpawnPlayers()
    {
        var allPlayers = GameObject.FindObjectsByType<PlayerInventory>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var player in allPlayers) Destroy(player.gameObject);
    }
}