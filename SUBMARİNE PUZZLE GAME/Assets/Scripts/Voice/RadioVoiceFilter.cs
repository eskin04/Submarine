using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioHighPassFilter))]
[RequireComponent(typeof(AudioLowPassFilter))]
[RequireComponent(typeof(AudioDistortionFilter))]
public class RadioVoiceFilter : MonoBehaviour
{
    private AudioHighPassFilter highPass;
    private AudioLowPassFilter lowPass;
    private AudioDistortionFilter distortion;

    [Header("Telsiz Frekans Ayarları")]
    public float radioHighPassCutoff = 350f;
    public float radioLowPassCutoff = 3500f;
    [Range(0f, 1f)] public float radioDistortionLevel = 0.15f;

    void Awake()
    {
        highPass = GetComponent<AudioHighPassFilter>();
        lowPass = GetComponent<AudioLowPassFilter>();
        distortion = GetComponent<AudioDistortionFilter>();

        EnableRadioEffect();
    }

    public void EnableRadioEffect()
    {
        highPass.cutoffFrequency = radioHighPassCutoff;
        highPass.enabled = true;

        lowPass.cutoffFrequency = radioLowPassCutoff;
        lowPass.enabled = true;

        distortion.distortionLevel = radioDistortionLevel;
        distortion.enabled = true;
    }

    public void DisableRadioEffect()
    {
        highPass.enabled = false;
        lowPass.enabled = false;
        distortion.enabled = false;
    }
}