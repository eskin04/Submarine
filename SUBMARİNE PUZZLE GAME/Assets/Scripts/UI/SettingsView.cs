
using System;
using UnityEngine;
using UnityEngine.UI;
using PurrNet;
using StarterAssets;
public class SettingsView : View
{
    public static Action resumeGame;
    public static Action restartGame;
    public static Action quitGame;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button audioButton;

    [SerializeField] Button quitButton;
    [SerializeField] Button stuckButton;
    [SerializeField] Button backButton;
    [SerializeField] private GameObject AudioView;
    [SerializeField] private GameObject SettingView;

    void Start()
    {
        if (!NetworkManager.main.isServer)
        {
            restartButton.gameObject.SetActive(false);
        }
        AudioView.SetActive(false);
        SettingView.SetActive(true);

    }
    void OnEnable()
    {
        resumeButton.onClick.AddListener(OnResume);
        restartButton.onClick.AddListener(OnRestart);
        quitButton.onClick.AddListener(OnQuit);
        stuckButton.onClick.AddListener(OnStuck);
        audioButton.onClick.AddListener(OnAudioView);
        backButton.onClick.AddListener(OnBack);

    }

    void OnDisable()
    {
        resumeButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        stuckButton.onClick.RemoveAllListeners();
        audioButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
    }

    private void OnResume()
    {
        resumeGame?.Invoke();
    }

    private void OnRestart()
    {
        restartGame?.Invoke();
    }

    private void OnQuit()
    {
        quitGame?.Invoke();
    }

    private void OnStuck()
    {
        if (FirstPersonController.Local != null)
        {
            FirstPersonController.Local.UnstuckPlayer();

            resumeGame?.Invoke();
        }
        else
        {
            Debug.LogWarning("Yerel oyuncu bulunamadı, ışınlanma iptal edildi.");
        }
    }

    private void OnAudioView()
    {
        AudioView.SetActive(true);
        SettingView.SetActive(false);
    }

    private void OnBack()
    {
        AudioView.SetActive(false);
        SettingView.SetActive(true);
    }


    public override void OnHide()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        OnBack();
    }

    public override void OnShow()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

    }




}