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
    private static MainGameState tutorialStartOwner;
    public static bool isTutorialStartedFlag => tutorialStartOwner != null &&
        tutorialStartOwner.isActiveAndEnabled && tutorialStartOwner.IsSpawned(true) &&
        tutorialStartOwner.isServer && tutorialStartOwner.isCurrentState && tutorialStartOwner.isTutorial;
    public static Action OnTutorialFinished;
    public bool isTutorial = false;
    [PurrScene, SerializeField] private string lobbyScene;

    private bool _isRestarting = false;
    private bool _isSettingsMenuOpen = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetTutorialStartSession() { tutorialStartOwner = null; }

    private void ReleaseTutorialStart()
    {
        if (ReferenceEquals(tutorialStartOwner, this)) tutorialStartOwner = null;
    }

    protected override void OnEarlySpawn(bool asServer)
    {
        base.OnEarlySpawn(asServer);
        if (asServer) ReleaseTutorialStart();
    }

    protected override void OnDespawned(bool asServer)
    {
        if (asServer) ReleaseTutorialStart();
        base.OnDespawned(asServer);
    }

    protected override void OnDestroy()
    {
        ReleaseTutorialStart();
        base.OnDestroy();
    }

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer)
        {
            SettingsView.resumeGame += CloseSettingsView;
            SettingsView.quitGame += QuitGame;
            return;
        }
        SettingsView.restartGame += RestartGame;
        if (isTutorial)
        {
            OnTutorialFinished += FinishTutorial;
            tutorialStartOwner = this;
            startTutorial?.Invoke();
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
        if (asServer) ReleaseTutorialStart();
        base.Exit(asServer);
        if (!asServer)
        {
            SettingsView.resumeGame -= CloseSettingsView;
            SettingsView.quitGame -= QuitGame;
            return;
        }
        FloodManager.OnGameEnd -= HandleGameEnd;
        SettingsView.restartGame -= RestartGame;
        if (isTutorial)
        {
            OnTutorialFinished -= FinishTutorial;
        }
    }


}
