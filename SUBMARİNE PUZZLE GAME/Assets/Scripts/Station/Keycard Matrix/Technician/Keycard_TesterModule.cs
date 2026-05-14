using UnityEngine;

public class Keycard_TesterModule : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public float lightIntensity = 5.0f;
    private Material runtimeMaterial;


    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");


    private void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[0];
        }
        UpdateLightState(0);
    }

    public void UpdateLightState(int status)
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.SetFloat(LightSelectionProp, status);
        if (status == 0)
        {
            runtimeMaterial.SetFloat(IntensityProp, 0f);
            return;
        }
        runtimeMaterial.SetFloat(IntensityProp, lightIntensity);

    }
}