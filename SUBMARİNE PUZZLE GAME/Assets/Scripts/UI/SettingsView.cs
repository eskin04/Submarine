
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
    [SerializeField] Button quitButton;
    [SerializeField] Button stuckButton;

    void Start()
    {
        if (!NetworkManager.main.isServer)
        {
            restartButton.gameObject.SetActive(false);
        }
    }
    void OnEnable()
    {
        resumeButton.onClick.AddListener(OnResume);
        restartButton.onClick.AddListener(OnRestart);
        quitButton.onClick.AddListener(OnQuit);
        stuckButton.onClick.AddListener(OnStuck);

    }

    void OnDisable()
    {
        resumeButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
        stuckButton.onClick.RemoveAllListeners();
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


    public override void OnHide()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public override void OnShow()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

    }




}