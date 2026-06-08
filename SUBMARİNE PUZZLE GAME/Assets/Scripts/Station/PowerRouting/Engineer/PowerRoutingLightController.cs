using UnityEngine;

public class PowerRoutingLightController : MonoBehaviour
{
    [Header("Visuals")]
    public MeshRenderer meshRenderer;
    public float lightIntensity = 5.0f;

    private Material _runtimeMaterial;

    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private void Awake()
    {
        if (meshRenderer != null)
        {
            _runtimeMaterial = meshRenderer.materials[0];
        }
    }

    public void TurnOn(LightColor color)
    {
        if (_runtimeMaterial == null) return;

        int colorIndex = GetColorIndex(color);
        _runtimeMaterial.SetFloat(LightSelectionProp, colorIndex);
        _runtimeMaterial.SetFloat(IntensityProp, lightIntensity);
    }

    public void TurnOff()
    {
        if (_runtimeMaterial == null) return;

        _runtimeMaterial.SetFloat(IntensityProp, 0.0f);
        _runtimeMaterial.SetFloat(LightSelectionProp, 0);
    }

    private int GetColorIndex(LightColor color)
    {
        return color switch
        {
            LightColor.Red => 1,
            LightColor.Green => 2,
            LightColor.Purple => 3,
            LightColor.Yellow => 4,
            _ => 0
        };
    }
}