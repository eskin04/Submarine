using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(Collider))]
public class Magnetic_ChannelButton : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("0 = CH1, 1 = CH2, 2 = CH3")]
    public int channelIndex;

    [Header("Referanslar")]
    public Magnetic_WaveOscilloscope oscilloscope;
    public Transform buttonMesh; // Hareket edecek model kısmı

    private Vector3 originalLocalPos;
    private bool isLocked = false;
    private bool isActive = false;

    private void Awake()
    {
        if (buttonMesh != null)
            originalLocalPos = buttonMesh.localPosition;
    }

    // Osiloskop tarafından çağrılır. Butonun görünümünü ayarlar.
    public void UpdateButtonState(bool active, bool locked)
    {
        isActive = active;
        isLocked = locked;

        if (buttonMesh != null)
        {
            buttonMesh.DOKill(); // Var olan animasyonları durdur

            if (isActive)
            {
                // Buton aktifse fiziksel olarak aşağıda basılı kalır
                buttonMesh.DOLocalMove(originalLocalPos + (Vector3.back * 0.02f), 0.2f);
            }
            else
            {
                // Normal durumdaysa eski yüksekliğine döner
                buttonMesh.DOLocalMove(originalLocalPos, 0.2f);
            }
        }
    }

    private void OnMouseDown()
    {
        if (oscilloscope == null) return;

        if (isLocked)
        {

            // Hata sesi için:
            // RuntimeManager.PlayOneShot("event:/UI/Error_Buzzer");
            return;
        }

        if (isActive) return; // Zaten bu kanaldaysak tekrar basmayı yoksay

        // Tıklama Sesi
        // RuntimeManager.PlayOneShot("event:/UI/Button_Press");

        oscilloscope.ChangeViewedChannel(channelIndex);
    }
}