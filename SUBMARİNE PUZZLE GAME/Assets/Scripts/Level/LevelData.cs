using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/LevelData")]
public class LevelData : ScriptableObject
{
    public int levelID;
    public int mainStationCount;
    public int utilityStationCount;
    public List<StationConfig> stationConfigs;

}
[System.Serializable]
public struct StationConfig
{
    public StationTier stationTier;
    public float TierRatio;
}


