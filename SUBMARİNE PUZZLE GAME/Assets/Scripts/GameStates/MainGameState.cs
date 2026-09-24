using System;
using PurrNet;
using PurrNet.StateMachine;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainGameState : StateNode
{
    public static Action startGame;
    [Header("Tutorial")]
    public static Action startTutorial;
    public static bool isTutorialStartedFlag = false;
    public static Action OnTutorialFinished;
    public bool isTutorial = false;
    [PurrScene, SerializeField] private string lobbyScene;

    private bool _isRestarting = false;
    private bool _isSettingsMenuOpen = false;

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        SettingsView.resumeGame += CloseSettingsView;
        SettingsView.quitGame += QuitGame;
        if (!asServer) return;
        SettingsView.restartGame += RestartGame;
        if (isTutorial)
        {
            OnTutorialFinished += FinishTutorial;
            startTutorial?.Invoke();
            isTutorialStartedFlag = true;
            return;
        }
        startGame?.Invoke();
        FloodManager.OnGameEnd += HandleGameEnd;
    }

    private void FinishTutorial()
    {
        HandleGameEnd(1);
    }

    // public override void StateUpdate(bool asServer)
    // {
    //     base.StateUpdate(asServer);
    //     if (!asServer) return;
    //     if (Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         if (!_isSettingsMenuOpen)
    //             OpenSettingsView();
    //         else
    //             CloseSettingsView();
    //     }
    // }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!_isSettingsMenuOpen)
                OpenSettingsView();
            else
                CloseSettingsView();
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
        _isSettingsMenuOpen = true;
        InstanceHandler.GetInstance<GameViewManager>().ShowView<SettingsView>(hideOthers: false);

    }

    private void CloseSettingsView()
    {
        _isSettingsMenuOpen = false;
        InstanceHandler.GetInstance<GameViewManager>().HideView<SettingsView>();

    }

    private async void RestartGame()
    {
        if (!networkManager.isServer) return;
        if (_isRestarting) return;
        _isRestarting = true;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        await ConnectionStarter.Instance.NetManager.sceneModule.LoadSceneAsync(currentSceneIndex);
    }

    private async void QuitGame()
    {

        RadioVoiceManager.Instance?.LeaveVoiceChannel();
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

        // LoadingScreenManager.Instance?.SetGameStarted(false);
    }



    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        SettingsView.resumeGame -= CloseSettingsView;
        SettingsView.quitGame -= QuitGame;

        if (!asServer) return;
        FloodManager.OnGameEnd -= HandleGameEnd;
        SettingsView.restartGame -= RestartGame;
        if (isTutorial)
        {
            OnTutorialFinished -= FinishTutorial;
        }
    }


}
