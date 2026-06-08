using System.Collections;
using PurrNet;
using PurrNet.Modules;
using PurrNet.StateMachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameEndState : StateNode<ushort>
{
    [PurrScene] public string NextScene;


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

        RpcShowLoadingScreen();

        yield return new WaitForSeconds(0.6f);

        HideGameEndView();
        if (isWin == 1)
        {
            networkManager.sceneModule.LoadSceneAsync(NextScene);
        }
        else
        {
            networkManager.sceneModule.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
        }
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

    [ObserversRpc]
    private void RpcShowLoadingScreen()
    {
        // Ağ üzerinden bu emri alan herkes kendi lokalindeki UI'ı açar
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreen();
        }
    }


    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
        if (!asServer) return;
    }

}
