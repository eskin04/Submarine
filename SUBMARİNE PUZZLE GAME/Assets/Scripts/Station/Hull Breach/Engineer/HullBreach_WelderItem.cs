using UnityEngine;
using PurrNet;

public class HullBreach_WelderItem : NetworkBehaviour, IInventoryItem
{
    [Header("Settings")]
    public float weldRange = 3f;
    public KeyCode operateKey = KeyCode.Mouse0;

    [Header("Effects (Local)")]
    public ParticleSystem weldSparks; // Ucundan çıkacak kıvılcım
                                      // TODO: FMOD Kaynak sesi referansı eklenebilir

    private bool isEquipped = false;
    private bool canOperate = true;

    #region IINVENTORYITEM IMPLEMENTATION

    public void OnEquip()
    {
        isEquipped = true;
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            promptView.AddPrompt("welder_use", "Hold Left Click", "Use Welder");
        }
    }

    public void OnUnequip()
    {
        isEquipped = false;
        StopWeldingEffects();
        if (InstanceHandler.TryGetInstance<PromptView>(out var promptView))
        {
            promptView.RemovePrompt("welder_use");
        }
    }

    public void OnDrop()
    {
        isEquipped = false;
        StopWeldingEffects();
    }

    public void CanOperate(bool canOperate)
    {
        this.canOperate = canOperate;
    }

    #endregion

    private void Update()
    {
        // Sadece eşyayı tutan kişi ve aktifse çalışır
        if (!isEquipped || !canOperate || !isOwner) return;

        if (Input.GetKey(operateKey))
        {
            PerformWelding();
        }
        else
        {
            StopWeldingEffects();
        }
    }

    private void PerformWelding()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        // Görsel efektleri başlat
        if (weldSparks != null && !weldSparks.isPlaying) weldSparks.Play();
        // TODO: FMOD Kaynak sesini başlat/devam ettir

        // Ekranın ortasından Raycast at
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, weldRange))
        {
            // Çarptığımız objede WeldPoint var mı?
            HullBreach_WeldPoint point = hit.collider.GetComponent<HullBreach_WeldPoint>();
            if (point != null)
            {
                // Saniyede Time.deltaTime kadar ilerleme kaydettir
                point.ApplyWeld(Time.deltaTime);
            }
        }
    }

    private void StopWeldingEffects()
    {
        if (weldSparks != null && weldSparks.isPlaying) weldSparks.Stop();
        // TODO: FMOD Kaynak sesini durdur
    }
}