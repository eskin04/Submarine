using UnityEngine;
using PurrNet;

public class FlashlightItem : NetworkBehaviour, IInventoryItem
{
    [Header("Components")]
    [SerializeField] private Light lightSource;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip toggleSound;

    [Header("Settings")]
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 5f;

    public SyncVar<float> storedBattery = new SyncVar<float>(100f);

    private bool isLightOn = false;

    private float localRuntimeBattery;

    private bool isEquipped = false;

    private ItemInfoView itemInfoView;
    private float UIbatteryUpdateTimer = .5f;
    private bool amIOwner;

    private Transform originalLightParent;
    private Vector3 originalLightPos;
    private Quaternion originalLightRot;

    protected override void OnSpawned()
    {
        storedBattery.value = maxBattery;
        itemInfoView = InstanceHandler.GetInstance<ItemInfoView>();
        if (lightSource)
        {
            lightSource.enabled = false;
            originalLightParent = lightSource.transform.parent;
            originalLightPos = lightSource.transform.localPosition;
            originalLightRot = lightSource.transform.localRotation;
        }
    }

    public void SetInteractionMode(bool isInteracting, Transform targetCamera = null)
    {
        if (lightSource == null) return;

        if (isInteracting && targetCamera != null)
        {

            lightSource.transform.SetParent(targetCamera);
            lightSource.transform.localPosition = new Vector3(0, 0, -.7f);
            lightSource.transform.localRotation = Quaternion.identity;
        }
        else
        {
            lightSource.transform.SetParent(originalLightParent);
            lightSource.transform.localPosition = originalLightPos;
            lightSource.transform.localRotation = originalLightRot;


        }
    }


    public void OnEquip()
    {
        isEquipped = true;
        amIOwner = isOwner;
        if (isOwner)
        {

            localRuntimeBattery = storedBattery;
            InstanceHandler.GetInstance<GameViewManager>().ShowView<ItemInfoView>(hideOthers: false);
            itemInfoView.UpdateBatteryUI(GetBatteryPercent());

        }
    }

    public void OnUnequip()
    {
        isEquipped = false;
        if (amIOwner)
        {

            CmdSyncData(localRuntimeBattery);
            InstanceHandler.GetInstance<GameViewManager>().HideView<ItemInfoView>();
            if (isLightOn)
            {
                ToggleLight(false);
            }


        }
    }

    public void OnDrop()
    {
        isEquipped = false;

        if (amIOwner)
        {
            CmdSyncData(localRuntimeBattery);
            InstanceHandler.GetInstance<GameViewManager>().HideView<ItemInfoView>();
            if (isLightOn)
            {
                ToggleLight(false);
            }

            amIOwner = false;

        }
    }

    private void Update()
    {

        if (isOwner && isLightOn)
        {
            if (localRuntimeBattery > 0)
            {
                localRuntimeBattery -= drainRate * Time.deltaTime;

                UIbatteryUpdateTimer -= Time.deltaTime;
                if (UIbatteryUpdateTimer <= 0f)
                {
                    UIbatteryUpdateTimer = .5f;
                    itemInfoView.UpdateBatteryUI(GetBatteryPercent());
                }

                if (localRuntimeBattery <= 0)
                {
                    localRuntimeBattery = 0;
                    CmdSyncData(localRuntimeBattery);
                    ToggleLight(false);
                    itemInfoView.UpdateBatteryUI(0f);
                    return;
                }
            }
        }

        if (isEquipped && isOwner && Input.GetKeyDown(KeyCode.F))
        {
            if (localRuntimeBattery > 0)
            {
                ToggleLight(!isLightOn);

            }
        }
    }

    [ServerRpc(requireOwnership: false)]
    private void CmdSyncData(float batteryLevel)
    {
        storedBattery.value = batteryLevel;
    }


    private void ToggleLight(bool isLightOnParam)
    {
        isLightOn = isLightOnParam;
        if (lightSource) lightSource.enabled = isLightOn;
        PlaySound();
    }

    private void PlaySound()
    {
        if (audioSource && toggleSound) audioSource.PlayOneShot(toggleSound);
    }

    public float GetBatteryPercent()
    {
        float val = isOwner ? localRuntimeBattery : storedBattery;
        return Mathf.Clamp01(val / maxBattery);
    }
}