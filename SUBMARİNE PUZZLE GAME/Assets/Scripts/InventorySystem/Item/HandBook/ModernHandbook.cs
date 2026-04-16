using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FMODUnity;

public class ModernHandbook : MonoBehaviour
{
    [Header("Sabit Sayfalar (Arka Plan)")]
    public Image leftPageStatic;
    public Image rightPageStatic;

    [Header("Dönen Menteşe Sistemi")]
    [Tooltip("Pivot noktası X:0, Y:0.5 olmalı")]
    public RectTransform pageHinge;
    public Image hingeFront; // Sağdan sola dönerken görünen yüz
    public Image hingeBack;  // 90 dereceyi geçince görünen arka yüz

    [Header("İçerik")]
    public Sprite[] bookPages;
    public Sprite emptyPageBackground; // Sayfa kalmadığında görünecek boş zemin

    [Header("Ayarlar")]
    public float flipDuration = 0.5f;
    public Ease flipEase = Ease.InOutQuad; // DOTween yumuşatma eğrisi

    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO _channel;
    [SerializeField] private EventReference _scrollSound;

    private int currentPageIndex = 0; // Sol sayfanın indeksi (0, 2, 4...)
    private bool isFlipping = false;

    void Start()
    {
        UpdateStaticPages();
        pageHinge.gameObject.SetActive(false);
    }

    void Update()
    {
        // Sayfa çevriliyorsa yeni girdi alma
        if (isFlipping) return;

        // Mouse tekerleği girdisi
        float scroll = Input.mouseScrollDelta.y;



        if (scroll > 0 && currentPageIndex < bookPages.Length - 2)
        {
            FlipRightToLeft(); // İleri (Aşağı kaydırma)
        }
        else if (scroll < 0 && currentPageIndex > 0)
        {
            FlipLeftToRight(); // Geri (Yukarı kaydırma)
        }
    }

    private void PlayScrollSound()
    {
        if (_channel != null && !_scrollSound.IsNull)
        {
            AudioEventPayload payload = new AudioEventPayload(_scrollSound, transform.position);
            _channel.RaiseEvent(payload);
        }
    }

    private void FlipRightToLeft()
    {
        isFlipping = true;
        PlayScrollSound();
        hingeFront.sprite = bookPages[currentPageIndex + 1];
        hingeBack.sprite = bookPages[currentPageIndex + 2];
        rightPageStatic.sprite = GetPageSprite(currentPageIndex + 3);

        pageHinge.localEulerAngles = Vector3.zero;
        pageHinge.gameObject.SetActive(true);
        hingeFront.gameObject.SetActive(true);
        hingeBack.gameObject.SetActive(false);

        pageHinge.DOLocalRotate(new Vector3(0, 180, 0), flipDuration, RotateMode.Fast)
            .SetEase(flipEase)
            .OnUpdate(() =>
            {
                if (pageHinge.localEulerAngles.y >= 90f)
                {
                    hingeFront.gameObject.SetActive(false);
                    hingeBack.gameObject.SetActive(true);
                }
            })
            .OnComplete(() =>
            {
                currentPageIndex += 2;
                UpdateStaticPages();
                pageHinge.gameObject.SetActive(false);
                isFlipping = false;
            });
    }

    private void FlipLeftToRight()
    {
        isFlipping = true;
        PlayScrollSound();
        hingeFront.sprite = bookPages[currentPageIndex - 1];
        hingeBack.sprite = bookPages[currentPageIndex];
        leftPageStatic.sprite = GetPageSprite(currentPageIndex - 2);

        pageHinge.localEulerAngles = new Vector3(0, 180, 0);
        pageHinge.gameObject.SetActive(true);
        hingeFront.gameObject.SetActive(false);
        hingeBack.gameObject.SetActive(true);

        pageHinge.DOLocalRotate(Vector3.zero, flipDuration, RotateMode.Fast)
            .SetEase(flipEase)
            .OnUpdate(() =>
            {
                if (pageHinge.localEulerAngles.y <= 90f)
                {
                    hingeFront.gameObject.SetActive(true);
                    hingeBack.gameObject.SetActive(false);
                }
            })
            .OnComplete(() =>
            {
                currentPageIndex -= 2;
                UpdateStaticPages();
                pageHinge.gameObject.SetActive(false);
                isFlipping = false;
            });
    }

    private void UpdateStaticPages()
    {
        leftPageStatic.sprite = GetPageSprite(currentPageIndex);
        rightPageStatic.sprite = GetPageSprite(currentPageIndex + 1);
    }

    private Sprite GetPageSprite(int index)
    {
        if (index >= 0 && index < bookPages.Length)
            return bookPages[index];

        return emptyPageBackground;
    }
}