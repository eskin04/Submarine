using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class LightsOut_WireVisual : MonoBehaviour
{
    [Header("Connections")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Cable Settings")]
    public float cableRadius = 0.03f;
    public int segments = 6;
    public int curveResolution = 20;

    [Header("Physics Settings")]
    public float baseSagAmount = 0.5f;
    [Range(0f, 1f)]
    public float tensionFactor = 0.5f;
    public float sagLimitFactor = 1.2f;

    private Mesh cableMesh;
    private MeshFilter meshFilter;
    private Vector3[] curvePoints;

    private void OnEnable()
    {
        meshFilter = GetComponent<MeshFilter>();
        cableMesh = new Mesh();
        cableMesh.name = "CableMesh";
        meshFilter.mesh = cableMesh;
    }

    private void LateUpdate()
    {
        if (startPoint == null || endPoint == null) return;

        GenerateCurvePoints();
        GenerateTubeMesh();
    }

    public void SetColor(Color color)
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr.sharedMaterial != null)
        {
            mr.material.color = color;
        }
    }

    private void GenerateCurvePoints()
    {
        if (curvePoints == null || curvePoints.Length != curveResolution)
            curvePoints = new Vector3[curveResolution];

        Vector3 p0 = startPoint.position;
        Vector3 p3 = endPoint.position;

        float distance = Vector3.Distance(p0, p3);

        Vector3 p1 = p0 + (startPoint.forward * (distance * 0.3f));

        float targetSag = Mathf.Max(0, baseSagAmount - (distance * tensionFactor));
        float physicalLimit = distance * sagLimitFactor;
        float currentSag = Mathf.Min(targetSag, physicalLimit);

        Vector3 midBase = (p1 + p3) / 2f;
        Vector3 p2 = midBase + (Vector3.down * currentSag);

        p0 = transform.InverseTransformPoint(p0);
        p1 = transform.InverseTransformPoint(p1);
        p2 = transform.InverseTransformPoint(p2);
        p3 = transform.InverseTransformPoint(p3);

        for (int i = 0; i < curveResolution; i++)
        {
            float t = i / (float)(curveResolution - 1);
            curvePoints[i] = CalculateCubicBezierPoint(t, p0, p1, p2, p3);
        }


    }

    private void GenerateTubeMesh()
    {
        if (curvePoints == null || curvePoints.Length < 2) return;

        int vertCount = curveResolution * segments;
        Vector3[] vertices = new Vector3[vertCount];
        int[] triangles = new int[(curveResolution - 1) * segments * 6];

        Vector2[] uvs = new Vector2[vertCount];

        for (int i = 0; i < curveResolution; i++)
        {
            Vector3 forward;
            if (i < curveResolution - 1) forward = (curvePoints[i + 1] - curvePoints[i]).normalized;
            else forward = (curvePoints[i] - curvePoints[i - 1]).normalized;

            Quaternion rotation = Quaternion.LookRotation(forward);

            for (int j = 0; j < segments; j++)
            {
                float angle = j * Mathf.PI * 2 / segments;

                float x = Mathf.Cos(angle) * cableRadius;
                float y = Mathf.Sin(angle) * cableRadius;

                Vector3 ringPos = rotation * new Vector3(x, y, 0);

                int vertIndex = i * segments + j;
                vertices[vertIndex] = curvePoints[i] + ringPos;

                float u = j / (float)(segments - 1);
                float v = i / (float)(curveResolution - 1);
                uvs[vertIndex] = new Vector2(u, v);
            }
        }

        int triIndex = 0;
        for (int i = 0; i < curveResolution - 1; i++)
        {
            for (int j = 0; j < segments; j++)
            {
                int current = i * segments + j;
                int next = current + segments;

                int nextJ = (j + 1) % segments;
                int currentRight = i * segments + nextJ;
                int nextRight = currentRight + segments;

                triangles[triIndex++] = current;
                triangles[triIndex++] = next;
                triangles[triIndex++] = currentRight;

                triangles[triIndex++] = next;
                triangles[triIndex++] = nextRight;
                triangles[triIndex++] = currentRight;
            }
        }

        cableMesh.Clear();
        cableMesh.vertices = vertices;
        cableMesh.triangles = triangles;
        cableMesh.uv = uvs;
        cableMesh.RecalculateNormals();
    }

    private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }


}