using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Audio/Audio Event Channel", fileName = "NewAudioEventChannel")]
public class AudioEventChannelSO : ScriptableObject
{
    // Olayı dinleyecek olan delegate
    public UnityAction<AudioEventPayload> OnAudioEventRequested;

    public void RaiseEvent(AudioEventPayload payload)
    {
        if (OnAudioEventRequested != null)
        {
            OnAudioEventRequested.Invoke(payload);
        }
        else
        {
            // Eğer sahnedeki Audio Manager henüz yüklenmediyse uyarır
            Debug.LogWarning($"[Audio] {name} kanalı tetiklendi ama dinleyen bir Audio Manager yok!");
        }
    }
}