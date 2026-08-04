using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HullBreach_WeldPoint : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Bu noktanın kaynatılması kaç saniye sürecek?")]
    public float requiredWeldTime = 1.0f;

    [Header("Local State")]
    public bool isWelded = false;
    private float currentWeldProgress = 0f;

    private HullBreach_CrackSocket parentSocket;

    public void Initialize(HullBreach_CrackSocket socket)
    {
        parentSocket = socket;
        isWelded = false;
        currentWeldProgress = 0f;
    }

    public void ApplyWeld(float deltaTime)
    {
        if (isWelded || parentSocket == null) return;

        currentWeldProgress += deltaTime;


        if (currentWeldProgress >= requiredWeldTime)
        {
            if (HighlightManager.Instance != null)
            {
                HighlightManager.Instance.SetInteractableState(transform.gameObject, false);
            }
            isWelded = true;
            parentSocket.OnPointWelded();

            Debug.Log($"<color=cyan>[WELD]</color> Bir köşe başarıyla sabitlendi!");
        }
    }
}