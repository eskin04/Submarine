using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }
    [Header("Dinlenecek Kanallar")]
    [SerializeField] private AudioEventChannelSO _sfxChannel;
    [SerializeField] private AudioEventChannelSO _environmentChannel;
    [SerializeField] private AudioEventChannelSO _uiChannel;

    [Header("Havuz Ayarları")]
    [SerializeField] private FMODEmitter _emitterPrefab; // Üzerinde FMODEmitter olan boş bir prefab
    [SerializeField] private int _initialPoolSize = 10;

    // Emitter Havuzu
    private Queue<FMODEmitter> _emitterPool = new Queue<FMODEmitter>();

    private void Awake()
    {
        // Singleton Kurulumu
        if (Instance == null)
        {
            Instance = this;
            // Sahneler arası geçişte sesin kesilmemesi için (Opsiyonel)
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {


        // 2. Oyun başlarken havuzu doldur (Senin mevcut kodun)
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewEmitterForPool();
        }
    }

    private void OnEnable()
    {
        // Eski One-Shot fonksiyonlarımız aynen duruyor
        if (_sfxChannel != null) _sfxChannel.OnAudioEventRequested += Play3DSound;
        if (_environmentChannel != null) _environmentChannel.OnAudioEventRequested += Play3DSound;
        if (_uiChannel != null) _uiChannel.OnAudioEventRequested += Play2DSound;
    }

    private void OnDisable()
    {
        if (_sfxChannel != null) _sfxChannel.OnAudioEventRequested -= Play3DSound;
        if (_environmentChannel != null) _environmentChannel.OnAudioEventRequested -= Play3DSound;
        if (_uiChannel != null) _uiChannel.OnAudioEventRequested -= Play2DSound;
    }

    /// <summary>
    /// SFX ve Çevre gibi uzamsal (3D) sesleri çalan ana fonksiyon
    /// </summary>
    private void Play3DSound(AudioEventPayload payload)
    {
        if (payload.EventReference.IsNull) return;


        // 1. FMOD'dan bir ses kopyası (Instance) oluştur
        EventInstance instance = RuntimeManager.CreateInstance(payload.EventReference);

        // 2. Sesin dünyadaki 3D pozisyonunu ayarla
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(payload.Position));

        // 3. Eğer payload içinde özel bir parametre (Örn: Basınç seviyesi) varsa FMOD'a gönder
        if (!string.IsNullOrEmpty(payload.ParameterName))
        {
            instance.setParameterByName(payload.ParameterName, payload.ParameterValue);
        }

        // 4. Sesi başlat
        instance.start();

        // 5. Serbest bırak (Ses bitince FMOD belleği kendi kendine temizler)
        instance.release();
    }

    /// <summary>
    /// UI menüsü gibi pozisyondan bağımsız (2D) sesleri çalan fonksiyon
    /// </summary>
    private void Play2DSound(AudioEventPayload payload)
    {
        if (payload.EventReference.IsNull) return;

        // UI sesleri için pozisyona veya instance oluşturmaya gerek yoktur, "OneShot" yeterlidir.
        RuntimeManager.PlayOneShot(payload.EventReference);
    }

    // ====================================================
    // YENİ HAVUZ SİSTEMİ METOTLARI
    // ====================================================

    /// <summary>
    /// Sürekli çalan veya bir objeye yapışması gereken sesler için havuzdan Emitter çeker
    /// </summary>
    public FMODEmitter PlayLoopingOrAttachedSound(EventReference eventRef, Transform targetTransform)
    {
        FMODEmitter emitter = GetEmitterFromPool();

        // Emitter objesini sesin çalacağı yere taşı ve objeye bağla (Parenting)
        emitter.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        emitter.transform.SetParent(targetTransform);
        emitter.gameObject.SetActive(true);

        // Sesi başlat ve "Durdurulduğunda ReturnToPool fonksiyonunu çalıştır" de
        emitter.Play(eventRef, ReturnToPool);

        return emitter; // Kumandayı tetikleyen koda geri veriyoruz
    }

    private FMODEmitter GetEmitterFromPool()
    {
        if (_emitterPool.Count == 0)
        {
            CreateNewEmitterForPool(); // Havuz boşaldıysa yeni üret (Performans için uyarı da verebilirsin)
        }
        return _emitterPool.Dequeue();
    }

    private void ReturnToPool(FMODEmitter emitter)
    {
        emitter.gameObject.SetActive(false);
        emitter.transform.SetParent(this.transform); // AudioManager'ın altına geri al
        _emitterPool.Enqueue(emitter);
    }

    private void CreateNewEmitterForPool()
    {
        FMODEmitter newEmitter = Instantiate(_emitterPrefab, this.transform);
        newEmitter.gameObject.SetActive(false);
        _emitterPool.Enqueue(newEmitter);
    }
}