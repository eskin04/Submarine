using System;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightsOut_RoomLight : MonoBehaviour
{
    private Light myLight;
    [SerializeField] private bool isNormalLight;

    private void Awake()
    {
        myLight = GetComponent<Light>();
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
        if (isNormalLight)
        {
            myLight.enabled = isPowerOn;
        }
        else
        {
            myLight.enabled = !isPowerOn;
        }
    }
}