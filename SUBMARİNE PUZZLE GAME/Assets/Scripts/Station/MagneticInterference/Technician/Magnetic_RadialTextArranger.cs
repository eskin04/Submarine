using UnityEngine;

public class Magnetic_RadialTextArranger : MonoBehaviour
{
    [Header("Ayarlar")]
    [Tooltip("Yerleştirilecek Text objelerini sırasıyla buraya sürükleyin (1, 2, 3...)")]
    public Transform[] textElements;

    [Tooltip("Metinlerin merkeze olan uzaklığı (Yarıçap)")]
    public float radius = 1.5f;

    [Tooltip("1. Metnin başlayacağı açı (90 derece = Saat 12 yönü)")]
    public float startAngle = 90f;

    [Tooltip("Tam çember mi? (360) Yoksa yarım ay şeklinde mi dizilecek? (Örn: 180)")]
    public float totalAngle = 360f;

    [Tooltip("Metinler hep düz mü dursun, yoksa merkeze doğru dönsün mü?")]
    public bool rotateTowardsCenter = false;

    // Bu özellik sayesinde editörde sağ tıklayıp metodu çalıştırabiliriz
    [ContextMenu("Metinleri Çembere Diz (Uygula)")]
    public void ArrangeInCircle()
    {
        if (textElements == null || textElements.Length == 0) return;

        // 1. Amplitude ve Frequency gibi 6'lılar için 360/6 = 60 derece, Phase (3'lü) için 360/3 = 120 derece hesaplar.
        float angleStep = totalAngle / textElements.Length;

        for (int i = 0; i < textElements.Length; i++)
        {
            if (textElements[i] == null) continue;

            // Saat yönünde (1, 2, 3...) dizmek için açıyı eksi yönde hesaplıyoruz
            float currentAngle = startAngle + (i * angleStep);

            // Unity'nin Math kütüphanesi derece değil radyan kullanır, çeviriyoruz
            float angleRad = currentAngle * Mathf.Deg2Rad;

            // X ve Y pozisyonlarını Trigonometri ile buluyoruz
            float x = Mathf.Cos(angleRad) * radius;
            float y = Mathf.Sin(angleRad) * radius;

            // Metnin pozisyonunu merkeze (bu scriptin olduğu objeye) göre ayarla
            // Not: Eğer şalterlerin Z ekseni yerine farklı bir eksende duruyorsa (X, Z gibi), x ve y değerlerinin yerini değiştirebilirsin.
            textElements[i].localPosition = new Vector3(x, y, 0f);

            // Eğer metinlerin dönerek merkeze bakmasını istersen
            if (rotateTowardsCenter)
            {
                // Metinlerin alt kısımları merkeze bakacak şekilde döndürür
                textElements[i].localRotation = Quaternion.Euler(0, 0, currentAngle - 90f);
            }
            else
            {
                // Metinler dümdüz (okunabilir) durur
                textElements[i].localRotation = Quaternion.identity;
            }
        }
    }
}