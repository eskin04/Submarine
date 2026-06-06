using UnityEngine;
using PurrLobby; // LobbyManager'ın bulunduğu isim uzayı

public class VivoxLobbyBridge : MonoBehaviour
{
    public LobbyManager lobbyManager;
    private string currentLobbyId = "";

    void OnEnable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnRoomJoined.AddListener(HandleRoomJoined);
            lobbyManager.OnRoomLeft.AddListener(HandleRoomLeft);
        }
    }

    void OnDisable()
    {
        if (lobbyManager != null)
        {
            lobbyManager.OnRoomJoined.RemoveListener(HandleRoomJoined);
            lobbyManager.OnRoomLeft.RemoveListener(HandleRoomLeft);
        }
    }

    private void HandleRoomJoined(Lobby room)
    {
        if (room.IsValid)
        {
            if (currentLobbyId == room.LobbyId) return;
            currentLobbyId = room.LobbyId;
            string channelName = "LOBBY_" + room.LobbyId;
            Debug.Log($"<color=cyan>[LobbyBridge]</color> Lobiye başarıyla girildi. Telsiz frekansı ayarlanıyor: {channelName}");

            RadioVoiceManager.Instance.StartLobbyVoice(channelName);
        }
    }

    private void HandleRoomLeft()
    {
        currentLobbyId = "";
        Debug.Log("<color=cyan>[LobbyBridge]</color> Lobiden çıkıldı. Telsiz bağlantısı kapatılıyor.");

        RadioVoiceManager.Instance.LeaveVoiceChannel();
    }
}