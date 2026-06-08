using UnityEngine;
using PurrLobby;
using UnityEngine.SceneManagement;
using PurrNet;
using System.Collections;

public class GameLoader : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private LevelData[] allLevels;



    [PurrScene] public string ContractScene;

    [SerializeField] private bool showContractOnlyOnce = true;

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

            StartCoroutine(LoadLevelWithContract(dataToLoad));

        }
    }

    private IEnumerator LoadLevelWithContract(LevelData levelData)
    {
        lobbyManager.SetLobbyStarted();
        LoadingScreenManager.Instance?.ShowLoadingScreen();
        yield return new WaitForSeconds(.6f);

        if (levelData.levelID == 1)
        {
            // bool hasSignedContract = PlayerPrefs.GetInt("ContractSigned", 0) == 1;
            bool hasSignedContract = false;

            if (!showContractOnlyOnce || !hasSignedContract)
            {
                Debug.Log($"[GameLoader] Herkes hazır! Level 1 ilk kez oynanıyor, {ContractScene} yükleniyor...");

                SceneManager.LoadSceneAsync(ContractScene);
                yield break;
            }
        }


        SceneManager.LoadSceneAsync(levelData.CurrentScene);
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