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

    // Plaka sokete oturduğunda soket tarafından çağrılır
    public void Initialize(HullBreach_CrackSocket socket)
    {
        parentSocket = socket;
        isWelded = false;
        currentWeldProgress = 0f;
    }

    // Kaynak makinesinin Raycast'i çarptığında tetiklenir
    public void ApplyWeld(float deltaTime)
    {
        if (isWelded || parentSocket == null) return;

        currentWeldProgress += deltaTime;

        // İsteğe bağlı: Köşe kaynatılırken rengini değiştirmek için Material/Color değişimi yapabilirsin

        if (currentWeldProgress >= requiredWeldTime)
        {
            isWelded = true;
            parentSocket.OnPointWelded();

            // Kaynak bitince küçük bir "Tıs" sesi veya görsel geri bildirim eklenebilir
            Debug.Log($"<color=cyan>[WELD]</color> Bir köşe başarıyla sabitlendi!");
        }
    }
}