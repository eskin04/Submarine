using UnityEngine;

[System.Serializable]
public class CircuitBranch
{
    public Vector2Int[] nodes;
}

[CreateAssetMenu(fileName = "NewSpatialPattern", menuName = "Puzzle/Spatial Pattern")]
public class SpatialPattern : ScriptableObject
{
    public CircuitBranch[] branches;
}