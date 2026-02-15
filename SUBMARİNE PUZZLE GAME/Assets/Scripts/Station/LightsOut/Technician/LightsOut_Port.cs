using UnityEngine;

public class LightsOut_Port : MonoBehaviour
{
    public int portID;

    [Header("Visuals")]
    [SerializeField] private MeshRenderer statusLightRenderer;
    [SerializeField] private int materialIndex = 0;

    public Transform snapPoint;


    private static readonly int LightSelectionProp = Shader.PropertyToID("_LIGHTSELECTION");
    private static readonly int IsGlowProp = Shader.PropertyToID("_sGlow");

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
            case WireColor.Red: selectionIndex = 0; break;
            case WireColor.Green: selectionIndex = 1; break;
            case WireColor.Yellow: selectionIndex = 2; break;
            case WireColor.Blue: selectionIndex = 3; break;
        }

        runtimeMaterial.SetInt(LightSelectionProp, selectionIndex);


        runtimeMaterial.SetFloat(IsGlowProp, 0.0f);

    }

    public void TurnOffLight()
    {
        if (runtimeMaterial == null) return;


        runtimeMaterial.SetFloat(IsGlowProp, 1.0f);

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