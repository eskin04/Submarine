using System.Collections;
using PurrNet;
using PurrNet.Modules;
using PurrNet.StateMachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndState : StateNode
{


    public override void Enter(bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;
        ShowGameEndView();
        machine.StartCoroutine(StartAgain());
    }

    private IEnumerator StartAgain()
    {
        yield return new WaitForSeconds(3);

        HideGameEndView();
        networkManager.sceneModule.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }
    [ObserversRpc]
    private void ShowGameEndView()
    {
        InstanceHandler.GetInstance<GameViewManager>().ShowView<GameEndView>(hideOthers: false);

    }
    [ObserversRpc]
    private void HideGameEndView()
    {
        InstanceHandler.GetInstance<GameViewManager>().HideView<GameEndView>();

    }


    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (!asServer) return;
    }

}
