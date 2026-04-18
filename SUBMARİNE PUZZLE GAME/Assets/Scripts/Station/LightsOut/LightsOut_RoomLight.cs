using System;
using Unity.VisualScripting;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Light))]
public class LightsOut_RoomLight : MonoBehaviour
{
    private Light myLight;
    private float originalIntensity;
    [SerializeField] private bool isNormalLight;

    private void Awake()
    {
        myLight = GetComponent<Light>();
        originalIntensity = myLight.intensity;
        if (isNormalLight)
        {
            myLight.enabled = true;
        }
        else
        {
            myLight.enabled = false;
        }
    }

    private void OnEnable()
    {
        LightsOut_StationManager.OnPowerStatusChanged += HandlePowerChange;
    }

    private void OnDisable()
    {
        LightsOut_StationManager.OnPowerStatusChanged -= HandlePowerChange;
    }

    private void HandlePowerChange(bool isPowerOn)
    {
        if (isPowerOn)
        {
            myLight.intensity = originalIntensity;
            myLight.enabled = isNormalLight;
        }
        else if (isNormalLight)
        {
            myLight.DOIntensity(0, 2f).OnComplete(() => myLight.enabled = false);
        }
        else
        {
            myLight.enabled = true;

        }
    }
}