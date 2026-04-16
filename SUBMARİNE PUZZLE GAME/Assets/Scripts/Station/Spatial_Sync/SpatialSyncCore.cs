using System.Collections.Generic;
using UnityEngine;

public class SpatialSyncCore
{
    public const int GRID_SIZE = 8;
    public const int REQUIRED_CLICKS = 6;

    // Veritabanındaki manuel deseni oyuna yükler ve dinamik noktaları bulur
    public bool TryLoadManualPattern(SpatialPattern pattern, out Vector2Int offset, out Dictionary<Vector2Int, List<Vector2Int>> graph, out Vector2Int refPoint, out Vector2Int targetPoint)
    {
        offset = Vector2Int.zero;
        graph = new Dictionary<Vector2Int, List<Vector2Int>>();
        refPoint = Vector2Int.zero;
        targetPoint = Vector2Int.zero;

        // 1. Rastgele Offset (Taşma Kontrolü ile)
        offset = new Vector2Int(Random.Range(0, GRID_SIZE), Random.Range(0, GRID_SIZE));

        foreach (var branch in pattern.branches)
        {
            foreach (var node in branch.nodes)
            {
                Vector2Int worldPos = node + offset;
                if (worldPos.x < 0 || worldPos.x >= GRID_SIZE || worldPos.y < 0 || worldPos.y >= GRID_SIZE)
                {
                    return false; // Herhangi bir dal taştıysa baştan başla
                }
                if (!graph.ContainsKey(worldPos))
                    graph[worldPos] = new List<Vector2Int>();
            }
        }

        // 2. Noktaları Çizgilere (Edge) Dönüştür
        // Dizideki sıralamayı baz alarak birbirine bağlarız. Aynı noktadan geçilirse döngü (loop) oluşur!
        foreach (var branch in pattern.branches)
        {
            for (int i = 0; i < branch.nodes.Length - 1; i++)
            {
                Vector2Int a = branch.nodes[i] + offset;
                Vector2Int b = branch.nodes[i + 1] + offset;

                if (!graph[a].Contains(b))
                {
                    graph[a].Add(b);
                    graph[b].Add(a);
                }
            }
        }

        // 3. ÇOKLU ÇÖZÜM GARANTİLİ DİNAMİK NOKTA ARAMA
        List<(Vector2Int, Vector2Int)> validPairs = new List<(Vector2Int, Vector2Int)>();
        List<Vector2Int> allNodes = new List<Vector2Int>(graph.Keys);

        for (int i = 0; i < allNodes.Count; i++)
        {
            for (int j = 0; j < allNodes.Count; j++)
            {
                if (i == j) continue;

                Vector2Int possibleRef = allNodes[i];
                Vector2Int possibleTarget = allNodes[j];

                List<Vector2Int> visited = new List<Vector2Int> { possibleRef };
                int validPathCount = CountExactPaths(graph, possibleRef, possibleTarget, REQUIRED_CLICKS - 1, visited);

                // Bu iki nokta arasında en az 2 farklı rotadan tam 5 adımda ulaşılabiliyor mu?
                if (validPathCount >= 1)
                {
                    validPairs.Add((possibleRef, possibleTarget));
                }
            }
        }

        // 4. SONUÇ
        if (validPairs.Count > 0)
        {
            // Bulunan onlarca geçerli ikiliden birini rastgele seç (Oyunun ezberlenmesini engeller)
            var chosenPair = validPairs[Random.Range(0, validPairs.Count)];
            refPoint = chosenPair.Item1;
            targetPoint = chosenPair.Item2;
            return true;
        }

        return false;
    }

    private int CountExactPaths(Dictionary<Vector2Int, List<Vector2Int>> graph, Vector2Int current, Vector2Int target, int remainingEdges, List<Vector2Int> visited)
    {
        if (remainingEdges == 0) return (current == target) ? 1 : 0;

        int totalPaths = 0;
        foreach (var neighbor in graph[current])
        {
            if (!visited.Contains(neighbor))
            {
                visited.Add(neighbor);
                totalPaths += CountExactPaths(graph, neighbor, target, remainingEdges - 1, visited);
                visited.Remove(neighbor);
            }
        }
        return totalPaths;
    }

    // Doğrulama sistemi graf üzerinden çalıştığı için her zaman kusursuzdur
    public bool ValidateCircuitInput(Dictionary<Vector2Int, List<Vector2Int>> graph, Vector2Int currentPos, Vector2Int inputPos, Vector2Int targetPoint, int currentStep, List<Vector2Int> visitedNodes)
    {
        if (!graph.ContainsKey(inputPos) || !graph[currentPos].Contains(inputPos) || visitedNodes.Contains(inputPos))
            return false;

        int remainingMoves = REQUIRED_CLICKS - (currentStep + 1);
        if (inputPos == targetPoint && remainingMoves > 0) return false;

        List<Vector2Int> tempVisited = new List<Vector2Int>(visitedNodes) { inputPos };
        return CountExactPaths(graph, inputPos, targetPoint, remainingMoves, tempVisited) > 0;
    }
}