using UnityEngine;
using UnityEngine.UI;
using PurrNet;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;

public class ContractManager : NetworkBehaviour
{
    [Header("Configuration")]
    [PurrScene] public string NextScene;
    public float delayBeforeLoad = 1.5f;

    [Header("UI")]
    public CanvasGroup[] contractPages;
    public Toggle acceptToggle;
    public Button nextButton;
    public Button prevButton;
    public TextMeshProUGUI statusText;

    [Header("Animation")]
    public float fadeDuration = 0.4f;
    public float scaleEffectAmount = 0.95f;

    [Header("Audio")]

    public AudioEventChannelSO _channel;
    public FMODUnity.EventReference pageTurnSound;
    public FMODUnity.EventReference contractSignSound;
    public FMODUnity.EventReference allReadySound;

    private int currentPageIndex = 0;
    private bool isFading = false;
    private HashSet<PlayerID> readyPlayers = new HashSet<PlayerID>();

    void Start()
    {
        for (int i = 0; i < contractPages.Length; i++)
        {
            contractPages[i].alpha = (i == 0) ? 1f : 0f;
            contractPages[i].blocksRaycasts = (i == 0);
            contractPages[i].interactable = (i == 0);
            contractPages[i].transform.localScale = (i == 0) ? Vector3.one : Vector3.one * scaleEffectAmount;
        }

        acceptToggle.gameObject.SetActive(false);
        acceptToggle.onValueChanged.AddListener(OnAcceptToggled);

        if (statusText != null) statusText.text = "Read the contract carefully and accept to start the mission.";

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

    private void PlaySound(FMODUnity.EventReference sound)
    {
        if (_channel != null && !sound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(sound, this.transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    private void FadeTransition(int fromIndex, int toIndex)
    {
        isFading = true;
        PlaySound(pageTurnSound);
        contractPages[fromIndex].blocksRaycasts = false;
        contractPages[fromIndex].interactable = false;

        contractPages[toIndex].blocksRaycasts = true;
        contractPages[toIndex].interactable = true;


        contractPages[fromIndex].DOFade(0f, fadeDuration).SetEase(Ease.OutQuad);
        contractPages[fromIndex].transform.DOScale(scaleEffectAmount, fadeDuration).SetEase(Ease.OutQuad);

        contractPages[toIndex].DOFade(1f, fadeDuration).SetEase(Ease.InQuad);
        contractPages[toIndex].transform.DOScale(1f, fadeDuration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            currentPageIndex = toIndex;
            isFading = false;
            UpdateButtons();
        });
    }

    private void UpdateButtons()
    {
        prevButton.gameObject.SetActive(currentPageIndex > 0);
        nextButton.gameObject.SetActive(currentPageIndex < contractPages.Length - 1);

        if (currentPageIndex == contractPages.Length - 1)
        {
            if (!acceptToggle.gameObject.activeSelf)
            {
                acceptToggle.gameObject.SetActive(true);
                CanvasGroup toggleCanvasGroup = acceptToggle.GetComponent<CanvasGroup>();
                if (toggleCanvasGroup != null)
                {
                    toggleCanvasGroup.alpha = 0f;
                    toggleCanvasGroup.DOFade(1f, fadeDuration);
                }

                UpdateStatusTextRpc(readyPlayers.Count, NetworkManager.main.playerCount);
            }
        }
        else
        {
            if (acceptToggle.gameObject.activeSelf)
            {
                acceptToggle.gameObject.SetActive(false);
                acceptToggle.isOn = false;
            }
        }
    }


    private void OnAcceptToggled(bool isReady)
    {
        if (isReady)
        {
            PlaySound(contractSignSound);
        }

        SetPlayerReadyServerRpc(isReady);
    }

    [ServerRpc(requireOwnership: false)]
    private void SetPlayerReadyServerRpc(bool isReady, RPCInfo info = default)
    {
        if (isReady)
            readyPlayers.Add(info.sender);
        else
            readyPlayers.Remove(info.sender);

        int totalPlayers = NetworkManager.main.playerCount;

        UpdateStatusTextRpc(readyPlayers.Count, totalPlayers);

        if (readyPlayers.Count >= totalPlayers && totalPlayers > 0)
        {
            StartLevelSequenceRpc();
        }
    }

    [ObserversRpc]
    private void UpdateStatusTextRpc(int readyCount, int totalCount)
    {
        if (statusText != null && currentPageIndex == contractPages.Length - 1)
        {
            statusText.text = $"Ready Players: {readyCount}/{totalCount}";
        }
    }

    [ObserversRpc]
    private void StartLevelSequenceRpc()
    {
        if (statusText != null)
        {
            statusText.text = "CONTRACT SIGNED. MISSION STARTING...";
            statusText.color = Color.green;
        }

        acceptToggle.interactable = false;
        prevButton.interactable = false;
        nextButton.interactable = false;
        if (LoadingScreenManager.Instance != null)
        {
            LoadingScreenManager.Instance.ShowLoadingScreenRPC();
        }


        PlaySound(allReadySound);

        if (isServer)
        {
            DOVirtual.DelayedCall(delayBeforeLoad, () =>
            {
                PlayerPrefs.SetInt("ContractSigned", 1);
                PlayerPrefs.Save();
                networkManager.sceneModule.LoadSceneAsync(NextScene);
            });
        }
        else
        {
            PlayerPrefs.SetInt("ContractSigned", 1);
            PlayerPrefs.Save();
        }
    }
}