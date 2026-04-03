using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    [Header("Level Verisi")]
    public LevelData levelData;

    [Header("UI Elementleri")]
    public Button myButton;
    public TextMeshProUGUI levelText;
    public GameObject lockIcon;
    public Image buttonBackgroundImage;

    [Header("Renk Ayarları")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.green;
    public Color lockedColor = Color.gray;

    public void SetupButton(bool isUnlocked, bool isHost, System.Action<int> onClickAction)
    {
        levelText.text = "Level " + levelData.levelID.ToString();
        myButton.onClick.RemoveAllListeners();

        if (isUnlocked)
        {
            lockIcon.SetActive(false);
            myButton.interactable = isHost;
            myButton.onClick.AddListener(() => onClickAction(levelData.levelID));

            buttonBackgroundImage.color = normalColor;
        }
        else
        {
            lockIcon.SetActive(true);
            myButton.interactable = false;

            buttonBackgroundImage.color = lockedColor;
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (!myButton.interactable && lockIcon.activeSelf) return;

        buttonBackgroundImage.color = isSelected ? selectedColor : normalColor;
    }
}