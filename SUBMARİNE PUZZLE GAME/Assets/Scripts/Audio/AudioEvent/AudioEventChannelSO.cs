using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Audio/Audio Event Channel", fileName = "NewAudioEventChannel")]
public class AudioEventChannelSO : ScriptableObject
{
    public UnityAction<AudioEventPayload> OnAudioEventRequested;

    public void RaiseEvent(AudioEventPayload payload)
    {
        if (OnAudioEventRequested != null)
        {
            OnAudioEventRequested.Invoke(payload);
        }
        else
        {
            Debug.LogWarning($"[Audio] {name} kanalı tetiklendi ama dinleyen bir Audio Manager yok!");
        }
    }
}