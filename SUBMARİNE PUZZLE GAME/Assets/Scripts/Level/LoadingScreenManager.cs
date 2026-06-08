using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class LoadingScreenManager : MonoBehaviour // ARTIK SADECE MONOBEHAVIOUR
{
    public static LoadingScreenManager Instance;

    [Header("UI References")]
    public Canvas loadingCanvas;
    public CanvasGroup loadingCanvasGroup;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;

    public bool isShowing = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (loadingCanvas != null)
            {
                loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                loadingCanvas.sortingOrder = 999;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // RPC DEĞİL, Normal Metot
    public void ShowLoadingScreen()
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

    // RPC DEĞİL, Normal Metot
    public void HideLoadingScreen()
    {
        if (!isShowing) return;

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