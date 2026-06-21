using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.Localization;

public class SlotMachineFrontend : MonoBehaviour
{
    public event Action<bool> OnInteract;

    public SlotMachineBackend backend;
    public TextMeshProUGUI betText;

    public RectTransform[] reelContainers;
    public RectTransform[] reelMasks;
    public TextMeshProUGUI resultText;

    public float spinDuration = 2f;
    public float reelDelay = 0.5f;
    public float symbolHeight = 100f;
    public float speed = 2000f;
    private bool _isInteractable = true;
    public bool IsInteractable
    {
        get => _isInteractable;
        private set
        {
            _isInteractable = value;
            OnInteract?.Invoke(_isInteractable);
        }
    }

    private LocalizedString uiResultString = new LocalizedString();


    private void OnEnable()
    {
        backend.OnBetChanged += UpdateBetUI;
        backend.OnSpinCalculated += HandleSpinCalculated;
        backend.OnWin += HandleWin;
        backend.OnPenalty += HandlePenalty;
        backend.OnLoss += HandleLoss;
        uiResultString.StringChanged += OnTranslatedResultReady;
    }

    private void OnDisable()
    {
        backend.OnBetChanged -= UpdateBetUI;
        backend.OnSpinCalculated -= HandleSpinCalculated;
        backend.OnWin -= HandleWin;
        backend.OnPenalty -= HandlePenalty;
        backend.OnLoss -= HandleLoss;
        uiResultString.StringChanged -= OnTranslatedResultReady;
    }

    private void OnTranslatedResultReady(string translatedText)
    {
        if (resultText != null)
        {
            resultText.text = translatedText;
        }
    }

    private void Start()
    {
        UpdateBetUI(backend.minBet);

        if (resultText != null)
        {
            resultText.text = "";
            resultText.alpha = 0f;
        }
    }
    private void UpdateBetUI(int newBet)
    {
        betText.text = newBet.ToString();
        betText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 10, 1f);
    }

    private void HandleSpinCalculated(AlchemicalSymbol[] result)
    {
        IsInteractable = false;
        if (resultText != null)
        {
            resultText.DOKill();
            resultText.alpha = 0f;
        }

        StartCoroutine(AnimateReels(result));
    }

    private IEnumerator AnimateReels(AlchemicalSymbol[] result)
    {
        for (int i = 0; i < reelContainers.Length; i++)
        {
            int symbolIndex = (int)result[i];

            StartCoroutine(SpinAndSnapReel(reelContainers[i], reelMasks[i], symbolIndex));

            yield return new WaitForSeconds(reelDelay);
        }

        yield return new WaitForSeconds(spinDuration + 0.5f);

        backend.FinalizeSpin();
    }

    private IEnumerator SpinAndSnapReel(RectTransform reel, RectTransform mask, int targetSymbolIndex)
    {
        float totalHeight = 7 * symbolHeight;
        float timePassed = 0f;

        while (timePassed < spinDuration)
        {
            timePassed += Time.deltaTime;
            Vector2 pos = reel.anchoredPosition;
            pos.y -= speed * Time.deltaTime;

            if (pos.y <= 0)
            {
                pos.y += totalHeight;
            }

            reel.anchoredPosition = pos;
            yield return null;
        }


        float baseTargetY = targetSymbolIndex * symbolHeight;

        float maskHeight = mask.rect.height;

        float centeringOffset = (maskHeight - symbolHeight) / 2f;

        float finalTargetY = baseTargetY - centeringOffset;

        if (reel.anchoredPosition.y < finalTargetY)
        {
            reel.anchoredPosition = new Vector2(reel.anchoredPosition.x, reel.anchoredPosition.y + totalHeight);
        }

        reel.DOAnchorPosY(finalTargetY, 0.5f).SetEase(Ease.OutElastic);
    }
    private void HandleWin(int amount, bool isMatch3)
    {
        Debug.Log($"Frontend: Kazandın! Miktar: {amount}");

        var args = new Dictionary<string, string> { { "Amount", $"-{amount}" } };
        ShowLocalizedResultText("slot_water_change", args, Color.green);
        IsInteractable = true;
    }

    private void HandlePenalty(int amount, bool isMatch3)
    {
        Debug.Log($"Frontend: Ceza! Miktar: {amount}");

        var args = new Dictionary<string, string> { { "Amount", $"+{amount}" } };
        ShowLocalizedResultText("slot_water_change", args, Color.red);
        IsInteractable = true;
    }

    private void HandleLoss()
    {
        Debug.Log("Frontend: Kaybettin.");

        ShowLocalizedResultText("slot_loss", null, Color.gray);
        IsInteractable = true;
    }

    private void ShowLocalizedResultText(string key, Dictionary<string, string> args, Color color)
    {
        {
            if (resultText == null) return;

            resultText.DOKill();
            resultText.color = color;

            // 1. Önce argümanları (varsa) sisteme yükle (Race condition önlemi)
            if (args != null)
            {
                uiResultString.Arguments = new object[] { args };
            }
            else
            {
                uiResultString.Arguments = null; // Eski argümanları temizle
            }

            // 2. Şifreyi ver ve zorla yenile
            // (Eğer farklı bir tablo kullanıyorsan "UI_Core" kısmını güncellemeyi unutma)
            uiResultString.TableReference = "UI_General";
            uiResultString.TableEntryReference = key;
            uiResultString.RefreshString();

            resultText.alpha = 1f;
            resultText.transform.localScale = Vector3.zero;

            Sequence seq = DOTween.Sequence();

            seq.Append(resultText.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack));

            seq.AppendInterval(2f);

            seq.Append(resultText.DOFade(0f, 1f));
        }
    }
}