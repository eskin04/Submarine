using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ThanksManager : NetworkBehaviour
{
    [PurrScene, SerializeField] private string lobbyScene;

    public async void QuitGame()
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

    }
}
