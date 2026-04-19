using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class UICursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Transform hoverImage;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverImage != null)
        {
            hoverImage.DOScale(1.1f, 0.1f).SetEase(Ease.OutBack);
        }
        CursorManager.OnHoverStateChanged?.Invoke(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverImage != null)
        {
            hoverImage.DOScale(1f, 0.1f).SetEase(Ease.OutBack);
        }
        CursorManager.OnHoverStateChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        CursorManager.OnHoverStateChanged?.Invoke(false);
    }
}