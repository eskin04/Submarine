using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Localization;

[RequireComponent(typeof(CanvasGroup))]
public class PromptSlot : MonoBehaviour
{
    [SerializeField] private Image keyIcon;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private TMP_Text actionText;

    private CanvasGroup canvasGroup;

    public string CurrentId { get; private set; }
    public string CurrentKey { get; private set; }
    public string CurrentActionKey { get; private set; }
    public Sprite CurrentIcon { get; private set; }

    private LocalizedString localizedAction = new LocalizedString { TableReference = "UI_General" };

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        localizedAction.StringChanged += OnTranslatedTextChanged;
    }

    private void OnDisable()
    {
        localizedAction.StringChanged -= OnTranslatedTextChanged;
    }

    private void OnTranslatedTextChanged(string translatedText)
    {
        actionText.text = translatedText;
    }

    public void Show(string id, string key, string action, Sprite icon = null)
    {
        CurrentId = id;

        UpdateContent(key, action, icon);

        gameObject.SetActive(true);

        canvasGroup.DOKill();
        canvasGroup.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
    }

    public void UpdateContent(string key, string actionKey, Sprite icon = null)
    {
        CurrentKey = key;
        CurrentActionKey = actionKey;
        CurrentIcon = icon;

        localizedAction.TableEntryReference = actionKey;

        if (icon != null)
        {
            keyIcon.sprite = icon;
            keyIcon.gameObject.SetActive(true);
            keyText.gameObject.SetActive(false);
        }
        else
        {
            keyText.text = $"[{key}]";
            keyText.gameObject.SetActive(true);
            keyIcon.gameObject.SetActive(false);
        }
    }

    public void Hide()
    {
        CurrentId = string.Empty;
        CurrentKey = string.Empty;
        CurrentActionKey = string.Empty;
        CurrentIcon = null;

        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad).OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}