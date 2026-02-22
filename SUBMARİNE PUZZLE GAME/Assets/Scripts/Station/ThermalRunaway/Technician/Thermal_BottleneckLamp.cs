using UnityEngine;

public class Thermal_BottleneckLamp : MonoBehaviour
{
    [Header("Visuals")]
    public int colorID;
    public MeshRenderer meshRenderer;
    public float lightIntensity = 5.0f;

    private Material runtimeMaterial;
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");
    private bool isOn = true;

    private void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[0];
            TurnOff();
        }
    }

    public void TurnOn()
    {
        if (isOn) return;
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(IntensityProp, lightIntensity);
            isOn = true;
        }
    }

    public void TurnOff()
    {
        if (!isOn) return;
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(IntensityProp, 0.0f);
            isOn = false;
        }
    }
}