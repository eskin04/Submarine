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
    [SerializeField] private FMODEmitter _emitterPrefab;
    [SerializeField] private int _initialPoolSize = 10;

    // Emitter Havuzu
    private Queue<FMODEmitter> _emitterPool = new Queue<FMODEmitter>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
        for (int i = 0; i < _initialPoolSize; i++)
        {
            CreateNewEmitterForPool();
        }
    }

    private void OnEnable()
    {
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

    private void Play3DSound(AudioEventPayload payload)
    {
        if (payload.EventReference.IsNull) return;


        EventInstance instance = RuntimeManager.CreateInstance(payload.EventReference);

        instance.set3DAttributes(RuntimeUtils.To3DAttributes(payload.Position));

        if (!string.IsNullOrEmpty(payload.ParameterName))
        {
            instance.setParameterByName(payload.ParameterName, payload.ParameterValue);
        }

        instance.start();

        instance.release();
    }

    private void Play2DSound(AudioEventPayload payload)
    {
        if (payload.EventReference.IsNull) return;

        RuntimeManager.PlayOneShot(payload.EventReference);
    }

    public FMODEmitter PlayLoopingOrAttachedSound(EventReference eventRef, Transform targetTransform)
    {
        FMODEmitter emitter = GetEmitterFromPool();

        emitter.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        emitter.transform.SetParent(targetTransform);
        emitter.gameObject.SetActive(true);

        emitter.Play(eventRef, ReturnToPool);

        return emitter;
    }

    private FMODEmitter GetEmitterFromPool()
    {
        if (_emitterPool.Count == 0)
        {
            CreateNewEmitterForPool();
        }
        return _emitterPool.Dequeue();
    }

    private void ReturnToPool(FMODEmitter emitter)
    {
        emitter.gameObject.SetActive(false);
        emitter.transform.SetParent(this.transform);
        _emitterPool.Enqueue(emitter);
    }

    private void CreateNewEmitterForPool()
    {
        FMODEmitter newEmitter = Instantiate(_emitterPrefab, this.transform);
        newEmitter.gameObject.SetActive(false);
        _emitterPool.Enqueue(newEmitter);
    }
}