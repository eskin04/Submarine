using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HullBreach_MapSlot : MonoBehaviour
{
    [Header("Slot Location Info")]
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex; // (Yanlar için 0-3, Ön/Arka için 0-1)

    [Header("Visuals")]
    public Image backgroundImage;
    public GameObject warningIcon;
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f); // Koyu Gri
    public Color warningColor = Color.red;

    private bool isBlinking = false;
    private Tweener blinkTween;

    private void Awake()
    {
        // Başlangıçta uyarı simgesini gizle
        if (warningIcon != null)
            warningIcon.SetActive(false);

        // Arka plan rengini normal renge ayarla
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    public void SetWarningState(bool isWarning)
    {
        if (isWarning && !isBlinking)
        {
            isBlinking = true;
            warningIcon.SetActive(true);

            // Rengi normalden kırmızıya yarım saniyede bir (Yoyo) değiştir
            blinkTween = backgroundImage.DOColor(warningColor, 0.5f)
                                      .SetEase(Ease.InOutSine)
                                      .SetLoops(-1, LoopType.Yoyo);
        }
        else if (!isWarning && isBlinking)
        {
            isBlinking = false;
            warningIcon.SetActive(false);

            // Animasyonu durdur ve rengi normale döndür
            blinkTween?.Kill();
            backgroundImage.color = normalColor;
        }
    }

    private void OnDisable()
    {
        // Obje kapanırsa (örn: modülden çıkılırsa) animasyonu temizle
        blinkTween?.Kill();
        backgroundImage.color = normalColor;
        isBlinking = false;
    }
}