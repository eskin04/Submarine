using TMPro;
using UnityEngine;
using DG.Tweening;
using PurrNet;


public class ModuleInfoView : View
{
    [SerializeField] private TMP_Text warningText;

    [Header("Animation Settings")]
    [SerializeField] private float yOffset = 30f; // Yazının ne kadar aşağıdan yukarı çıkacağı
    [SerializeField] private float fadeDuration = 0.3f; // Çıkış ve solma animasyonunun hızı
    [SerializeField] private float displayDuration = 1f; // Yazının ekranda net kalacağı süre

    private RectTransform _warningRect;
    private Vector2 _originalPos;
    private Sequence _warningSequence;

    private void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        if (warningText != null)
        {
            _warningRect = warningText.GetComponent<RectTransform>();
            _originalPos = _warningRect.anchoredPosition; // Orijinal pozisyonu hafızaya al

            // Başlangıçta yazıyı görünmez yap
            warningText.alpha = 0f;
        }
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ModuleInfoView>();
        _warningSequence?.Kill();
    }

    public override void OnHide()
    {
        // View gizlendiğinde devam eden animasyon varsa iptal et
        _warningSequence?.Kill();
        if (warningText != null) warningText.alpha = 0f;
    }

    public override void OnShow()
    {
        // İhtiyaca göre doldurulabilir
    }

    public void SetWarningText(string text)
    {
        if (warningText == null || _warningRect == null) return;

        // Eğer halihazırda çalışan bir uyarı animasyonu varsa onu durdur (Yazıların üst üste binmesini engeller)
        _warningSequence?.Kill();

        // Metni ata
        warningText.text = text;

        // Başlangıç durumunu ayarla (Y ekseninde aşağıda ve tamamen saydam)
        _warningRect.anchoredPosition = _originalPos - new Vector2(0, yOffset);
        warningText.alpha = 0f;

        // Yeni DOTween dizisini oluştur
        _warningSequence = DOTween.Sequence();

        // 1. Adım: Yazı yavaşça orijinal pozisyonuna çıksın (DOAnchorPos) ve aynı anda görünür (DOFade) olsun
        _warningSequence.Append(_warningRect.DOAnchorPos(_originalPos, fadeDuration).SetEase(Ease.OutBack));
        _warningSequence.Join(warningText.DOFade(1f, fadeDuration));

        // 2. Adım: Belirlediğimiz süre kadar (1 saniye) ekranda kalsın
        _warningSequence.AppendInterval(displayDuration);

        // 3. Adım: Süre bitince olduğu yerde kalarak saydamlaşıp yok olsun
        _warningSequence.Append(warningText.DOFade(0f, fadeDuration));
    }
}