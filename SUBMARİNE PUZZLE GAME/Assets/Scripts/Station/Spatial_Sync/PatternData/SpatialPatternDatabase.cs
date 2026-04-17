using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatternDatabase", menuName = "Puzzle/Pattern Database")]
public class SpatialPatternDatabase : ScriptableObject
{
    [SerializeField] private List<SpatialPattern> patterns = new List<SpatialPattern>();

    public int PatternCount => patterns.Count;

    public bool TryGetPattern(int id, out SpatialPattern pattern)
    {
        if (id >= 0 && id < patterns.Count)
        {
            pattern = patterns[id];
            return true;
        }

        pattern = null;
        Debug.LogWarning($"[SpatialPatternDatabase] ID {id} bulunamadı! Liste sınırları dışında.");
        return false;
    }
}