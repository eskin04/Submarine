using UnityEngine;
using UnityEngine.UI;
using PurrNet;
using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class ContractManager : NetworkBehaviour
{
    [PurrScene] public string NextScene;

    public CanvasGroup[] contractPages; // Artık RectTransform yerine CanvasGroup kullanıyoruz
    public Toggle acceptToggle;
    public Button nextButton;
    public Button prevButton;

    public float fadeDuration = 0.5f;

    private int currentPageIndex = 0;
    private bool isFading = false; // Geçiş sırasında art arda tıklanmayı engellemek için

    private HashSet<PlayerID> readyPlayers = new HashSet<PlayerID>();

    void Start()
    {
        // Sadece ilk sayfa görünür olsun, diğerlerini gizle (Alpha = 0) ve etkileşimi kapat
        for (int i = 0; i < contractPages.Length; i++)
        {
            contractPages[i].alpha = (i == 0) ? 1f : 0f;
            contractPages[i].blocksRaycasts = (i == 0);
            contractPages[i].interactable = (i == 0);
        }

        acceptToggle.gameObject.SetActive(false);
        acceptToggle.onValueChanged.AddListener(OnAcceptToggled);

        UpdateButtons();
    }

    protected override void OnDestroy()
    {
        acceptToggle.onValueChanged.RemoveListener(OnAcceptToggled);
    }

    public void NextPage()
    {
        if (currentPageIndex < contractPages.Length - 1 && !isFading)
        {
            FadeTransition(currentPageIndex, currentPageIndex + 1);
        }
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0 && !isFading)
        {
            FadeTransition(currentPageIndex, currentPageIndex - 1);
        }
    }

    private void FadeTransition(int fromIndex, int toIndex)
    {
        isFading = true;

        // Mevcut sayfayı karart (Fade Out)
        contractPages[fromIndex].blocksRaycasts = false;
        contractPages[fromIndex].interactable = false;

        contractPages[fromIndex].DOFade(0f, fadeDuration).OnComplete(() =>
        {
            // Kararma bitince, yeni sayfayı aydınlat (Fade In)
            currentPageIndex = toIndex;

            contractPages[currentPageIndex].blocksRaycasts = true;
            contractPages[currentPageIndex].interactable = true;

            contractPages[currentPageIndex].DOFade(1f, fadeDuration).OnComplete(() =>
            {
                isFading = false;
                UpdateButtons();
            });
        });
    }

    private void UpdateButtons()
    {
        prevButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < contractPages.Length - 1);

        // Checkbox sadece son sayfada görünsün
        if (currentPageIndex == contractPages.Length - 1)
        {
            // Eğer checkbox zaten aktif değilse, onu da ufak bir fade ile getirebiliriz
            if (!acceptToggle.gameObject.activeSelf)
            {
                acceptToggle.gameObject.SetActive(true);
                CanvasGroup toggleCanvasGroup = acceptToggle.GetComponent<CanvasGroup>();
                if (toggleCanvasGroup != null)
                {
                    toggleCanvasGroup.alpha = 0f;
                    toggleCanvasGroup.DOFade(1f, fadeDuration);
                }
            }
        }
    }

    // --- Ağ (Network) Kısmı ---

    private void OnAcceptToggled(bool isReady)
    {
        // PurrNet ile durumu sunucuya bildir
        SetPlayerReadyServerRpc(isReady);
    }

    // Sadece sunucuda çalışır, client'lar çağırabilir (RequireOwnership = false)
    [ServerRpc(requireOwnership: false)]
    private void SetPlayerReadyServerRpc(bool isReady, RPCInfo info = default)
    {
        Debug.Log($"Player {info.sender} is {(isReady ? "ready" : "not ready")}.");
        if (isReady)
        {
            readyPlayers.Add(info.sender);
        }
        else
        {
            readyPlayers.Remove(info.sender);
        }

        CheckAllPlayersReady();
    }

    // Sunucu herkesin hazır olup olmadığını kontrol eder
    private void CheckAllPlayersReady()
    {
        // PurrNet'teki mevcut oyuncu sayısını alıyoruz (Örn: 2 kişi)
        int totalPlayers = NetworkManager.main.playerCount;
        Debug.Log($"Ready Players: {readyPlayers.Count}/{totalPlayers}");

        // Herkes onayladıysa
        if (readyPlayers.Count >= totalPlayers && totalPlayers > 0)
        {
            StartLevel1();
        }
    }

    private void StartLevel1()
    {
        PlayerPrefs.SetInt("ContractSigned", 1);
        PlayerPrefs.Save();
        // PurrNet Scene Loading modülü ile sahneyi tüm oyuncular için yükle
        networkManager.sceneModule.LoadSceneAsync(NextScene);
    }
}