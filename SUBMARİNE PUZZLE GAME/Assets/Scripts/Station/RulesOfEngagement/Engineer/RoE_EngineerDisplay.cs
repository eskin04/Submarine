using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class RoE_EngineerDisplay : MonoBehaviour
{
    [Header("References")]
    public Image symbolImage;
    public RoE_StationManager stationManager;
    public TextMeshProUGUI ruleText;
    public TextMeshProUGUI targetCodeText;

    private Coroutine displayCoroutine;
    private string currentDisplayedCode = "";

    public void StartDisplaySequence(List<int> symbolIndices, string codeName)
    {
        currentDisplayedCode = codeName;
        targetCodeText.text = codeName;
        targetCodeText.color = Color.yellow;
        if (displayCoroutine != null) StopCoroutine(displayCoroutine);

        displayCoroutine = StartCoroutine(SymbolLoop(symbolIndices));
    }

    private IEnumerator SymbolLoop(List<int> indices)
    {
        while (true)
        {
            foreach (int index in indices)
            {
                if (index >= 0 && index < stationManager.availableSymbols.Count)
                {
                    var symbolData = stationManager.availableSymbols[index];

                    symbolImage.sprite = symbolData.icon;
                    symbolImage.color = ConvertColor(symbolData.color);
                    symbolImage.enabled = true;
                }

                yield return new WaitForSeconds(1.0f);

                symbolImage.enabled = false;
                yield return new WaitForSeconds(0.2f);
            }

            symbolImage.enabled = false;
            yield return new WaitForSeconds(2.0f);
        }
    }

    public void OnTargetDestroyed(string destroyedCodeName)
    {
        if (currentDisplayedCode == destroyedCodeName)
        {
            if (displayCoroutine != null) StopCoroutine(displayCoroutine);

            symbolImage.enabled = false;

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

    private Color ConvertColor(DecryptionSymbol.Color col)
    {
        switch (col)
        {
            case DecryptionSymbol.Color.Red: return Color.red;
            case DecryptionSymbol.Color.Green: return Color.green;
            case DecryptionSymbol.Color.Blue: return Color.blue;
            default: return Color.white;
        }
    }
}