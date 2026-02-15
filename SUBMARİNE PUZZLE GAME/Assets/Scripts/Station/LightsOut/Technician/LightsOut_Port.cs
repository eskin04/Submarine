using UnityEngine;

public class LightsOut_Port : MonoBehaviour
{
    public int portID;

    [Header("Visuals")]
    [SerializeField] private MeshRenderer statusLightRenderer;
    [SerializeField] private int materialIndex = 0;
    [SerializeField] private float lightIntensity = 5.0f;

    public Transform snapPoint;


    private static readonly int LightSelectionProp = Shader.PropertyToID("_ColorIndex");
    private static readonly int IntensityProp = Shader.PropertyToID("_LightIntensity");

    private Material runtimeMaterial;

    private void Awake()
    {
        if (statusLightRenderer != null)
        {
            runtimeMaterial = statusLightRenderer.materials[materialIndex];
            TurnOffLight();
        }
    }



    public void SetLightColor(WireColor color)
    {
        if (runtimeMaterial == null) return;
        int selectionIndex = 0;
        switch (color)
        {
            case WireColor.Red: selectionIndex = 1; break;
            case WireColor.Green: selectionIndex = 2; break;
            case WireColor.Blue: selectionIndex = 3; break;
            case WireColor.Yellow: selectionIndex = 4; break;
        }

        runtimeMaterial.SetFloat(LightSelectionProp, selectionIndex);


        runtimeMaterial.SetFloat(IntensityProp, lightIntensity);

    }

    public void TurnOffLight()
    {
        if (runtimeMaterial == null) return;

        runtimeMaterial.SetFloat(LightSelectionProp, 0);

        runtimeMaterial.SetFloat(IntensityProp, 0.0f);

    }

    [ContextMenu("Test Light On (Yellow)")]
    public void TestLightOn()
    {
        SetLightColor(WireColor.Yellow);
    }

    [ContextMenu("Test Light Off")]
    public void TestLightOff()
    {
        TurnOffLight();
    }
}