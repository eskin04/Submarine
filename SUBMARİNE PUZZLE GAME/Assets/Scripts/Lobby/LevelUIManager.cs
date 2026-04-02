using System.Collections.Generic;
using UnityEngine;
using PurrLobby;

public class LevelUIManager : MonoBehaviour
{
    [SerializeField] private LobbyManager lobbyManager;
    [SerializeField] private List<LevelButton> allLevelButtons;

    private void OnEnable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnRoomUpdated.AddListener(OnLobbyUpdated);
            lobbyManager.OnRoomJoined.AddListener(OnLobbyJoined);
            lobbyManager.OnRoomLeft.AddListener(OnLobbyLeft);
        }
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnRoomUpdated.RemoveListener(OnLobbyUpdated);
            lobbyManager.OnRoomJoined.RemoveListener(OnLobbyJoined);
            lobbyManager.OnRoomLeft.RemoveListener(OnLobbyLeft);
        }
    }

    private void Start()
    {
        if (lobbyManager.CurrentLobby.IsValid)
            RefreshButtons();
        else
            DisableAllButtons();
    }

    private void OnLobbyJoined(Lobby room)
    {
        RefreshButtons();
    }

    private void OnLobbyLeft()
    {
        DisableAllButtons();
    }

    public void RefreshButtons()
    {
        if (!lobbyManager.CurrentLobby.IsValid) return;

        // int unlockedLevelID = SaveManager.GetMaxUnlockedLevelID();
        int unlockedLevelID = 8;
        bool isHost = lobbyManager.CurrentLobby.IsOwner;

        foreach (var btn in allLevelButtons)
        {
            bool isUnlocked = btn.levelData.levelID <= unlockedLevelID;
            btn.SetupButton(isUnlocked, isHost, OnLevelSelectedByHost);
        }

        UpdateSelectionVisuals();
    }

    private void DisableAllButtons()
    {
        foreach (var btn in allLevelButtons)
        {
            btn.SetupButton(false, false, null);
        }
    }

    private void OnLevelSelectedByHost(int selectedLevelID)
    {
        lobbyManager.SetLobbyData("SelectedLevelID", selectedLevelID.ToString());
    }

    private void OnLobbyUpdated(Lobby room)
    {
        RefreshButtons();
    }

    private void UpdateSelectionVisuals()
    {
        if (!lobbyManager.CurrentLobby.IsValid) return;

        int selectedLevelID = -1; // Varsayılan değer

        if (lobbyManager.CurrentLobby.Properties != null &&
            lobbyManager.CurrentLobby.Properties.TryGetValue("SelectedLevelID", out string levelString))
        {
            int.TryParse(levelString, out selectedLevelID);
        }

        foreach (var btn in allLevelButtons)
        {
            btn.SetSelected(btn.levelData.levelID == selectedLevelID);
        }
    }
}