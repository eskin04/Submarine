using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HullBreach_MapSlot : MonoBehaviour
{
    [Header("Slot Location Info")]
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex;

    [Header("Visuals")]
    public Image backgroundImage;
    public GameObject warningIcon;
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color warningColor = Color.red;

    private bool isBlinking = false;
    private Tweener blinkTween;

    private void Awake()
    {
        if (warningIcon != null)
            warningIcon.SetActive(false);

        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    public void SetWarningState(bool isWarning)
    {
        if (isWarning && !isBlinking)
        {
            isBlinking = true;
            warningIcon.SetActive(true);

            blinkTween = backgroundImage.DOColor(warningColor, 0.5f)
                                      .SetEase(Ease.InOutSine)
                                      .SetLoops(-1, LoopType.Yoyo);
        }
        else if (!isWarning && isBlinking)
        {
            isBlinking = false;
            warningIcon.SetActive(false);

            blinkTween?.Kill();
            backgroundImage.color = normalColor;
        }
    }

    private void OnDisable()
    {
        blinkTween?.Kill();
        backgroundImage.color = normalColor;
        isBlinking = false;
    }
}