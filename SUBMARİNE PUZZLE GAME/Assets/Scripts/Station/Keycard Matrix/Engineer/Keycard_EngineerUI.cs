using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Keycard_EngineerUI : MonoBehaviour
{
    [Header("Referanslar")]
    public TextMeshProUGUI infoText;

    public void HandlePuzzleSync(List<CardData> syncedCards)
    {
        SetWaitingForInput();
    }

    public void SetWaitingForInput()
    {
        if (infoText != null)
            infoText.text = "WAITING FOR INPUT...";
    }

    public void UpdateSocketVisual(int cardID, ConditionData condition)
    {
        if (cardID == -1)
        {
            SetWaitingForInput();
        }
        else if (infoText != null)
        {
            infoText.text = Keycard_ConditionParser.ParseToEnglish(condition);
        }
    }
}