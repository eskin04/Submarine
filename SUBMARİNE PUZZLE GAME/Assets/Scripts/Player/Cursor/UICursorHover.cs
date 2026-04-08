using UnityEngine;
using UnityEngine.EventSystems;

public class UICursorHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        CursorManager.OnHoverStateChanged?.Invoke(true);
        Debug.Log("Hovering over: " + gameObject.name);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        CursorManager.OnHoverStateChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        CursorManager.OnHoverStateChanged?.Invoke(false);
    }
}