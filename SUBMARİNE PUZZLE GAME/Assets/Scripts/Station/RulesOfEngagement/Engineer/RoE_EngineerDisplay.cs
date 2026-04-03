using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RoE_EngineerDisplay : MonoBehaviour
{
    [Header("References")]
    public Image[] symbolImages;
    public RoE_StationManager stationManager;
    public TextMeshProUGUI ruleText;
    public TextMeshProUGUI targetCodeText;

    private string currentDisplayedCode = "";

    public void StartDisplaySequence(List<int> symbolIndices, string codeName)
    {
        currentDisplayedCode = codeName;
        targetCodeText.text = codeName;


        DisplayAllSymbols(symbolIndices);
    }

    private void DisplayAllSymbols(List<int> indices)
    {
        for (int i = 0; i < symbolImages.Length; i++)
        {
            symbolImages[i].enabled = false;
        }

        for (int i = 0; i < indices.Count; i++)
        {
            if (i >= symbolImages.Length) break;

            int index = indices[i];

            if (index >= 0 && index < stationManager.availableSymbols.Count)
            {
                var symbolData = stationManager.availableSymbols[index];

                symbolImages[i].sprite = symbolData.icon;
                symbolImages[i].enabled = true;
            }
        }
    }

    public void OnTargetDestroyed(string destroyedCodeName)
    {
        if (currentDisplayedCode == destroyedCodeName)
        {

            for (int i = 0; i < symbolImages.Length; i++)
            {
                symbolImages[i].enabled = false;
            }

            if (targetCodeText != null)
            {
                targetCodeText.text = "SIGNAL LOST";
                targetCodeText.color = Color.red;
            }

            currentDisplayedCode = "";
        }
    }

    public void UpdateRuleDisplay(string description)
    {
        if (ruleText != null)
        {
            ruleText.text = description;
        }
    }
}