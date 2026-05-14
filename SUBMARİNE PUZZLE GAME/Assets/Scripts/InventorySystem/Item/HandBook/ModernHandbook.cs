using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FMODUnity;

public class ModernHandbook : MonoBehaviour
{
    public Image leftPageStatic;
    public Image rightPageStatic;

    public RectTransform pageHinge;
    public Image hingeFront;
    public Image hingeBack;

    [Header("Content")]
    public Sprite[] bookPages;
    public Sprite emptyPageBackground;

    [Header("Settings")]
    public float flipDuration = 0.5f;
    public Ease flipEase = Ease.InOutQuad;

    [Header("Audio Settings")]
    [SerializeField] private AudioEventChannelSO _channel;
    [SerializeField] private EventReference _scrollSound;

    private int currentPageIndex = 0;
    private bool isFlipping = false;

    void Start()
    {
        UpdateStaticPages();
        pageHinge.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isFlipping) return;

        float scroll = Input.mouseScrollDelta.y;



        if (scroll < 0 && currentPageIndex < bookPages.Length - 2)
        {
            FlipRightToLeft();
        }
        else if (scroll > 0 && currentPageIndex > 0)
        {
            FlipLeftToRight();
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