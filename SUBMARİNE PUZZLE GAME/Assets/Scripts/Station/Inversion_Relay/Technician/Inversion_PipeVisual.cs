using UnityEngine;
using TMPro;

public class Inversion_PipeVisual : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro letterText;
    public MeshRenderer lightRenderer;

    [Header("Settings")]
    public float lightIntensity = 5.0f;

    private Material runtimeMaterial;
    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private void Awake()
    {
        if (lightRenderer != null)
        {
            runtimeMaterial = lightRenderer.materials[0];
            SetLight(0);
        }
    }

    public void SetLetter(PipeLetter letter)
    {
        if (letterText != null)
            letterText.text = letter.ToString();
    }

    public void SetLight(int status)
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.SetFloat(LightSelectionProp, status);

        if (status == 0)
        {
            runtimeMaterial.SetFloat(IntensityProp, 0f);
        }
        else
        {
            runtimeMaterial.SetFloat(IntensityProp, lightIntensity);
        }
    }
}