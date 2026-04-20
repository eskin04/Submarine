using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SSTechnicianUIManager : MonoBehaviour
{
    [Header("UI")]
    public RectTransform nodeLayer;
    public RectTransform labelLayer;
    public TextMeshProUGUI punchInputText;
    public GameObject LegendPanel;


    [Header("Prefabs")]
    public GameObject nodePrefab;
    public GameObject labelPrefab;

    [Header("Settings")]
    public float cellSize = 50f;

    [Header("Edge Settings")]
    public RectTransform edgeLayer;
    public float edgeThickness = 8f;
    private Color _pathColor = new Color(0f, 0.8f, 1f, 0.8f);
    private Color _visitedNodeColor = new Color(0f, 0.8f, 1f, 1f);

    private List<GameObject> _spawnedEdges = new List<GameObject>();

    private List<GameObject> _spawnedElements = new List<GameObject>();
    private Dictionary<Vector2Int, Image> _nodeImages = new Dictionary<Vector2Int, Image>();

    // Renk Teması
    private Color _defaultNodeColor = new Color(0.2f, 0.4f, 0.2f, 0.3f);
    private Color _targetColor = new Color(1f, 0.5f, 0f, 1f);

    void Start()
    {
        DrawGridAndLabels();
    }

    private Vector2 GetNodeUIPosition(Vector2Int gridPos)
    {
        Vector2 containerCenter = new Vector2(nodeLayer.rect.width / 2f, nodeLayer.rect.height / 2f);
        Vector2 gridOffset = new Vector2(-cellSize * 3.5f, -cellSize * 3.5f);
        return containerCenter + gridOffset + new Vector2(gridPos.x * cellSize, gridPos.y * cellSize);
    }

    public void DrawGridAndLabels()
    {

        Vector2 containerCenter = new Vector2(nodeLayer.rect.width / 2f, nodeLayer.rect.height / 2f);
        Vector2 gridOffset = new Vector2(-cellSize * 3.5f, -cellSize * 3.5f);

        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector2 uiPos = containerCenter + gridOffset + new Vector2(x * cellSize, y * cellSize);

                GameObject nodeObj = Instantiate(nodePrefab, nodeLayer);
                RectTransform rt = nodeObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
                rt.anchoredPosition = uiPos;

                Image img = nodeObj.GetComponent<Image>();
                img.color = _defaultNodeColor;

                _nodeImages[gridPos] = img;
                _spawnedElements.Add(nodeObj);
            }
        }

        float labelMargin = cellSize * 0.8f;

        for (int i = 0; i < 8; i++)
        {
            Vector2 hPos = containerCenter + gridOffset + new Vector2(i * cellSize, -labelMargin);
            CreateLabel(hPos, ((char)('A' + i)).ToString());

            Vector2 vPos = containerCenter + gridOffset + new Vector2(-labelMargin, i * cellSize);
            CreateLabel(vPos, (i + 1).ToString());
        }
    }

    private void CreateLabel(Vector2 pos, string text)
    {
        GameObject labelObj = Instantiate(labelPrefab, labelLayer);
        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = pos;

        TextMeshProUGUI tmp = labelObj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;

        _spawnedElements.Add(labelObj);
    }

    public void SetTarget(Vector2Int targetPos)
    {
        LegendPanel.SetActive(true);
        foreach (var img in _nodeImages.Values)
        {
            img.transform.DOKill();
            img.transform.localScale = Vector3.one;
            img.color = _defaultNodeColor;
        }

        foreach (var edge in _spawnedEdges) Destroy(edge); _spawnedEdges.Clear();


        if (_nodeImages.TryGetValue(targetPos, out Image targetImg))
        {
            targetImg.color = _targetColor;

            targetImg.transform.DOScale(1.5f, 1.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(targetImg.gameObject);
        }


    }

    public void AddPathStep(Vector2Int startPos, Vector2Int endPos)
    {
        if (_nodeImages.TryGetValue(endPos, out Image nodeImg))
        {
            nodeImg.color = _visitedNodeColor;

            nodeImg.transform.DOKill();
            nodeImg.transform.localScale = Vector3.one;
            nodeImg.transform.DOPunchScale(new Vector3(0.5f, 0.5f, 0), 0.3f).SetLink(nodeImg.gameObject);
        }



        if (startPos != endPos)
        {
            Vector2 startUI = GetNodeUIPosition(startPos);
            Vector2 endUI = GetNodeUIPosition(endPos);

            GameObject edgeObj = new GameObject("TechPathEdge", typeof(RectTransform), typeof(Image));
            RectTransform rt = edgeObj.GetComponent<RectTransform>();
            rt.SetParent(edgeLayer, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero;

            Image img = edgeObj.GetComponent<Image>();
            img.color = _pathColor;

            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = startUI;

            Vector2 direction = endUI - startUI;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            rt.sizeDelta = new Vector2(distance, edgeThickness);
            rt.localEulerAngles = new Vector3(0, 0, angle);

            _spawnedEdges.Add(edgeObj);
        }
    }

    public void UpdateInputText(string input)
    {
        if (punchInputText == null) return;

        punchInputText.text = input;

        punchInputText.transform.DOKill();
        punchInputText.transform.localScale = Vector3.one;

        punchInputText.transform.DOPunchScale(new Vector3(0.3f, 0.3f, 0), 0.2f).SetLink(punchInputText.gameObject);
    }




}