using System.Collections;
using PurrNet;
using PurrNet.Logging;
using UnityEngine;
using PurrLobby;
using PurrNet.Steam;
using Steamworks;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif

public class ConnectionStarter : MonoBehaviour
{
    private NetworkManager _networkManager;
    private LobbyDataHolder _lobbyDataHolder;
    [SerializeField] private RadioVoiceManager _voiceManager;
    private void Awake()
    {
        if (!TryGetComponent(out _networkManager))
        {
            PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
        }

        _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!_lobbyDataHolder)
            PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);


    }

    private void Start()
    {
        if (!_networkManager)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(NetworkManager)} is null!", this);
            return;
        }

        if (!_lobbyDataHolder)
        {
            PurrLogger.LogError($"Failed to start connection. {nameof(LobbyDataHolder)} is null!", this);
            return;
        }

        if (!_lobbyDataHolder.CurrentLobby.IsValid)
        {
            PurrLogger.LogError($"Failed to start connection. Lobby is invalid!", this);
            return;
        }

        if (_networkManager.transport is SteamTransport)
        {
            if (!ulong.TryParse(_lobbyDataHolder.CurrentLobby.LobbyId, out ulong steamLobbyID))
            {
                PurrLogger.LogError($"Failed to parse Steam Lobby ID!", this);
                return;
            }


            if (_voiceManager != null)
            {
                _voiceManager.StartLobbyVoice(steamLobbyID.ToString());
            }

            var lobbyOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(steamLobbyID));
            if (!lobbyOwner.IsValid())
            {
                PurrLogger.LogError($"Failed to get Steam Lobby Owner!", this);
                return;
            }
            (_networkManager.transport as SteamTransport).address = lobbyOwner.ToString();

        }

#if UTP_LOBBYRELAY
        else if(_networkManager.transport is UTPTransport) {
            if(_lobbyDataHolder.CurrentLobby.IsOwner) {
                (_networkManager.transport as UTPTransport).InitializeRelayServer((Allocation)_lobbyDataHolder.CurrentLobby.ServerObject);
            }
            (_networkManager.transport as UTPTransport).InitializeRelayClient(_lobbyDataHolder.CurrentLobby.Properties["JoinCode"]);
            
        }
#else
#endif

        if (_lobbyDataHolder.CurrentLobby.IsOwner)
            _networkManager.StartServer();
        StartCoroutine(StartClient());
    }

    private IEnumerator StartClient()
    {
        yield return new WaitForSeconds(1f);
        _networkManager.StartClient();
    }
}