using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class RoE_RadarBlip : MonoBehaviour
{
    [Header("References")]
    public Button btn;
    public Image blipImage;
    public TextMeshProUGUI codeText;

    [Header("Fade Settings")]
    public float fadeInDuration = 0.3f;
    public float stayVisibleTime = 2.0f;
    public float fadeOutDuration = 1.0f;

    [Header("Visual Settings")]
    public Color defaultColor = Color.red;
    public Color selectedColor = Color.green;
    public float defaultScale = 1.0f;
    public float selectedScale = 1.5f;

    private CanvasGroup canvasGroup;

    private string myCodeName;
    private RoE_TechnicianUI uiManager;
    private bool isSelected = false;
    private Sequence fadeSequence;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        fadeSequence?.Kill();
    }


    public void Setup(string codeName, RoE_TechnicianUI manager)
    {
        myCodeName = codeName;
        uiManager = manager;

        if (codeText) codeText.text = codeName;
        if (blipImage == null) blipImage = gameObject.GetComponent<Image>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        uiManager.OnThreatSelected(myCodeName);
    }

    public void SetSelectionState(bool state)
    {
        isSelected = state;

        if (blipImage != null)
        {
            blipImage.DOColor(isSelected ? selectedColor : defaultColor, 0.2f);
            transform.DOScale(isSelected ? selectedScale : defaultScale, 0.2f);
        }

        if (isSelected)
        {
            fadeSequence?.Kill();
            SetFullVisibility(true);
        }
        else
        {
            Ping();
        }
    }

    private void SetFullVisibility(bool state)
    {
        canvasGroup.alpha = state ? 1f : 0f;
        canvasGroup.interactable = state;
        canvasGroup.blocksRaycasts = state;
    }
    public void Ping()
    {
        if (isSelected)
        {
            SetFullVisibility(true);
            return;
        }

        fadeSequence?.Kill();

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        fadeSequence = DOTween.Sequence();

        fadeSequence.Append(canvasGroup.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));

        fadeSequence.AppendInterval(stayVisibleTime);

        fadeSequence.Append(canvasGroup.DOFade(0f, fadeOutDuration).SetEase(Ease.InQuad));

        fadeSequence.OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        });
    }
}