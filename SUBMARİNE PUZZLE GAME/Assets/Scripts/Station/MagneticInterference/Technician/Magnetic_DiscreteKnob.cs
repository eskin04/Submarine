using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_DiscreteKnob : MonoBehaviour
{
    public enum KnobType { Amplitude, Frequency, Phase }

    [Header("References")]
    public Magnetic_WaveOscilloscope oscilloscope;
    public Transform knobMesh;

    [Header("Knob Settings")]
    public KnobType type;
    public bool invertRotation = false;

    [Tooltip("Knob'un bir sonraki değere atlaması için mouse'un kaç derece çevrilmesi gerektiği. (Örn: 60 veya 90)")]
    public float snapThresholdAngle = 60f;

    [Tooltip("Mouse hareketinin ne kadar zor olacağı. Düşük değerler daha fazla mouse hareketi gerektirir.")]
    public float turnSensitivity = 1.0f;

    [Tooltip("Knob'un görsel olarak döneceği derece miktarı (Genelde snapThresholdAngle ile aynı tutulur)")]
    public float visualStepAngle = 60f;

    // Arka plan matematik değişkenleri
    private float accumulatedTension = 0f; // Mouse'un biriktirdiği dönme enerjisi
    private float currentLockedVisualAngle = 0f; // Knob'un şu anki sabit açısı
    private float previousMouseAngle;
    private Vector2 knobScreenPos;
    private bool isDragging = false;


    public void InitializePosition(int startingValue)
    {
        // 1 tabanli degeri aci hesabina uydurmak icin 1 cikartiyoruz (Orenk: 1 degeri -> 0. adim)
        int stepIndex = startingValue - 1;
        if (type == KnobType.Phase)
        {
            stepIndex = startingValue;
        }

        currentLockedVisualAngle = stepIndex * visualStepAngle;

        // Modeli anlik olarak baslangic acisina donduruyoruz
        if (knobMesh != null)
        {
            knobMesh.localEulerAngles = new Vector3(0, 0, -currentLockedVisualAngle);
        }

        // Kalıntı gerilimleri temizle
        accumulatedTension = 0f;
    }

    private void OnMouseDown()
    {
        if (oscilloscope == null) return;

        isDragging = true;
        knobScreenPos = Camera.main.WorldToScreenPoint(knobMesh.position);

        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;
        previousMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        // Tıklama anında çalışan bir FMOD sesi eklenebilir (Örn: Elini metale koyma sesi)
    }

    private void OnMouseDrag()
    {
        if (!isDragging || oscilloscope == null) return;

        Vector2 mouseDir = (Vector2)Input.mousePosition - knobScreenPos;
        float currentMouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        float deltaAngle = Mathf.DeltaAngle(previousMouseAngle, currentMouseAngle);
        if (invertRotation) deltaAngle = -deltaAngle;

        previousMouseAngle = currentMouseAngle;

        // Gerilimi biriktir (Zorlanma hissi)
        accumulatedTension += deltaAngle * turnSensitivity;

        // Knob'un direndiğini göstermek için görseli hafifçe mouse'a doğru esnetiyoruz (Maksimum 15 derece esner)
        float strainVisual = Mathf.Clamp(accumulatedTension * 0.25f, -15f, 15f);
        knobMesh.localEulerAngles = new Vector3(0, 0, -(currentLockedVisualAngle + strainVisual));

        // Eğer biriken gerilim, eşik değerini aşarsa (Snap!)
        if (Mathf.Abs(accumulatedTension) >= snapThresholdAngle)
        {
            int direction = (int)Mathf.Sign(accumulatedTension); // 1 (Sağ) veya -1 (Sol)
            PerformSnap(direction);
        }
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        // Eğer oyuncu yeterince çevirmeden bırakırsa, gerilimi sıfırla ve eski konumuna geri yaylandır
        if (accumulatedTension != 0)
        {
            accumulatedTension = 0f;

            knobMesh.DOKill();
            // Yetersiz güçte bırakıldığı için yerine sekerek geri döner
            knobMesh.DOLocalRotate(new Vector3(0, 0, -currentLockedVisualAngle), 0.3f)
                    .SetEase(Ease.OutElastic, 1.5f, 0.5f);
        }
    }

    private void PerformSnap(int direction)
    {
        // 1. Gerilimi sıfırla
        accumulatedTension = 0f;

        // 2. Görsel açıyı kilitli değere ekle
        currentLockedVisualAngle += direction * visualStepAngle;
        direction = -direction;
        // 3. Backend'e değişikliği bildir
        switch (type)
        {
            case KnobType.Amplitude:
                oscilloscope.ChangeAmplitude(direction);
                break;
            case KnobType.Frequency:
                oscilloscope.ChangeFrequency(direction);
                break;
            case KnobType.Phase:
                oscilloscope.ChangePhase(direction);
                break;
        }

        // 4. Mekanik Snap Animasyonu (Sert bir şekilde yerine oturma)
        knobMesh.DOKill();
        knobMesh.DOLocalRotate(new Vector3(0, 0, -currentLockedVisualAngle), 0.15f)
                .SetEase(Ease.OutBack, 2.0f); // OutBack ile hedefi azıcık geçip geri oturur (ağır metal hissi)

        // 5. FMOD Ağır Şalter Sesi Eklenebilir
        // RuntimeManager.PlayOneShot("event:/Interactables/Heavy_Knob_Snap");
    }
}