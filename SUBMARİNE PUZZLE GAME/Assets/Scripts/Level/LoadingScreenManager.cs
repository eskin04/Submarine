using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PurrNet;
using DG.Tweening;

public class LoadingScreenManager : NetworkBehaviour
{
    public static LoadingScreenManager Instance;

    [Header("UI References")]
    public CanvasGroup loadingCanvasGroup;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    private bool isShowing = false;

    public bool IsShowing => isShowing;
    private bool isGameStarted = false;
    public bool IsGameStarted => isGameStarted;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetGameStarted(bool started)
    {
        isGameStarted = started;
    }


    [ObserversRpc(runLocally: true)]
    public void ShowLoadingScreenRPC()
    {
        if (isShowing) return;
        isShowing = true;

        loadingCanvasGroup.blocksRaycasts = true;
        progressSlider.value = 0f;
        UpdateProgressText(0f);

        loadingCanvasGroup.DOFade(1f, 0.5f).SetUpdate(true);

        progressSlider.DOValue(0.9f, 3f).SetEase(Ease.OutCubic).SetUpdate(true)
            .OnUpdate(() => UpdateProgressText(progressSlider.value));
    }

    [ObserversRpc(runLocally: true)]
    public void HideLoadingScreen()
    {
        progressSlider.DOKill();

        progressSlider.DOValue(1f, 0.5f).SetUpdate(true)
            .OnUpdate(() => UpdateProgressText(progressSlider.value))
            .OnComplete(() =>
            {
                loadingCanvasGroup.DOFade(0f, 0.5f).SetUpdate(true).OnComplete(() =>
                {
                    loadingCanvasGroup.blocksRaycasts = false;
                    isShowing = false;
                });
            });
    }

    private void UpdateProgressText(float val)
    {
        if (progressText != null)
        {
            progressText.text = $"%{Mathf.RoundToInt(val * 100)}";
        }
    }
}