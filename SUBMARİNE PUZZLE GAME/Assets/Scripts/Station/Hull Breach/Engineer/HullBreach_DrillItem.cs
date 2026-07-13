using UnityEngine;
using PurrNet;
using DG.Tweening;
using UnityEngine.UI;

public class HullBreach_DrillItem : NetworkBehaviour, IInventoryItem
{
    [Header("References")]
    public Transform drillVisual; // Matkabın sadece görselini (Mesh) taşıyan alt obje

    [Header("Minigame Settings")]
    public float followSpeed = 15f;
    public Vector3 angledRotation = new Vector3(45f, 0f, 45f); // Çapraz duruş
    public Vector3 straightRotation = Vector3.zero;            // Vidalama duruşu
    public float animSpeed = 0.15f;
    public ParticleSystem weldSparks;
    public float zOffsetFromPlate = 0.1f;
    public Vector3 minigameScale = new Vector3(0.5f, 0.5f, 0.5f); // Modül içindeki boyutu
    private Vector3 originalLocalScale;

    [Header("Charge Settings")]
    public float maxCharge = 100f;
    public float currentCharge = 100f;
    public float chargePerScrew = 12.5f; // Vida başına harcanacak şarj
    public Slider drillUIChargeBar;

    private bool isEquipped = false;
    private bool isInMinigame = false;
    private Transform currentPlate;
    private Plane platePlane;
    private Camera mainCam;

    // Orijinal pozisyon hafızası (Modülden çıkınca geri dönmek için)
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private bool isDrilling = false;

    private void Awake()
    {
        if (drillVisual != null)
        {
            originalLocalPos = drillVisual.localPosition;
            originalLocalRot = drillVisual.localRotation;
            originalLocalScale = drillVisual.localScale;
        }

        if (drillUIChargeBar != null)
        {
            drillUIChargeBar.minValue = 0f;
            drillUIChargeBar.maxValue = maxCharge;
            drillUIChargeBar.value = currentCharge;
        }
    }

    #region IINVENTORYITEM GEREKSİNİMLERİ
    public void OnEquip()
    {
        isEquipped = true;
    }

    public void OnUnequip()
    {
        isEquipped = false;
        if (isInMinigame) StopMinigame(); // Başka eşyaya geçilirse minigame'i zorla kapat
        CmdSyncCharge(currentCharge);
    }

    public void OnDrop()
    {
        isEquipped = false;
        if (isInMinigame) StopMinigame();
        CmdSyncCharge(currentCharge);
    }

    public void CanOperate(bool canOperate) { }
    #endregion

    // ==========================================
    // MINIGAME KONTROLLERİ (Soket Tarafından Çağrılır)
    // ==========================================
    public void StartMinigame(Transform plateTransform)
    {
        if (!isEquipped || drillVisual == null) return;

        isInMinigame = true;
        currentPlate = plateTransform;
        mainCam = Camera.main;

        // Plakanın yüzeyini matematiksel bir düzleme çeviriyoruz ki fare tam üstünde gezsin
        platePlane = new Plane(currentPlate.forward, currentPlate.position);

        // Matkabı çapraz konuma getir
        drillVisual.DOKill();
        drillVisual.localRotation = Quaternion.Euler(angledRotation);
        drillVisual.DOScale(minigameScale, 0.3f);
    }

    public void StopMinigame()
    {
        isInMinigame = false;
        isDrilling = false;

        if (weldSparks != null && weldSparks.isPlaying) weldSparks.Stop();

        // Matkabı orijinal el pozisyonuna geri döndür
        if (drillVisual != null)
        {
            drillVisual.DOKill();
            drillVisual.DOLocalMove(originalLocalPos, 0.3f);
            drillVisual.DOLocalRotateQuaternion(originalLocalRot, 0.3f);
            drillVisual.DOScale(originalLocalScale, 0.3f);
        }
    }

    // ==========================================
    // FARE TAKİBİ VE VİDALAMA (Sadece Lokal)
    // ==========================================
    private void Update()
    {
        // Sadece minigame içindeysek ve objenin sahibi bizsek çalışır
        if (!isInMinigame || mainCam == null || !isOwner) return;

        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);

        // 1. FARE TAKİBİ (Matkabı plaka yüzeyinde farenin olduğu yere taşı)
        if (platePlane.Raycast(ray, out float enterDistance))
        {
            Vector3 targetHitPoint = ray.GetPoint(enterDistance);

            // YENİ: Noktayı plakanın normali (yüzey yönü) boyunca dışarı/kendimize doğru çek
            targetHitPoint += platePlane.normal * zOffsetFromPlate;

            // Dünya pozisyonunu Lerp ile güncelle
            drillVisual.position = Vector3.Lerp(drillVisual.position, targetHitPoint, Time.deltaTime * followSpeed);
        }

        // 2. TIKLAMA VE KAYNAK KONTROLÜ
        if (Input.GetMouseButton(0))
        {
            if (currentCharge <= 0)
            {
                if (isDrilling) StopDrillingAnim();
                // TODO: İsteğe bağlı olarak buraya "Tık tık" boşta dönme sesi veya ekranda "Şarj Bitti" uyarısı eklenebilir.
                return;
            }
            CheckAndDrill(ray);
        }
        else if (isDrilling)
        {
            StopDrillingAnim();
        }
    }

    private void CheckAndDrill(Ray ray)
    {
        // Fare imlecinin altındaki WeldPoint'leri ara
        if (Physics.Raycast(ray, out RaycastHit hit, 5f))
        {
            HullBreach_WeldPoint wp = hit.collider.GetComponent<HullBreach_WeldPoint>();

            if (wp != null && !wp.isWelded)
            {
                if (!isDrilling)
                {
                    // Kaynak başladı, matkabı dik konuma getir
                    isDrilling = true;
                    drillVisual.DOKill();
                    drillVisual.DOLocalRotate(straightRotation, animSpeed);
                    if (weldSparks != null) weldSparks.Play();
                }

                float drainRate = chargePerScrew / wp.requiredWeldTime;
                currentCharge -= drainRate * Time.deltaTime;

                // Şarjın eksiye düşmesini engelle
                if (currentCharge < 0) currentCharge = 0;

                // Görseli güncelle
                UpdateDrillVisuals();

                wp.ApplyWeld(Time.deltaTime);
                return;
            }
        }

        // Vidadan dışarı çıkılırsa veya boşa tıklanırsa
        if (isDrilling) StopDrillingAnim();
    }

    public void UpdateDrillVisuals()
    {
        if (drillUIChargeBar != null)
        {
            drillUIChargeBar.value = currentCharge;
        }
    }

    private void StopDrillingAnim()
    {
        isDrilling = false;
        if (weldSparks != null) weldSparks.Stop();

        drillVisual.DOKill();
        drillVisual.DOLocalRotate(angledRotation, animSpeed);
    }

    // ==========================================
    // AĞ (NETWORK) ŞARJ SENKRONİZASYONU
    // ==========================================
    [ServerRpc(requireOwnership: false)]
    private void CmdSyncCharge(float newCharge)
    {
        // Sunucuya gelen güncel şarj bilgisini tüm oyunculara dağıt
        RpcSyncCharge(newCharge);
    }

    [ObserversRpc(runLocally: true)]
    private void RpcSyncCharge(float newCharge)
    {
        // Diğer oyuncularda da şarjı eşitle ve matkabın üzerindeki UI'ı (Slider) güncelle
        currentCharge = newCharge;
        UpdateDrillVisuals();
    }
}