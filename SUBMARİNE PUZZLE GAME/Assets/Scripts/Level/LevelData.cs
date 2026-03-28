using System.Collections.Generic;
using UnityEngine;
using PurrNet;

[CreateAssetMenu(fileName = "LevelData", menuName = "Level/LevelData")]
public class LevelData : ScriptableObject
{
    [PurrScene] public string CurrentScene;
    public int levelID;
    public int mainStationCount;
    public List<StationConfig> stationConfigs;

}
[System.Serializable]
public struct StationConfig
{
    public StationTier stationTier;
    public float TierRatio;
}


