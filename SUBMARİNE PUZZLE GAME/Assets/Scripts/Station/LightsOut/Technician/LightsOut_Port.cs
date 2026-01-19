using UnityEngine;
using UnityEngine.UI;

public class LightsOut_Port : MonoBehaviour
{
    public int portID;

    [Header("Visuals")]
    public Image connectionLight;
    public Transform snapPoint;

    private void Awake()
    {
        if (connectionLight) connectionLight.color = Color.black;
    }

    public void SetLightColor(WireColor color)
    {
        if (connectionLight == null) return;

        switch (color)
        {
            case WireColor.Yellow: connectionLight.color = Color.yellow; break;
            case WireColor.Green: connectionLight.color = Color.green; break;
            case WireColor.Blue: connectionLight.color = Color.blue; break;
            case WireColor.Red: connectionLight.color = Color.red; break;
            default: connectionLight.color = Color.black; break; // Kapalı
        }
    }

    public void TurnOffLight()
    {
        if (connectionLight) connectionLight.color = Color.black;
    }
}