using System.Collections;
using UnityEngine;
using PurrNet;

public class TornPageItem : NetworkBehaviour, IInventoryItem
{
    [Header("References")]
    [SerializeField] private MeshRenderer pageRenderer;
    [SerializeField] private Material baseMaterial;
    [SerializeField] private MeshFilter pageMeshFilter;

    [Header("Placement & Hologram Settings")]
    [SerializeField] private LayerMask placementLayer; // Hangi yüzeylere yapışabilir? (Örn: Wall, Floor)
    [SerializeField] private LayerMask obstacleLayer; // Çakışmayı ne engeller? (Örn: Interactable, Default)
    [SerializeField] private Material validHologramMat; // Yeşil/Mavi yarı saydam materyal
    [SerializeField] private Material invalidHologramMat; // Kırmızı yarı saydam materyal
    [SerializeField] private Vector3 pageExtents = new Vector3(0.15f, 0.2f, 0.05f); // Sayfanın yarım boyutu (Çarpışma testi için)

    private Texture2D pageTexture;
    private Material instancedMat;

    // Gelen parçaları birleştireceğimiz geçici hafıza
    private byte[] clientImageBuffer;

    private GameObject hologramObj;
    private MeshRenderer hologramRenderer;
    private bool isEquippedLocally = false; // Sayfa oyuncunun elinde mi?
    private bool isPlacementModeActive = false; // 'R' tuşuna basıldı mı?
    private bool isValidPlacement = false; // Yüzeye tam oturdu mu?

    private Vector3 currentPlacePos;
    private Quaternion currentPlaceRot;
    private Transform currentPlaceParent; // Asansörler için ebeveyn objesi

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


    private void Update()
    {
        // Sadece objeyi elinde tutan kişi (Local) yerleştirme yapabilir
        if (!isEquippedLocally) return;

        // 'R' tuşu ile yapıştırma modunu aç/kapat
        if (Input.GetKeyDown(KeyCode.R))
        {
            isPlacementModeActive = !isPlacementModeActive;
            if (hologramObj != null) hologramObj.SetActive(isPlacementModeActive);
        }

        if (isPlacementModeActive)
        {
            UpdateHologram();

            // Sığma kontrolü başarılıysa ve sol tıka basıldıysa yapıştır
            if (isValidPlacement && Input.GetMouseButtonDown(0))
            {
                PlacePageOnSurface();
            }
        }
    }


    private void UpdateHologram()
    {
        // Işının hem duvarlara hem de engellere (her şeye) çarpabilmesi için iki maskeyi birleştiriyoruz
        LayerMask raycastMask = placementLayer | obstacleLayer;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Işın menzil içindeki herhangi bir fiziksel objeye çarpıyor mu?
        if (Physics.Raycast(ray, out RaycastHit hit, 3f, raycastMask))
        {
            if (hologramObj == null) CreateHologram();
            hologramObj.SetActive(true);

            // 1. POZİSYON: Z-Fighting'i önlemek için normal yönünde %1 birim dışarı çek
            currentPlacePos = hit.point + (hit.normal * 0.01f);

            // 2. ROTASYON (Z Eksenine 90 Derece Ekleme)
            // Sonra senin modeline özel olarak Z ekseninde 90 derece döndürüyoruz
            currentPlaceRot = Quaternion.LookRotation(-hit.normal);

            hologramObj.transform.position = currentPlacePos;
            hologramObj.transform.rotation = currentPlaceRot;
            currentPlaceParent = hit.collider.transform;

            // 3. GEÇERLİLİK KONTROLÜ (Kırmızı/Yeşil Mantığı)

            // Işının çarptığı obje gerçekten yapıştırmaya izin verdiğimiz bir duvar mı?
            // (Bitwise işlemi ile katman kontrolü yapıyoruz)
            bool isHitSurfacePlaceable = (placementLayer.value & (1 << hit.collider.gameObject.layer)) > 0;

            // Hologramın olduğu hacimde başka bir engel (obstacle) var mı?
            bool isOverlapping = Physics.CheckBox(currentPlacePos, pageExtents, currentPlaceRot, obstacleLayer);

            // Eğer yüzey doğruysa VE başka bir objeyle çakışmıyorsa YEŞİL (Geçerli), aksi halde KIRMIZI (Geçersiz)
            isValidPlacement = isHitSurfacePlaceable && !isOverlapping;

            hologramRenderer.material = isValidPlacement ? validHologramMat : invalidHologramMat;
        }
        else
        {
            // Işın tamamen boşa bakıyorsa (gökyüzü veya çok uzak) hologramı gizle
            if (hologramObj != null) hologramObj.SetActive(false);
            isValidPlacement = false;
        }
    }

    private void CreateHologram()
    {
        // 1. Hologramın Kök Objesi (Pozisyon ve Duvar normalini bu tutacak)
        hologramObj = new GameObject("Page_Hologram_Root");

        // 2. Hologramın Görsel Objesi (Blender yamukluklarını ve Scale'i bu düzeltecek)
        GameObject visualObj = new GameObject("Hologram_Visual");
        visualObj.transform.SetParent(hologramObj.transform);

        MeshFilter mf = visualObj.AddComponent<MeshFilter>();
        hologramRenderer = visualObj.AddComponent<MeshRenderer>();

        // Orijinal mesh'i ata
        mf.mesh = pageMeshFilter.mesh;

        // 2. SORUNUN KESİN ÇÖZÜMÜ:
        // Prefab içindeki PageVisual objende yaptığın tüm düzeltmeleri (Scale 100 ve Z Rotasyonu)
        // kod otomatik olarak holograma da uygular!
        visualObj.transform.localPosition = pageMeshFilter.transform.localPosition;
        visualObj.transform.localRotation = pageMeshFilter.transform.localRotation;
        visualObj.transform.localScale = pageMeshFilter.transform.localScale;
    }

    private void PlacePageOnSurface()
    {
        isPlacementModeActive = false;
        isEquippedLocally = false;
        if (hologramObj != null)
        {
            Destroy(hologramObj);
            hologramObj = null;
        }

        gameObject.SetActive(true);

        var rbPhysics = GetComponent<Rigidbody>();
        if (rbPhysics != null)
        {
            rbPhysics.isKinematic = true;
            rbPhysics.useGravity = false;
        }

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // 1. SORUNUN ÇÖZÜMÜ: Sadece sabit yüzeyler. Parent yok, Scale bozulması yok!
        transform.SetParent(null);
        transform.position = currentPlacePos;
        transform.rotation = currentPlaceRot; // Bu, duvara tam 90 derece dik bakar

        if (InventoryManager.LocalPlayer != null)
        {
            InventoryManager.LocalPlayer.RemoveCurrentItem();
        }
    }
    // =========================================================================================
    // IInventoryItem KONTUARLARI
    // =========================================================================================
    public void OnEquip()
    {
        isEquippedLocally = true;
    }

    public void OnUnequip()
    {
        // Eşya elden bırakıldığında, yere atıldığında veya başka silaha geçildiğinde modu sıfırla
        isEquippedLocally = false;
        isPlacementModeActive = false;
        if (hologramObj != null) hologramObj.SetActive(false);
    }

    public void OnDrop()
    {
        OnUnequip();
    }

    public void CanOperate(bool canOperate) { }
}