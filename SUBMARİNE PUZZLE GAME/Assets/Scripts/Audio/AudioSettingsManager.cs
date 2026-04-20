using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider musicSlider;
    public Slider uiSlider;
    public Slider ambienceSlider;

    // FMOD VCA Referansları
    private FMOD.Studio.VCA _masterVCA;
    private FMOD.Studio.VCA _sfxVCA;
    private FMOD.Studio.VCA _musicVCA;
    private FMOD.Studio.VCA _uiVCA;
    private FMOD.Studio.VCA _ambienceVCA;


    private void Start()
    {
        _masterVCA = RuntimeManager.GetVCA("vca:/Master");
        _sfxVCA = RuntimeManager.GetVCA("vca:/SFX");
        _musicVCA = RuntimeManager.GetVCA("vca:/Music");
        _uiVCA = RuntimeManager.GetVCA("vca:/UI");
        _ambienceVCA = RuntimeManager.GetVCA("vca:/Ambience");


        float savedMaster = PlayerPrefs.GetFloat("Volume_Master", 1f);
        float savedSFX = PlayerPrefs.GetFloat("Volume_SFX", 1f);
        float savedMusic = PlayerPrefs.GetFloat("Volume_Music", 1f);
        float savedUI = PlayerPrefs.GetFloat("Volume_UI", 1f);
        float savedAmbience = PlayerPrefs.GetFloat("Volume_Ambience", 1f);

        _masterVCA.setVolume(savedMaster);
        _sfxVCA.setVolume(savedSFX);
        _musicVCA.setVolume(savedMusic);
        _uiVCA.setVolume(savedUI);
        _ambienceVCA.setVolume(savedAmbience);

        if (masterSlider != null) masterSlider.value = savedMaster;
        if (sfxSlider != null) sfxSlider.value = savedSFX;
        if (musicSlider != null) musicSlider.value = savedMusic;
        if (uiSlider != null) uiSlider.value = savedUI;
        if (ambienceSlider != null) ambienceSlider.value = savedAmbience;

    }



    public void SetMasterVolume()
    {
        float volume = masterSlider.value;
        _masterVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_Master", volume);
    }

    public void SetSFXVolume()
    {
        float volume = sfxSlider.value;

        _sfxVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_SFX", volume);
    }

    public void SetMusicVolume()
    {
        float volume = musicSlider.value;

        _musicVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_Music", volume);
    }

    public void SetUIVolume()
    {
        float volume = uiSlider.value;

        _uiVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_UI", volume);
    }

    public void SetAmbienceVolume()
    {
        float volume = ambienceSlider.value;

        _ambienceVCA.setVolume(volume);
        PlayerPrefs.SetFloat("Volume_Ambience", volume);
    }
}