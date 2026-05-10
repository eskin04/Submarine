using UnityEngine;
using PurrNet; // Eğer objenin ağ üzerinde transform senkronizasyonu varsa eklenebilir

public class Keycard_Item : NetworkBehaviour
{
    [Header("Card")]
    public CardData myData;

    [Header("Mesh")]
    public MeshRenderer meshRenderer;
    private Material runtimeMaterial;

    private static readonly int ColorProp = Shader.PropertyToID("_KeycardColor");
    private static readonly int TypeProp = Shader.PropertyToID("_KeycardCrack");
    private static readonly int DetailProp = Shader.PropertyToID("_KeycardDetail");

    private void Awake()
    {
        if (meshRenderer != null)
        {
            runtimeMaterial = meshRenderer.materials[0];
        }
        else
        {
            Debug.LogError("[Keycard_Item] MeshRenderer atanmamış!");
        }
    }

    [ObserversRpc(runLocally: true)]
    public void InitializeCard(CardData data)
    {
        myData = data;
        gameObject.name = $"Keycard_{data.Color}_{data.Type}_{data.Detail}";

        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(ColorProp, (float)myData.Color);

            runtimeMaterial.SetFloat(TypeProp, (float)myData.Type);

            runtimeMaterial.SetFloat(DetailProp, (float)myData.Detail);

        }
    }

}