using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class EngineerUIManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform nodeLayer;
    public RectTransform edgeLayer;

    [Header("Prefabs")]
    public GameObject nodePrefab;

    [Header("Graph Settings")]
    public float cellSize = 100f;
    public float edgeThickness = 6f;

    public TextMeshProUGUI legendText;


    private List<GameObject> _spawnedNodes = new List<GameObject>();
    private List<GameObject> _spawnedEdges = new List<GameObject>();

    private Color _lineColor = new Color(0.1f, 0.7f, 0.2f, 0.8f);
    private Color _refColor = Color.cyan;
    private Color _nodeColor = new Color(0.2f, 0.8f, 0.2f);


    public void DrawCircuit(Dictionary<Vector2Int, List<Vector2Int>> graph, Vector2Int refPoint)
    {
        ClearUI();

        if (graph.Count == 0) return;

        Vector2 minGrid = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxGrid = new Vector2(float.MinValue, float.MinValue);

        foreach (var node in graph.Keys)
        {
            if (node.x < minGrid.x) minGrid.x = node.x;
            if (node.x > maxGrid.x) maxGrid.x = node.x;
            if (node.y < minGrid.y) minGrid.y = node.y;
            if (node.y > maxGrid.y) maxGrid.y = node.y;
        }

        Vector2 patternCenter = (minGrid + maxGrid) / 2f;

        Vector2 containerCenter = new Vector2(nodeLayer.rect.width / 2f, nodeLayer.rect.height / 2f);

        if (legendText != null)
        {
            legendText.text = "Reference: " + GetCoordinateString(refPoint);
        }

        List<Vector2Int> drawnNodes = new List<Vector2Int>();
        HashSet<string> drawnEdges = new HashSet<string>();

        foreach (var kvp in graph)
        {
            Vector2Int startNode = kvp.Key;

            Vector2 startUI = ((Vector2)startNode - patternCenter) * cellSize + containerCenter;

            if (!drawnNodes.Contains(startNode))
            {
                DrawNodeAtPosition(startUI, startNode, refPoint);
                drawnNodes.Add(startNode);
            }

            foreach (var endNode in kvp.Value)
            {
                string edgeID1 = $"{startNode}-{endNode}";
                string edgeID2 = $"{endNode}-{startNode}";

                if (!drawnEdges.Contains(edgeID1) && !drawnEdges.Contains(edgeID2))
                {
                    Vector2 endUI = ((Vector2)endNode - patternCenter) * cellSize + containerCenter;

                    DrawEdgeBetweenPoints(startUI, endUI);
                    drawnEdges.Add(edgeID1);
                }
            }
        }
    }

    private void DrawNodeAtPosition(Vector2 uiPos, Vector2Int gridPos, Vector2Int refPoint)
    {
        GameObject nodeObj = Instantiate(nodePrefab, nodeLayer);
        RectTransform rt = nodeObj.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;

        rt.anchoredPosition = uiPos;

        Image img = nodeObj.GetComponent<Image>();

        if (gridPos == refPoint)
        {
            img.color = _refColor;
            img.transform.localScale = Vector3.one;

            img.transform.DOScale(1.5f, 1.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(nodeObj);
        }
        else img.color = _nodeColor;



        _spawnedNodes.Add(nodeObj);
    }

    private void DrawEdgeBetweenPoints(Vector2 startUI, Vector2 endUI)
    {
        GameObject edgeObj = new GameObject("EdgeImage", typeof(RectTransform), typeof(Image));
        RectTransform rt = edgeObj.GetComponent<RectTransform>();

        rt.SetParent(edgeLayer, false);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;

        Image img = edgeObj.GetComponent<Image>();
        img.color = _lineColor;

        rt.pivot = new Vector2(0f, 0.5f);

        rt.anchoredPosition = startUI;

        Vector2 direction = endUI - startUI;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rt.sizeDelta = new Vector2(distance, edgeThickness);
        rt.localEulerAngles = new Vector3(0, 0, angle);

        _spawnedEdges.Add(edgeObj);
    }


    public void ClearUI()
    {
        foreach (var node in _spawnedNodes) Destroy(node);
        foreach (var edge in _spawnedEdges) Destroy(edge);
        _spawnedNodes.Clear();
        _spawnedEdges.Clear();
    }

    private string GetCoordinateString(Vector2Int gridPos)
    {
        char letter = (char)('A' + gridPos.x);
        int number = gridPos.y + 1;
        return $"{letter}{number}";
    }
}