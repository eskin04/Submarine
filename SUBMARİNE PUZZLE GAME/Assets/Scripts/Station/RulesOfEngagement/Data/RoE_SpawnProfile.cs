using UnityEngine;

[CreateAssetMenu(fileName = "New Spawn Profile", menuName = "RoE/Spawn Profile")]
public class RoE_SpawnProfile : ScriptableObject
{
    [Header("Profile Identity")]
    public string profileName = "General Threat";

    [Header("Probability")]
    [Range(0, 100)]
    public float spawnChance = 10f;

    [Header("Distance Settings")]
    public float minDistance = 500f;
    public float maxDistance = 1000f;

    [Header("Speed Settings")]
    public float minSpeed = 5f;
    public float maxSpeed = 15f;
}