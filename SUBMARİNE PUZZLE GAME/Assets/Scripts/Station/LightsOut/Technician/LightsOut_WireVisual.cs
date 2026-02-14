using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class LightsOut_WireVisual : MonoBehaviour
{
    [Header("Settings")]
    public Transform startPoint;
    public Transform endPoint;
    public int segmentCount = 20;

    [Header("Physics Settings")]
    public float baseSagAmount = .5f;
    [Range(0f, 1f)]
    public float tensionFactor = 0.8f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = segmentCount;

    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;
        DrawBezierCurve();
    }

    public void SetColor(Color color)
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = color;
        }
    }

    private void DrawBezierCurve()
    {
        Vector3 p0 = startPoint.position;
        Vector3 p2 = endPoint.position;

        float distance = Vector3.Distance(p0, p2);

        float currentSag = Mathf.Max(0, baseSagAmount - (distance * tensionFactor));

        Vector3 midPoint = (p0 + p2) / 2f;
        Vector3 p1 = midPoint + (Vector3.down * currentSag);

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);
            Vector3 pixel = CalculateQuadraticBezierPoint(t, p0, p1, p2);
            lineRenderer.SetPosition(i, pixel);
        }
    }

    private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;

        Vector3 p = (uu * p0) + (2 * u * t * p1) + (tt * p2);
        return p;
    }
}