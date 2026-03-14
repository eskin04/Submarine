using UnityEngine;
using PurrNet;
using DG.Tweening;

public class LocalHeadlamp : NetworkBehaviour
{
    [Header("Components")]
    [SerializeField] private Light headLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip toggleSound;

    [Header("Settings")]
    [SerializeField] private KeyCode operateKey = KeyCode.F;
    [SerializeField] private float maxBattery = 100f;
    [SerializeField] private float drainRate = 5f;
    [SerializeField] private float rechargeRate = .2f;
    [SerializeField] private float rechargeDelay = 2.5f;
    private float rechargeTimer;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerThreshold = 20f;
    [SerializeField] private float minFlickerInterval = 2f;
    [SerializeField] private float maxFlickerInterval = 6f;

    private bool isLightOn = false;
    private float localBattery;
    private float uiUpdateTimer = 0.5f;
    private float nextFlickerTime;

    // DOTween referansları
    private float originalIntensity;
    private Tween flickerTween;

    private ItemInfoView itemInfoView;
    private GameViewManager viewManager;

    protected override void OnSpawned()
    {
        if (!isOwner)
        {
            if (headLight) headLight.enabled = false;
            this.enabled = false;
            return;
        }

        localBattery = maxBattery;
        rechargeTimer = rechargeDelay;

        itemInfoView = InstanceHandler.GetInstance<ItemInfoView>();
        viewManager = InstanceHandler.GetInstance<GameViewManager>();

        if (headLight)
        {
            originalIntensity = headLight.intensity;

            if (Camera.main != null)
            {
                headLight.transform.SetParent(Camera.main.transform);
                headLight.transform.localPosition = new Vector3(0, 0, -0.5f);
                headLight.transform.localRotation = Quaternion.identity;
            }
        }

        SetFlickerTimer();
    }

    private void Update()
    {
        if (!isOwner) return;

        HandleInput();
        HandleBattery();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(operateKey))
        {
            if (localBattery > 0 || isLightOn)
            {
                ToggleLight(!isLightOn);
            }
        }
    }

    private void HandleBattery()
    {
        if (isLightOn && localBattery > 0)
        {
            localBattery -= drainRate * Time.deltaTime;
            rechargeTimer = rechargeDelay;
            uiUpdateTimer -= Time.deltaTime;
            if (uiUpdateTimer <= 0f)
            {
                uiUpdateTimer = 0.5f;
                itemInfoView?.UpdateBatteryUI(localBattery / maxBattery);
            }

            if (localBattery <= flickerThreshold)
            {
                nextFlickerTime -= Time.deltaTime;
                if (nextFlickerTime <= 0f)
                {
                    PlayDOTweenFlicker();
                    SetFlickerTimer();
                }
            }

            if (localBattery <= 0)
            {
                localBattery = 0;
                ToggleLight(false);
                itemInfoView?.UpdateBatteryUI(0f);
            }
        }
        else if (!isLightOn && localBattery < maxBattery)
        {
            if (rechargeTimer > 0)
            {
                rechargeTimer -= Time.deltaTime;
            }
            else
            {
                localBattery += rechargeRate * Time.deltaTime;

                if (localBattery > maxBattery)
                {
                    localBattery = maxBattery;
                }
            }
        }
    }

    private void ToggleLight(bool state)
    {
        isLightOn = state;

        flickerTween?.Kill();

        if (headLight)
        {
            headLight.enabled = isLightOn;
            headLight.intensity = originalIntensity;
        }

        PlaySound();

        if (state)
        {
            viewManager?.ShowView<ItemInfoView>(hideOthers: false);
            itemInfoView?.UpdateBatteryUI(localBattery / maxBattery);
        }
        else
        {
            viewManager?.HideView<ItemInfoView>();
        }
    }

    private void PlayDOTweenFlicker()
    {
        if (headLight == null) return;

        if (flickerTween != null && flickerTween.IsActive()) return;

        int loops = Random.Range(1, 4) * 2;
        float duration = Random.Range(0.05f, 0.15f);

        flickerTween = headLight.DOIntensity(0f, duration)
            .SetLoops(loops, LoopType.Yoyo)
            .SetEase(Ease.Flash)
            .OnKill(() =>
            {
                if (headLight != null) headLight.intensity = originalIntensity;
            });
    }

    private void SetFlickerTimer()
    {
        nextFlickerTime = Random.Range(minFlickerInterval, maxFlickerInterval);
    }

    private void PlaySound()
    {
        if (audioSource && toggleSound) audioSource.PlayOneShot(toggleSound);
    }
}