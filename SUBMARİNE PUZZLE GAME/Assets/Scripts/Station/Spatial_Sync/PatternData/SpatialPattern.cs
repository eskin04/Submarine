using UnityEngine;

// Her bir dalı (çizgiyi) temsil eden yapı
[System.Serializable]
public class CircuitBranch
{
    [Tooltip("Bu dala ait noktaların koordinatları. Örn: (0,0), (1,0)")]
    public Vector2Int[] nodes;
}

[CreateAssetMenu(fileName = "NewSpatialPattern", menuName = "Puzzle/Spatial Pattern")]
public class SpatialPattern : ScriptableObject
{
    [Tooltip("Deseni oluşturan tüm dallar/yollar")]
    public CircuitBranch[] branches;
}