using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightsOut_RoomLight : MonoBehaviour
{
    private Light myLight;

    private void Awake()
    {
        myLight = GetComponent<Light>();
    }

    // Obje aktifleşince dinlemeye başla (Abone ol)
    private void OnEnable()
    {
        LightsOut_StationManager.OnPowerStatusChanged += HandlePowerChange;
    }

    // Obje kapanınca veya sahne değişince dinlemeyi bırak (Abonelikten çık)
    // BU ÇOK ÖNEMLİ: Bunu yapmazsan sahne değiştiğinde hata alırsın.
    private void OnDisable()
    {
        LightsOut_StationManager.OnPowerStatusChanged -= HandlePowerChange;
    }

    // Olay tetiklendiğinde ne yapayım?
    private void HandlePowerChange(bool isPowerOn)
    {
        if (myLight != null)
        {
            myLight.enabled = isPowerOn;
        }
    }
}