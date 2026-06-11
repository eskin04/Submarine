using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Magnetic_EngineerUI : MonoBehaviour
{
    [Header("References")]
    public Magnetic_StationManager stationManager;
    public Magnetic_SymbolDatabase symbolDatabase;

    [Header("Channel Navigation")]
    public TextMeshProUGUI channelIndicatorText;
    public Magnetic_EngChannelButton[] channelButtons;

    public TextMeshProUGUI currentVariableText;

    public TextMeshProUGUI frequencyFormatText;

    public TextMeshProUGUI leftEquationText;
    public Image symbolImage;
    public TextMeshProUGUI rightEquationText;

    private int currentlyViewedChannel = 0;

    private void OnEnable()
    {
        if (stationManager != null)
        {
            stationManager.OnPuzzleGenerated += HandleNewPuzzle;
            stationManager.OnEngChannelAdvanced += HandleChannelAdvanced;
            stationManager.OnFrequencyFormatReceived += HandleFrequencyFormat;
        }
    }

    private void OnDisable()
    {
        if (stationManager != null)
        {
            stationManager.OnPuzzleGenerated -= HandleNewPuzzle;
            stationManager.OnEngChannelAdvanced -= HandleChannelAdvanced;
            stationManager.OnFrequencyFormatReceived -= HandleFrequencyFormat;
        }
    }

    private void Start()
    {
        if (stationManager != null && stationManager.isRoundActive.value)
        {
            HandleNewPuzzle();
        }
    }

    private void HandleNewPuzzle()
    {
        currentlyViewedChannel = 0;
        ChangeViewedChannelInternal(0, 0);


    }

    private void HandleFrequencyFormat(string formatString)
    {
        if (frequencyFormatText != null)
        {
            frequencyFormatText.text = formatString;
        }
    }

    // ==========================================

    public void ChangeViewedChannel(int channelIndex)
    {
        int maxUnlocked = stationManager != null ? stationManager.engCurrentChannel.value : 0;
        ChangeViewedChannelInternal(channelIndex, maxUnlocked);
    }

    private void ChangeViewedChannelInternal(int channelIndex, int maxUnlocked)
    {
        if (channelIndex > maxUnlocked || channelIndex > 2) return;

        currentlyViewedChannel = channelIndex;

        if (channelIndicatorText != null)
            channelIndicatorText.text = $"CH-{currentlyViewedChannel + 1}";

        UpdateNavigationButtons(maxUnlocked);
        UpdateVariableStatus(currentlyViewedChannel, maxUnlocked);
        LoadChannelEquation(currentlyViewedChannel);
    }

    private void LoadChannelEquation(int channelIndex)
    {
        ChannelData data = stationManager.GetChannelData(channelIndex);
        string eqStr = data.equation.displayString;

        int sIndex = eqStr.IndexOf('S');

        if (sIndex != -1)
        {
            string leftStr = eqStr.Substring(0, sIndex).Trim();
            if (string.IsNullOrEmpty(leftStr)) leftEquationText.gameObject.SetActive(false);
            else { leftEquationText.gameObject.SetActive(true); leftEquationText.text = leftStr; }

            int symbolId = int.Parse(eqStr.Substring(sIndex + 1, 1));
            if (symbolDatabase != null) { symbolImage.sprite = symbolDatabase.GetSymbol(symbolId); symbolImage.gameObject.SetActive(true); }

            string rightStr = eqStr.Substring(sIndex + 2).Trim();
            if (string.IsNullOrEmpty(rightStr)) rightEquationText.gameObject.SetActive(false);
            else { rightEquationText.gameObject.SetActive(true); rightEquationText.text = rightStr; }
        }
        else
        {
            leftEquationText.gameObject.SetActive(true);
            leftEquationText.text = eqStr;
            symbolImage.gameObject.SetActive(false);
            rightEquationText.gameObject.SetActive(false);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(leftEquationText.transform.parent as RectTransform);
    }

    private void UpdateVariableStatus(int channelIndex, int maxUnlocked)
    {
        if (currentVariableText == null) return;

        char[] varNames = new char[] { 'X', 'Y', 'Z' };

        if (channelIndex < maxUnlocked)
        {
            int correctAnswer = stationManager.GetChannelData(channelIndex).equation.targetAnswer;
            currentVariableText.text = $"{varNames[channelIndex]} = {correctAnswer}";
        }
        else
        {
            currentVariableText.text = $"{varNames[channelIndex]} = ?";
        }
    }

    private void UpdateNavigationButtons(int maxUnlocked)
    {
        for (int i = 0; i < channelButtons.Length; i++)
        {
            if (channelButtons[i] != null)
            {
                bool isThisActive = (i == currentlyViewedChannel);
                bool isThisLocked = (i > maxUnlocked);
                channelButtons[i].UpdateButtonState(isThisActive, isThisLocked);
            }
        }
    }

    private void HandleChannelAdvanced(int newChannelIndex)
    {
        if (newChannelIndex >= 3)
        {
            UpdateVariableStatus(currentlyViewedChannel, newChannelIndex);
            UpdateNavigationButtons(newChannelIndex);
            return;
        }

        if (currentlyViewedChannel == newChannelIndex - 1)
        {
            ChangeViewedChannelInternal(newChannelIndex, newChannelIndex);
        }
        else
        {
            UpdateNavigationButtons(newChannelIndex);
            UpdateVariableStatus(currentlyViewedChannel, newChannelIndex);
        }
    }

    public bool IsViewingActiveChannel()
    {
        if (stationManager == null) return false;
        return currentlyViewedChannel == stationManager.engCurrentChannel.value;
    }
}