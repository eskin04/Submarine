using UnityEngine;
using UnityEngine.UI;

public class LightsOut_StatusLight : MonoBehaviour
{
    public int lightID;

    [Header("Visuals")]
    public MeshRenderer meshRenderer;
    public float lightIntensity = 5.0f;
    private Material runtimeMaterial;


    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private int lastColorIndex = 0;

    private void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[0];
        }
    }

    public void SetState(StatusLightState state)
    {
        switch (state)
        {
            case StatusLightState.Red:
                ApplyVisual(1);
                break;
            case StatusLightState.Yellow:
                ApplyVisual(4);
                break;
            case StatusLightState.Green:
                ApplyVisual(2);
                break;
            default:
                ApplyVisual(0);
                break;
        }
    }

    private void ApplyVisual(int index)
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(LightSelectionProp, index); // 1 = Renk seçimi
            if (lastColorIndex == 0 && index != 0)
            {
                runtimeMaterial.SetFloat(IntensityProp, lightIntensity); // Işık yoğunluğu
            }
            else if (lastColorIndex != 0 && index == 0)
            {
                runtimeMaterial.SetFloat(IntensityProp, 0.0f); // Işığı kapat
            }
            lastColorIndex = index;
        }


    }
}