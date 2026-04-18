using UnityEngine;

public class StatusLightController : MonoBehaviour
{
    [Header("Light Meshes")]
    public MeshRenderer[] lightMeshes;

    [Header("Shader Settings")]
    public int redIndex = 0;
    public int greenIndex = 1;
    public float activeIntensity = 2.0f;
    public float passiveIntensity = 0.5f;

    private static readonly int ColorIndexProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");
    private MaterialPropertyBlock _propBlock;

    void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        SpatialSyncNetworkManager.OnStepCorrect += UpdateLights;
        SpatialSyncNetworkManager.OnPuzzleReset += ResetAllLights;
        SpatialSyncNetworkManager.OnStationStarted += ResetAllLights;
    }

    private void OnDisable()
    {
        SpatialSyncNetworkManager.OnStepCorrect -= UpdateLights;
        SpatialSyncNetworkManager.OnPuzzleReset -= ResetAllLights;
        SpatialSyncNetworkManager.OnStationStarted -= ResetAllLights;
    }



    public void UpdateLights(int correctCount)
    {
        for (int i = 0; i < lightMeshes.Length; i++)
        {
            if (lightMeshes[i] == null) continue;

            lightMeshes[i].GetPropertyBlock(_propBlock);

            if (i < correctCount)
            {
                _propBlock.SetInt(ColorIndexProp, greenIndex);
                _propBlock.SetFloat(IntensityProp, activeIntensity);
            }
            else
            {
                _propBlock.SetInt(ColorIndexProp, redIndex);
                _propBlock.SetFloat(IntensityProp, passiveIntensity);
            }

            lightMeshes[i].SetPropertyBlock(_propBlock);
        }
    }

    public void ResetAllLights()
    {
        UpdateLights(0);
    }
}