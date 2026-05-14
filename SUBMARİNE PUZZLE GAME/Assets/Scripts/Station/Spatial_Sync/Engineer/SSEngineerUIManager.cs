using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SSEngineerUIManager : MonoBehaviour
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
    public GameObject legendPanel;

    [Header("Radar Settings")]
    public RectTransform coordinateLayer;
    public float radarLineThickness = 1f;
    public Color radarColor = new Color(0f, 0.8f, 0.2f, 0.3f);


    [Header("Radar Animation Settings")]
    public float gridMaxAlpha = 0.5f;
    public float gridFadeInTime = 0.5f;
    public float gridVisibleTime = 1.5f;
    public float maxgridWaitTime = 8f;
    public float mingridWaitTime = 5f;

    private CanvasGroup _radarCanvasGroup;


    private List<GameObject> _spawnedNodes = new List<GameObject>();
    private List<GameObject> _spawnedEdges = new List<GameObject>();

    private Color _lineColor = new Color(0.1f, 0.7f, 0.2f, 0.8f);
    private Color _refColor = Color.cyan;
    private Color _nodeColor = new Color(0.2f, 0.8f, 0.2f);

    void Start()
    {
        GenerateAndAnimateRadarGrid();
    }

    private void GenerateAndAnimateRadarGrid()
    {
        GameObject radarLayerObj = new GameObject("RadarBackground_Auto", typeof(RectTransform), typeof(CanvasGroup));
        radarLayerObj.transform.SetParent(coordinateLayer, false);

        RectTransform radarLayerRT = radarLayerObj.GetComponent<RectTransform>();
        radarLayerRT.anchorMin = Vector2.zero;
        radarLayerRT.anchorMax = Vector2.one;
        radarLayerRT.offsetMin = Vector2.zero;
        radarLayerRT.offsetMax = Vector2.zero;

        _radarCanvasGroup = radarLayerObj.GetComponent<CanvasGroup>();
        _radarCanvasGroup.alpha = 0f;

        Vector2 containerCenter = new Vector2(radarLayerRT.rect.width / 2f, radarLayerRT.rect.height / 2f);
        Vector2 gridOffset = new Vector2(-cellSize * 3.5f, -cellSize * 3.5f);

        float totalLength = 7 * cellSize;

        for (int i = 0; i < 8; i++)
        {
            Vector2 hPos = containerCenter + gridOffset + new Vector2(0, i * cellSize);
            CreateRadarLine(radarLayerRT, hPos, new Vector2(totalLength, radarLineThickness), new Vector2(0f, 0.5f));

            Vector2 vPos = containerCenter + gridOffset + new Vector2(i * cellSize, 0);
            CreateRadarLine(radarLayerRT, vPos, new Vector2(radarLineThickness, totalLength), new Vector2(0.5f, 0f));

            for (int j = 0; j < 8; j++)
            {
                Vector2 nodePos = containerCenter + gridOffset + new Vector2(i * cellSize, j * cellSize);

                GameObject nodeObj = Instantiate(nodePrefab, radarLayerRT);

                RectTransform rt = nodeObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = nodePos;



                Image img = nodeObj.GetComponent<Image>();
                if (img != null)
                {
                    img.color = radarColor;
                }
            }
        }

        Sequence radarSeq = DOTween.Sequence();
        float gridWaitTime = Random.Range(mingridWaitTime, maxgridWaitTime);
        radarSeq.AppendInterval(gridWaitTime);
        radarSeq.Append(_radarCanvasGroup.DOFade(gridMaxAlpha, gridFadeInTime).SetEase(Ease.InOutSine));
        radarSeq.AppendInterval(gridVisibleTime);
        radarSeq.Append(_radarCanvasGroup.DOFade(0f, gridFadeInTime).SetEase(Ease.InOutSine));

        radarSeq.SetLoops(-1);
        radarSeq.SetLink(radarLayerObj);
    }

    private void CreateRadarLine(Transform parent, Vector2 pos, Vector2 size, Vector2 pivot)
    {
        GameObject elementObj = new GameObject("RadarLine", typeof(RectTransform), typeof(Image));
        elementObj.transform.SetParent(parent, false);

        RectTransform rt = elementObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = elementObj.GetComponent<Image>();
        img.color = radarColor;
        img.raycastTarget = false;
    }




    public void DrawCircuit(Dictionary<Vector2Int, List<Vector2Int>> graph, Vector2Int refPoint)
    {
        ClearUI();
        legendPanel.SetActive(true);
        if (graph.Count == 0) return;

        if (legendText != null)
        {
            legendText.text = "Reference: " + GetCoordinateString(refPoint);
        }

        Vector2 containerCenter = new Vector2(nodeLayer.rect.width / 2f, nodeLayer.rect.height / 2f);
        Vector2 gridOffset = new Vector2(-cellSize * 3.5f, -cellSize * 3.5f);

        List<Vector2Int> drawnNodes = new List<Vector2Int>();
        HashSet<string> drawnEdges = new HashSet<string>();


        foreach (var kvp in graph)
        {
            Vector2Int startNode = kvp.Key;

            Vector2 startUI = containerCenter + gridOffset + new Vector2(startNode.x * cellSize, startNode.y * cellSize);

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
                    Vector2 endUI = containerCenter + gridOffset + new Vector2(endNode.x * cellSize, endNode.y * cellSize);

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