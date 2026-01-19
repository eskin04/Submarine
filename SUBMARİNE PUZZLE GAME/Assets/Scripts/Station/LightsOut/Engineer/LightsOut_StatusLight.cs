using UnityEngine;
using UnityEngine.UI;

public class LightsOut_StatusLight : MonoBehaviour
{
    public int lightID;

    [Header("Visuals")]
    public MeshRenderer meshRenderer;
    public Image uiImage;

    [Header("Material / Colors")]
    public Material matRed;
    public Material matYellow;
    public Material matGreen;
    public Material matOff;

    private Color colorRed = Color.red;
    private Color colorYellow = Color.yellow;
    private Color colorGreen = Color.green;
    private Color colorOff = new Color(0.2f, 0, 0);

    public void SetState(StatusLightState state)
    {
        switch (state)
        {
            case StatusLightState.Red:
                ApplyVisual(matRed, colorRed);
                break;
            case StatusLightState.Yellow:
                ApplyVisual(matYellow, colorYellow);
                break;
            case StatusLightState.Green:
                ApplyVisual(matGreen, colorGreen);
                break;
            default:
                ApplyVisual(matOff, colorOff);
                break;
        }
    }

    private void ApplyVisual(Material mat, Color col)
    {
        if (meshRenderer != null)
        {
            meshRenderer.material = mat;
        }

        if (uiImage != null)
        {
            uiImage.color = col;
        }
    }
}