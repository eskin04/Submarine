using System.Collections;
using PurrNet;
using PurrNet.Logging;
using UnityEngine;
using PurrLobby;
using PurrNet.Steam;
using Steamworks;
using Mono.Cecil.Cil;
using PurrNet.Transports;
using System;

#if UTP_LOBBYRELAY
using PurrNet.UTP;
using Unity.Services.Relay.Models;
#endif

public class ConnectionStarter : MonoBehaviour
{
    public static ConnectionStarter Instance { get; private set; }
    private NetworkManager _networkManager;
    private LobbyDataHolder _lobbyDataHolder;
    private RadioVoiceManager _voiceManager;
    public NetworkManager NetManager => _networkManager;
    [PurrScene, SerializeField] private string lobbyScene;
    private bool _isDisconnecting = false;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!TryGetComponent(out _networkManager))
        {
            PurrLogger.LogError($"Failed to get {nameof(NetworkManager)} component.", this);
        }
        _voiceManager = FindFirstObjectByType<RadioVoiceManager>();

        _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!_lobbyDataHolder)
            PurrLogger.LogError($"Failed to get {nameof(LobbyDataHolder)} component.", this);


    }

    private void OnEnable()
    {
        if (_networkManager != null)
        {
            _networkManager.onClientConnectionState += HandleConnectionStateChange;
        }
    }

    private void OnDisable()
    {
        if (_networkManager != null)
        {
            _networkManager.onClientConnectionState -= HandleConnectionStateChange;
        }
    }

    private void HandleConnectionStateChange(ConnectionState state)
    {
        if (_isDisconnecting || _networkManager.isServer) return;

        if (state == ConnectionState.Disconnected)
        {
            _isDisconnecting = true;
            DisconnectAndReturnToMainMenu();
        }
    }




    private async void DisconnectAndReturnToMainMenu()
    {
        if (Unity.Services.Vivox.VivoxService.Instance != null && Unity.Services.Vivox.VivoxService.Instance.IsLoggedIn)
        {
            foreach (var channel in Unity.Services.Vivox.VivoxService.Instance.ActiveChannels)
            {
                await Unity.Services.Vivox.VivoxService.Instance.LeaveChannelAsync(channel.Key);
            }
            await Unity.Services.Vivox.VivoxService.Instance.LogoutAsync();
        }

        if (_lobbyDataHolder != null)
        {
            // _lobbyDataHolder.LeaveLobby() gibi bir metodun varsa burada çağır

        }

        if (_networkManager.isClient)
        {
            _networkManager.StopClient();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(lobbyScene);

        // 5. Kendini Yok Et (Zırhı parçala)
        Destroy(gameObject);
    }

    private void Start()
    {
        if (_networkManager == null || _networkManager.isServer || _networkManager.isClient)
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