using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PatternDatabase", menuName = "Puzzle/Pattern Database")]
public class SpatialPatternDatabase : ScriptableObject
{
    [Tooltip("Oyundaki tüm desenlerin listesi")]
    [SerializeField] private List<SpatialPattern> patterns = new List<SpatialPattern>();

    // Toplam desen sayısını döndürür (Rastgele seçim için NetworkManager'da kullanılır)
    public int PatternCount => patterns.Count;

    // İstenilen ID'ye sahip deseni güvenli bir şekilde döndürür
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