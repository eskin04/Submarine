using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : MaskableGraphic
{
    public float thickness = 5f;
    public List<Vector2> points = new List<Vector2>();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        if (points.Count < 2) return;

        float width = thickness / 2;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];

            Vector2 dir = (p2 - p1).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * width;

            UIVertex v1 = CreateVertex(p1 + normal);
            UIVertex v2 = CreateVertex(p1 - normal);
            UIVertex v3 = CreateVertex(p2 - normal);
            UIVertex v4 = CreateVertex(p2 + normal);

            vh.AddUIVertexQuad(new UIVertex[] { v1, v2, v3, v4 });
        }
    }

    private UIVertex CreateVertex(Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.position = position;
        vertex.color = color;
        return vertex;
    }

    public void SetPoints(List<Vector2> newPoints)
    {
        points = newPoints;
        SetVerticesDirty();
    }
}