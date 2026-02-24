
using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsView : View
{
    public static Action resumeGame;
    public static Action restartGame;
    public static Action quitGame;
    [SerializeField] Button resumeButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button quitButton;


    void OnEnable()
    {
        resumeButton.onClick.AddListener(OnResume);
        restartButton.onClick.AddListener(OnRestart);
        quitButton.onClick.AddListener(OnQuit);

    }

    void OnDisable()
    {
        resumeButton.onClick.RemoveAllListeners();
        restartButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();
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