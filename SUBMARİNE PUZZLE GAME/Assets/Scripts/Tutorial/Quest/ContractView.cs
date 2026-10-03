using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using FMODUnity;
using PurrNet;

public class ContractView : View
{
    public static event Action OnContractRead;
    public static event Action OnContractSigned;

    [Header("UI References")]
    public CanvasGroup[] contractPages;
    public Toggle acceptToggle;
    public Button nextButton;
    public Button prevButton;

    [Header("Animation Settings")]
    public float fadeDuration = 0.4f;
    public float scaleEffectAmount = 0.95f;

    [Header("Audio")]
    public AudioEventChannelSO _channel;
    public EventReference pageTurnSound;
    public EventReference contractSignSound;

    private int currentPageIndex = 0;
    private bool isFading = false;
    private bool hasReadContract = false;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        if (nextButton != null) nextButton.onClick.AddListener(NextPage);
        if (prevButton != null) prevButton.onClick.AddListener(PreviousPage);
        if (acceptToggle != null) acceptToggle.onValueChanged.AddListener(OnAcceptToggled);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ContractView>();
    }

    public override void OnShow()
    {
        currentPageIndex = 0;
        isFading = false;
        hasReadContract = false;

        for (int i = 0; i < contractPages.Length; i++)
        {
            if (contractPages[i] != null)
            {
                contractPages[i].alpha = (i == 0) ? 1f : 0f;
                contractPages[i].blocksRaycasts = (i == 0);
                contractPages[i].interactable = (i == 0);
                contractPages[i].transform.localScale = (i == 0) ? Vector3.one : Vector3.one * scaleEffectAmount;
            }
        }

        if (acceptToggle != null)
        {
            acceptToggle.SetIsOnWithoutNotify(false);
            acceptToggle.gameObject.SetActive(false);
        }
        UpdateButtons();
        ToggleView(true);
    }

    public override void OnHide()
    {
        if (acceptToggle != null) acceptToggle.SetIsOnWithoutNotify(false);
        ToggleView(false);
    }

    private void ToggleView(bool isVisible)
    {

        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isVisible;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;

    }

    public void NextPage()
    {
        if (currentPageIndex < contractPages.Length - 1 && !isFading)
            FadeTransition(currentPageIndex, currentPageIndex + 1);
    }

    public void PreviousPage()
    {
        if (currentPageIndex > 0 && !isFading)
            FadeTransition(currentPageIndex, currentPageIndex - 1);
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
        if (prevButton != null) prevButton.gameObject.SetActive(currentPageIndex > 0);
        if (nextButton != null) nextButton.gameObject.SetActive(currentPageIndex < contractPages.Length - 1);

        if (currentPageIndex == contractPages.Length - 1)
        {
            if (!hasReadContract)
            {
                hasReadContract = true;
                OnContractRead?.Invoke();
            }

            if (acceptToggle != null && !acceptToggle.gameObject.activeSelf)
            {
                acceptToggle.gameObject.SetActive(true);
                CanvasGroup toggleGroup = acceptToggle.GetComponent<CanvasGroup>();
                if (toggleGroup != null)
                {
                    toggleGroup.alpha = 0f;
                    toggleGroup.DOFade(1f, fadeDuration);
                }
            }
        }
        else
        {
            if (acceptToggle != null && acceptToggle.gameObject.activeSelf)
            {
                acceptToggle.gameObject.SetActive(false);
                acceptToggle.SetIsOnWithoutNotify(false);
            }
        }
    }

    private void OnAcceptToggled(bool isReady)
    {
        if (isReady)
        {
            PlaySound(contractSignSound);
            OnContractSigned?.Invoke();
        }
    }

    private void PlaySound(EventReference sound)
    {
        if (_channel != null && !sound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(sound, transform.position);
            _channel.RaiseEvent(payload);
        }
    }
}