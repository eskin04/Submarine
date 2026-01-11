using UnityEngine;
using UnityEngine.UI; // Button ve RawImage için gerekli
using PurrLobby;
using System.Collections.Generic;

public class LobbyRoleUI : MonoBehaviour
{
    [Header("Ayarlar")]
    public Texture2D defaultAvatar;

    [Header("Mühendis (Engineer) UI")]
    public Button btnEngineer;
    public RawImage imgEngineerAvatar;
    public GameObject imgEngineerTaken;

    [Header("Teknisyen (Technician) UI")]
    public Button btnTechnician;
    public RawImage imgTechnicianAvatar;
    public GameObject imgTechnicianTaken;

    [Header("Referanslar")]
    public LobbyManager lobbyManager;

    private void OnEnable()
    {
        if (lobbyManager != null)
            lobbyManager.OnRoomUpdated.AddListener(UpdateUI);
    }

    private void OnDisable()
    {
        if (lobbyManager != null)
            lobbyManager.OnRoomUpdated.RemoveListener(UpdateUI);
    }

    public void UpdateUI(Lobby currentLobby)
    {
        ResetUI();

        if (!currentLobby.IsValid || currentLobby.Members == null) return;

        foreach (var user in currentLobby.Members)
        {
            switch (user.Role)
            {
                case PlayerRole.Engineer:
                    btnEngineer.interactable = false;

                    if (user.Avatar != null)
                        imgEngineerAvatar.texture = user.Avatar;

                    SetImageAlpha(imgEngineerAvatar, 1f);

                    if (imgEngineerTaken) imgEngineerTaken.SetActive(true);
                    break;

                case PlayerRole.Technician:
                    btnTechnician.interactable = false;

                    if (user.Avatar != null)
                        imgTechnicianAvatar.texture = user.Avatar;

                    SetImageAlpha(imgTechnicianAvatar, 1f);

                    if (imgTechnicianTaken) imgTechnicianTaken.SetActive(true);
                    break;
            }
        }
    }

    private void ResetUI()
    {
        btnEngineer.interactable = true;
        imgEngineerAvatar.texture = defaultAvatar;
        SetImageAlpha(imgEngineerAvatar, 0.5f);
        if (imgEngineerTaken) imgEngineerTaken.SetActive(false);

        btnTechnician.interactable = true;
        imgTechnicianAvatar.texture = defaultAvatar;
        SetImageAlpha(imgTechnicianAvatar, 0.5f);
        if (imgTechnicianTaken) imgTechnicianTaken.SetActive(false);
    }

    private void SetImageAlpha(RawImage img, float alpha)
    {
        if (img == null) return;
        Color color = img.color;
        color.a = alpha;
        img.color = color;
    }
}