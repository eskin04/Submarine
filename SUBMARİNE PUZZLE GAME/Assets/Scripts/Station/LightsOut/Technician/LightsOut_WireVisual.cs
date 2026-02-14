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

        // Kablo kalınlığını buradan veya editörden ayarlayabilirsin
        // lineRenderer.startWidth = 0.05f;
        // lineRenderer.endWidth = 0.05f;
    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;
        DrawBezierCurve();
    }

    public void SetColor(Color color)
    {
        if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();

        // LineRenderer'ın materyal rengini değiştiriyoruz
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        // Materyalin kendisine de renk atıyoruz (Emission vs. için)
        if (lineRenderer.material != null)
        {
            lineRenderer.material.color = color;
            // Eğer HDR/Emission kullanıyorsan:
            // lineRenderer.material.SetColor("_EmissionColor", color);
        }
    }

    private void DrawBezierCurve()
    {
        Vector3 p0 = startPoint.position;
        Vector3 p2 = endPoint.position;

        // İki nokta arasındaki mesafe
        float distance = Vector3.Distance(p0, p2);

        // Mesafe arttıkça sarkma (sag) azalır (Kablo gerilir)
        float currentSag = Mathf.Max(0, baseSagAmount - (distance * tensionFactor));

        // Orta noktanın biraz aşağısı (Sarkma noktası)
        // Vector3.down, dünyada "aşağı" demektir.
        Vector3 midPoint = (p0 + p2) / 2f;
        Vector3 p1 = midPoint + (Vector3.down * currentSag);

        // Bezier eğrisini çiz
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