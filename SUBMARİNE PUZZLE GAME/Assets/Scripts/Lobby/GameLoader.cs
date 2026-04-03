using UnityEngine;
using PurrLobby;
using UnityEngine.SceneManagement;

public class GameLoader : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private LevelData[] allLevels;

    private void OnEnable()
    {
        if (lobbyManager != null)
            lobbyManager.OnAllReady.AddListener(OnPlayersReadyToStart);
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
            lobbyManager.OnAllReady.RemoveListener(OnPlayersReadyToStart);
    }

    private void OnPlayersReadyToStart()
    {
        if (!lobbyManager.CurrentLobby.IsValid)
            return;

        int levelToLoadID = 1;

        if (lobbyManager.CurrentLobby.Properties != null &&
            lobbyManager.CurrentLobby.Properties.TryGetValue("SelectedLevelID", out string levelString))
        {
            int.TryParse(levelString, out levelToLoadID);
        }

        LevelData dataToLoad = GetLevelDataByID(levelToLoadID);

        if (dataToLoad != null)
        {

            lobbyManager.SetLobbyStarted();

            Debug.Log($"[GameLoader] Herkes hazır! Görev {dataToLoad.levelID} yükleniyor...");

            SceneManager.LoadSceneAsync(dataToLoad.CurrentScene);

        }
    }

    private LevelData GetLevelDataByID(int id)
    {
        foreach (var level in allLevels)
        {
            if (level.levelID == id) return level;
        }
        return null;
    }
}