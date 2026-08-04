using UnityEngine;
using PurrNet;

public class WaterLevelVisualizer : MonoBehaviour
{
    [Header("Height Settings")]
    public float minHeight = -2.0f;

    public float maxHeight = 3.0f;

    [Header("Animation Settings")]
    public float smoothSpeed = 2.0f;
    public GameObject waterMesh;

    private float targetY;

    private void Awake()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, minHeight, transform.localPosition.z);
        waterMesh.SetActive(false);
    }

    private void Update()
    {
        float currentWaterLevel = GetWaterLevel();
        if (currentWaterLevel <= 0f) return;
        if (!waterMesh.activeSelf)
        {
            waterMesh.SetActive(true);
        }

        targetY = Mathf.Lerp(minHeight, maxHeight, currentWaterLevel / 100f);

        Vector3 newPos = transform.localPosition;
        newPos.y = Mathf.Lerp(newPos.y, targetY, Time.deltaTime * smoothSpeed);

        transform.localPosition = newPos;
    }



    private float GetWaterLevel()
    {
        if (InstanceHandler.TryGetInstance<FloodManager>(out FloodManager floodManager))
        {
            return floodManager.GetCurrentWaterLevel();
        }

        return 0f;
    }
}