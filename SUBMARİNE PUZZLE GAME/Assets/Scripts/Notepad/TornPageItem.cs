using System.Collections;
using UnityEngine;
using PurrNet;

public class TornPageItem : NetworkBehaviour, IInventoryItem
{
    [Header("References")]
    [SerializeField] private MeshRenderer pageRenderer;
    [SerializeField] private Material baseMaterial;

    private Texture2D pageTexture;
    private Material instancedMat;

    // Gelen parçaları birleştireceğimiz geçici hafıza
    private byte[] clientImageBuffer;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        InitializeMaterial();
    }

    private void InitializeMaterial()
    {
        if (instancedMat != null) return;
        pageTexture = new Texture2D(2, 2);
        instancedMat = new Material(baseMaterial);
        instancedMat.SetTexture("_BaseMap", pageTexture);
        pageRenderer.material = instancedMat;
    }

    // =========================================================================================
    // SERVER'DAN CLIENT'LARA PARÇALI GÖNDERİM SİSTEMİ
    // =========================================================================================

    // Sadece Server çağırır
    public void SetImageDataAndDistribute(byte[] completeData)
    {
        if (!isServer) return;

        // 1. Server (Host) için resmi anında uygula
        ApplyImageData(completeData);

        // 2. Diğer oyunculara kargolamaya başla
        StartCoroutine(DistributeChunksRoutine(completeData));
    }

    private IEnumerator DistributeChunksRoutine(byte[] imageData)
    {
        int chunkSize = 1000;
        int totalSize = imageData.Length;
        int totalChunks = Mathf.CeilToInt((float)totalSize / chunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int length = Mathf.Min(chunkSize, totalSize - i * chunkSize);
            byte[] chunk = new byte[length];
            System.Array.Copy(imageData, i * chunkSize, chunk, 0, length);

            // Tüm oyunculara bu parçayı yolla
            ReceiveChunkObserversRpc(chunk, i * chunkSize, totalSize);

            yield return null; // Ağı boğmamak için 1 frame bekle
        }
    }

    [ObserversRpc]
    private void ReceiveChunkObserversRpc(byte[] chunk, int startIndex, int totalSize)
    {
        // Server zaten resmi uyguladı, tekrar etmesine gerek yok
        if (isServer) return;

        // Hafıza (Buffer) boşsa, resim boyutu kadar yer ayır
        if (clientImageBuffer == null || clientImageBuffer.Length != totalSize)
        {
            clientImageBuffer = new byte[totalSize];
        }

        // Gelen küçük kargoyu ana yapboza yerleştir
        System.Array.Copy(chunk, 0, clientImageBuffer, startIndex, chunk.Length);

        // Eğer yapboz tamamlandıysa resmi uygula ve hafızayı temizle
        if (startIndex + chunk.Length >= totalSize)
        {
            ApplyImageData(clientImageBuffer);
            clientImageBuffer = null; // Bellek (RAM) temizliği
        }
    }

    private void ApplyImageData(byte[] data)
    {
        if (data != null && data.Length > 0)
        {
            InitializeMaterial();
            pageTexture.LoadImage(data); // JPG verisini işle
            pageTexture.Apply();
        }
    }

    // =========================================================================================
    // IInventoryItem KONTUARLARI
    // =========================================================================================
    public void OnEquip() { }
    public void OnUnequip() { }
    public void OnDrop() { }
    public void CanOperate(bool canOperate) { }
}