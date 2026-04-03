using System.Collections;
using PurrNet;
using PurrNet.Modules;
using PurrNet.StateMachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndState : StateNode<ushort>
{


    public override void Enter(ushort isWin, bool asServer)
    {
        base.Enter(isWin, asServer);
        if (!asServer) return;
        Debug.Log($"Showing Game End View. Win: {isWin}");

        machine.StartCoroutine(StartAgain(isWin));

    }

    private IEnumerator StartAgain(ushort isWin)
    {
        yield return new WaitForSeconds(3f);
        ShowGameEndView(isWin);
        yield return new WaitForSeconds(3f);

        HideGameEndView();
        int totalScenes = SceneManager.sceneCountInBuildSettings;
        int nextSceneIndex = (SceneManager.GetActiveScene().buildIndex + 1) % totalScenes;
        networkManager.sceneModule.LoadSceneAsync(nextSceneIndex);
    }

    [ObserversRpc]
    private void ShowGameEndView(ushort isWin)
    {
        var view = InstanceHandler.GetInstance<GameEndView>();
        if (view != null)
        {
            view.SetResultText(isWin);
        }
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
