using UnityEngine;
using PurrNet;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Collider))]
public class HullBreach_CrackSocket : NetworkBehaviour
{
    [Header("Location Data")]
    public int floorIndex;
    public CrackZone zone;
    public int spawnPointIndex;
    [Header("Particle System")]
    public ParticleSystem waterParticlePrefab;
    private ParticleSystem activeWaterParticle;
    public float offsetRateMultiplier = 0.15f;
    public float offsetSpeedMultiplier = 0.2f;

    [Header("References")]
    public HullBreach_StationManager stationManager;
    public ModuleInteraction moduleInteraction;

    [Header("Live State")]
    public int currentCrackID = -1;
    public bool isCrackSpawned = false;
    public HullBreach_PlateItem slottedPlate;
    public bool isFixed = false;

    private Interactable interactable;
    private Collider socketCollider;
    private int weldedPointsCount = 0;
    private float originalRateMultiplier;
    private float originalSpeedMultiplier;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        socketCollider = GetComponent<Collider>();

        if (moduleInteraction != null) moduleInteraction.enabled = false;

        isCrackSpawned = false;
        UpdateSocketVisualsAndInteraction();

        if (stationManager != null) stationManager.RegisterSocket(this);

        interactable.onInteractCondition += CanInteractWithSocket;
    }

    // ==========================================
    // DİNAMİK ETKİLEŞİM ŞARTLARI
    // ==========================================
    private bool CanInteractWithSocket()
    {
        if (!isCrackSpawned || isFixed) return false;

        if (slottedPlate == null)
        {
            return CheckIfHoldingPlate();
        }
        else
        {
            return CheckIfHoldingDrill();
        }
    }

    private bool CheckIfHoldingPlate()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return false;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return false;
        HullBreach_PlateItem plate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (plate != null) return true;

        return false;
    }

    private bool CheckIfHoldingDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return false;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return false;
        HullBreach_DrillItem drill = heldItemObj.GetComponent<HullBreach_DrillItem>();
        if (drill != null) return true;

        return false;
    }

    // ==========================================
    // ETKİLEŞİM YÖNLENDİRİCİSİ (Router)
    // ==========================================
    public void OnSocketInteracted()
    {
        if (stationManager == null || !stationManager.isRoundActive.value || !isCrackSpawned) return;

        if (slottedPlate == null)
        {
            TryInsertPlate();
        }
        else if (!isFixed && moduleInteraction != null)
        {
            moduleInteraction.enabled = true;
            moduleInteraction.Interact();
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null)
            {
                drill.StartMinigame(slottedPlate.transform);
            }
        }
    }

    public void OnStopInteract()
    {
        if (moduleInteraction != null && moduleInteraction.enabled)
        {
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null) drill.StopMinigame();
        }
    }

    private void TryInsertPlate()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return;

        HullBreach_PlateItem heldPlate = heldItemObj.GetComponent<HullBreach_PlateItem>();
        if (heldPlate != null)
        {
            interactable.StopInteract();
            stationManager.CmdTryPlacePlate(currentCrackID, heldItemObj, this);
        }
    }

    // ==========================================
    // AĞ VE DURUM GÜNCELLEMELERİ
    // ==========================================

    [ObserversRpc(runLocally: true)]
    public void RpcActivateCrack(int crackID)
    {
        currentCrackID = crackID;
        isCrackSpawned = true;
        isFixed = false;
        slottedPlate = null;
        UpdateSocketVisualsAndInteraction();
    }

    [ObserversRpc(runLocally: true)]
    public void RpcPlacePlateInSocket(GameObject plateObj, float randomZRot)
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return;
        inv.RemoveCurrentItem();
        slottedPlate = plateObj.GetComponent<HullBreach_PlateItem>();

        plateObj.transform.SetParent(this.transform);
        plateObj.transform.localPosition = Vector3.zero;
        plateObj.transform.localRotation = Quaternion.Euler(0, 0, randomZRot);
        plateObj.SetActive(true);




        weldedPointsCount = 0;
        HullBreach_WeldPoint[] points = plateObj.GetComponentsInChildren<HullBreach_WeldPoint>();
        foreach (var point in points)
        {
            point.Initialize(this);
        }
        interactable.SetDisplayName("Use Drill to Weld");
        UpdateSocketVisualsAndInteraction();
    }

    public void OnPointWelded()
    {
        weldedPointsCount++;
        if (weldedPointsCount >= 4)
        {
            stationManager.CmdFixCrack(currentCrackID);
        }
    }

    public void RpcOnCrackFixed()
    {
        isFixed = true;

        if (moduleInteraction != null && moduleInteraction.enabled)
        {
            moduleInteraction.StopInteract();
            interactable.SetInteractable(false);
            HullBreach_DrillItem drill = GetPlayerEquippedDrill();
            if (drill != null) drill.StopMinigame();
        }

        UpdateSocketVisualsAndInteraction();
    }

    private HullBreach_DrillItem GetPlayerEquippedDrill()
    {
        InventoryManager inv = InventoryManager.LocalPlayer;
        if (inv == null) return null;
        GameObject heldItemObj = inv.GetCurrentHeldObject();
        if (heldItemObj == null) return null;

        HullBreach_DrillItem heldDrill = heldItemObj.GetComponent<HullBreach_DrillItem>();
        return heldDrill;
    }

    private void UpdateSocketVisualsAndInteraction()
    {
        if (interactable == null || socketCollider == null) return;

        if (!isCrackSpawned || isFixed)
        {
            interactable.SetInteractable(false);
            socketCollider.enabled = false;

            if (activeWaterParticle != null)
            {
                activeWaterParticle.Stop();

                Destroy(activeWaterParticle.gameObject, 2f);
                activeWaterParticle = null;
            }
            return;
        }

        interactable.SetInteractable(true);
        socketCollider.enabled = true;

        if (activeWaterParticle == null && waterParticlePrefab != null)
        {
            activeWaterParticle = Instantiate(waterParticlePrefab, transform);

            activeWaterParticle.transform.localPosition = Vector3.zero;
            activeWaterParticle.transform.localRotation = Quaternion.identity;

            originalRateMultiplier = activeWaterParticle.emission.rateOverTimeMultiplier;
            originalSpeedMultiplier = activeWaterParticle.main.startSpeedMultiplier;
        }

        if (activeWaterParticle != null)
        {
            var main = activeWaterParticle.main;
            var emission = activeWaterParticle.emission;

            if (slottedPlate != null)
            {
                if (!activeWaterParticle.isPlaying) activeWaterParticle.Play();

                emission.rateOverTimeMultiplier = originalRateMultiplier * offsetRateMultiplier;
                main.startSpeedMultiplier = originalSpeedMultiplier * offsetSpeedMultiplier;
            }
            else
            {
                if (!activeWaterParticle.isPlaying) activeWaterParticle.Play();

                emission.rateOverTimeMultiplier = originalRateMultiplier;
                main.startSpeedMultiplier = originalSpeedMultiplier;
            }
        }
    }
}