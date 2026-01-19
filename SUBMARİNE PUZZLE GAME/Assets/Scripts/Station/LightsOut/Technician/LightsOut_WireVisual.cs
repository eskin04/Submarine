using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(UILineRenderer))]
public class LightsOut_WireVisual : MonoBehaviour
{
    [Header("Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public int segmentCount = 20;

    [Header("Physics Settings")]
    public float baseSagAmount = 150f;
    [Range(0f, 1f)]
    public float tensionFactor = 0.5f;

    private UILineRenderer uiLine;
    private RectTransform rectTransform;

    private void Awake()
    {
        uiLine = GetComponent<UILineRenderer>();
        rectTransform = GetComponent<RectTransform>();
        uiLine.raycastTarget = false;
    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;
        DrawBezierCurve();
    }

    public void SetColor(Color color)
    {
        if (uiLine == null) uiLine = GetComponent<UILineRenderer>();
        uiLine.color = color;
    }

    private void DrawBezierCurve()
    {
        Vector2 p0, p2;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            RectTransformUtility.WorldToScreenPoint(null, startPoint.position),
            null,
            out p0
        );

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform,
            RectTransformUtility.WorldToScreenPoint(null, endPoint.position),
            null,
            out p2
        );


        float distance = Vector2.Distance(p0, p2);

        float currentSag = Mathf.Max(0, baseSagAmount - (distance * tensionFactor));


        Vector2 midPoint = (p0 + p2) / 2f;
        Vector2 p1 = midPoint + (Vector2.down * currentSag);

        List<Vector2> pointList = new List<Vector2>();

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector2 pixel = CalculateQuadraticBezierPoint(t, p0, p1, p2);
            pointList.Add(pixel);
        }

        uiLine.SetPoints(pointList);
    }

    private Vector2 CalculateQuadraticBezierPoint(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        return (uu * p0) + (2 * u * t * p1) + (tt * p2);
    }
}