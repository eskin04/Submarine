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

    private void OnMouseDown()
    {
        if (oscilloscope == null) return;

        // Fiziksel tuşa basılma animasyonu (Aşağı inip geri çıkar)
        if (buttonMesh != null)
        {
            buttonMesh.DOKill();
            buttonMesh.localPosition = Vector3.zero; // Başlangıç noktasına eşitle
            buttonMesh.DOPunchPosition(Vector3.down * 0.02f, 0.2f, 1, 0);
        }

        // Tıklama Sesi Eklenebilir
        // RuntimeManager.PlayOneShot("event:/UI/Button_Press");

        // Osiloskop'a kanal geçiş komutunu gönder
        oscilloscope.ChangeViewedChannel(channelIndex);
    }
}