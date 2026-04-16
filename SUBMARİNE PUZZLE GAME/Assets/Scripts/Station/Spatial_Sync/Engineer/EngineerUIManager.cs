using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EngineerUIManager : MonoBehaviour
{
    [Header("UI Katmanları")]
    public RectTransform nodeLayer;
    public RectTransform edgeLayer;

    [Header("Prefab'ler")]
    public GameObject nodePrefab;

    [Header("Izgara Ayarları")]
    public float cellSize = 100f;
    public float edgeThickness = 6f;


    private List<GameObject> _spawnedNodes = new List<GameObject>();
    private List<GameObject> _spawnedEdges = new List<GameObject>();

    private Color _lineColor = new Color(0.1f, 0.7f, 0.2f, 0.8f);
    private Color _refColor = Color.cyan;
    private Color _nodeColor = new Color(0.2f, 0.8f, 0.2f);

    private Vector2 GetUIPosition(Vector2Int gridPos)
    {
        float x = (gridPos.x * cellSize) + (cellSize / 2f);
        float y = (gridPos.y * cellSize) + (cellSize / 2f);
        return new Vector2(x, y);
    }

    public void DrawCircuit(Dictionary<Vector2Int, List<Vector2Int>> graph, Vector2Int refPoint)
    {
        ClearUI();

        List<Vector2Int> drawnNodes = new List<Vector2Int>();
        HashSet<string> drawnEdges = new HashSet<string>();

        foreach (var kvp in graph)
        {
            Vector2Int startNode = kvp.Key;

            if (!drawnNodes.Contains(startNode))
            {
                DrawNode(startNode, refPoint);
                drawnNodes.Add(startNode);
            }

            foreach (var endNode in kvp.Value)
            {
                string edgeID1 = $"{startNode}-{endNode}";
                string edgeID2 = $"{endNode}-{startNode}";

                if (!drawnEdges.Contains(edgeID1) && !drawnEdges.Contains(edgeID2))
                {
                    DrawEdgeWithImage(startNode, endNode);
                    drawnEdges.Add(edgeID1);
                }
            }
        }
    }

    private void DrawNode(Vector2Int gridPos, Vector2Int refPoint)
    {
        GameObject nodeObj = Instantiate(nodePrefab, nodeLayer);
        RectTransform rt = nodeObj.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.anchoredPosition = GetUIPosition(gridPos);

        Image img = nodeObj.GetComponent<Image>();
        TextMeshProUGUI textComp = nodeObj.GetComponentInChildren<TextMeshProUGUI>();

        Transform highlightTransform = nodeObj.transform.Find("HighlightBG");
        GameObject highlightObj = highlightTransform != null ? highlightTransform.gameObject : null;
        // -----------------------------------------------------------------------

        // 1. EĞER BU NOKTA REFERANS (BAŞLANGIÇ) NOKTASIYSA
        if (gridPos == refPoint)
        {
            img.color = _refColor;

            if (textComp != null)
                textComp.text = GetCoordinateString(gridPos);

            if (highlightObj != null)
                highlightObj.SetActive(true); // Vurgu arka planını aç
        }
        // 2. DİĞER TÜM NOKTALAR
        else
        {
            img.color = _nodeColor;

            if (textComp != null)
                textComp.text = ""; // Yazıyı sil

            if (highlightObj != null)
                highlightObj.SetActive(false); // Vurgu arka planını kapat
        }

        _spawnedNodes.Add(nodeObj);
    }

    // --- YENİ ÇİZGİ FONKSİYONU (PurrNet Hatasını ve Materyal Sorununu Çözer) ---
    private void DrawEdgeWithImage(Vector2Int startGridPos, Vector2Int endGridPos)
    {
        Vector2 startUI = GetUIPosition(startGridPos);
        Vector2 endUI = GetUIPosition(endGridPos);

        // 1. Yeni bir GameObject oluştur
        GameObject edgeObj = new GameObject("EdgeImage", typeof(RectTransform), typeof(Image));
        RectTransform rt = edgeObj.GetComponent<RectTransform>();

        // 2. Parent ata (Layer hiyerarşisi bozulmasın diye 'false' ile)
        rt.SetParent(edgeLayer, false);

        rt.anchorMin = Vector2.zero; // X:0, Y:0
        rt.anchorMax = Vector2.zero; // X:0, Y:0

        // 3. Image bileşenini ayarla (Standart Unlit/UI materyali kullanır)
        Image img = edgeObj.GetComponent<Image>();
        img.color = _lineColor;
        // İstersen buraya bir Sprite atayabilirsin: img.sprite = myGlowSprite;

        // 4. Çizginin Pivot noktasını Sol-Orta (0, 0.5) yap ki uzarken sadece ileriye gitsin
        rt.pivot = new Vector2(0f, 0.5f);

        // 5. Çizgiyi başlangıç noktasına yerleştir
        rt.anchoredPosition = startUI;

        // 6. Mesafe (Uzunluk) ve Açı (Rotation) Hesaplama
        Vector2 direction = endUI - startUI;
        float distance = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 7. Genişliği mesafe kadar uzat, açıyı döndür
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

    // Backend koordinatını Harf ve Sayıya çevirir (Örn: 0,0 -> A1)
    private string GetCoordinateString(Vector2Int gridPos)
    {
        char letter = (char)('A' + gridPos.x); // X=0 için 'A', X=1 için 'B'
        int number = gridPos.y + 1;            // Y=0 için '1', Y=1 için '2'
        return $"{letter}{number}";
    }
}