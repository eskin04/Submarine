using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasGroup))]
public class LightsOut_Cable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Identity")]
    public int cableID;
    public WireColor myPhysicalColor;

    [Header("References")]
    public LightsOut_TechnicianUI uiManager;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Transform parentRect;
    private Vector2 dragOffset;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Setup(int id, WireColor color, LightsOut_TechnicianUI manager)
    {
        cableID = id;
        myPhysicalColor = color;
        uiManager = manager;

        GetComponent<Image>().color = GetColorValue(color);
    }


    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;
        LightsOut_Port currentPort = GetComponentInParent<LightsOut_Port>();
        if (currentPort != null)
        {
            currentPort.TurnOffLight();
        }
        parentRect = transform.parent;
        transform.SetParent(uiManager.dragLayer);
        parentRect = uiManager.dragLayer.GetComponent<RectTransform>();

        Vector2 localMousePos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localMousePos
        );

        dragOffset = rectTransform.anchoredPosition - localMousePos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 localMousePos;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)parentRect,
            eventData.position,
            eventData.pressEventCamera,
            out localMousePos
        ))
        {
            rectTransform.anchoredPosition = localMousePos + dragOffset;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;


        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        LightsOut_Port foundPort = null;

        foreach (RaycastResult result in results)
        {
            LightsOut_Port port = result.gameObject.GetComponentInParent<LightsOut_Port>();

            if (port != null)
            {
                foundPort = port;
                break;
            }
        }

        if (foundPort != null)
        {
            uiManager.OnCableDropped(this, foundPort);
        }
        else
        {
            ResetPosition();

            uiManager.OnCableDisconnected(this);
        }
    }

    public void SnapToPort(LightsOut_Port port)
    {
        transform.SetParent(port.transform);
        rectTransform.position = port.snapPoint.position;
    }

    public void ResetPosition()
    {
        Transform myHomeSlot = uiManager.startSlots[cableID];

        transform.SetParent(myHomeSlot);

        rectTransform.anchoredPosition = Vector2.zero;

    }

    private Color GetColorValue(WireColor c)
    {
        switch (c)
        {
            case WireColor.Yellow: return Color.yellow;
            case WireColor.Green: return Color.green;
            case WireColor.Blue: return Color.blue;
            case WireColor.Red: return Color.red;
            default: return Color.white;
        }
    }
}