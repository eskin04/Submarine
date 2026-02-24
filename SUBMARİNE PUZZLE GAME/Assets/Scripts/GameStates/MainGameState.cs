using System;
using PurrNet;
using PurrNet.StateMachine;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameState : StateNode
{
    public static Action startGame;

    public bool isTestMode = false;
    [PurrScene, SerializeField] private string lobbyScene;

    private bool _isRestarting = false;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer || isTestMode) return;
        startGame?.Invoke();
        FloodManager.OnGameEnd += HandleGameEnd;
        SettingsView.resumeGame += CloseSettingsView;
        SettingsView.restartGame += RestartGame;
        SettingsView.quitGame += QuitGame;
    }

    public override void StateUpdate(bool asServer)
    {
        base.StateUpdate(asServer);
        if (!asServer) return;
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            OpenSettingsView();
        }
    }

    private void HandleGameEnd(int isGameWin)
    {
        machine.Next((ushort)isGameWin);
    }

    public void StartGame()
    {
        startGame?.Invoke();
    }

    private void OpenSettingsView()
    {
        InstanceHandler.GetInstance<GameViewManager>().ShowView<SettingsView>(hideOthers: false);

    }

    private void CloseSettingsView()
    {
        InstanceHandler.GetInstance<GameViewManager>().HideView<SettingsView>();

    }

    private void RestartGame()
    {
        if (_isRestarting) return;
        _isRestarting = true;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        ConnectionStarter.Instance.NetManager.sceneModule.LoadSceneAsync(currentSceneIndex);
    }

    private async void QuitGame()
    {
        if (VivoxService.Instance != null && VivoxService.Instance.IsLoggedIn)
        {
            foreach (var channel in VivoxService.Instance.ActiveChannels)
            {
                await VivoxService.Instance.LeaveChannelAsync(channel.Key);
            }

            await VivoxService.Instance.LogoutAsync();
        }

        if (networkManager.isServer)
        {
            networkManager.StopServer();
        }
        if (networkManager.isClient)
        {
            networkManager.StopClient();
        }

        if (ConnectionStarter.Instance != null)
        {
            Destroy(ConnectionStarter.Instance.gameObject);
        }
        await SceneManager.LoadSceneAsync(lobbyScene);
    }



    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (!asServer) return;
        FloodManager.OnGameEnd -= HandleGameEnd;
        SettingsView.resumeGame -= CloseSettingsView;
        SettingsView.restartGame -= RestartGame;
        SettingsView.quitGame -= QuitGame;
    }
}
