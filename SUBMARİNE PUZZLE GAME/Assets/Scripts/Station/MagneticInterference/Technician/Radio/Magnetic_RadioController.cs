using UnityEngine;
using TMPro;
using System.Globalization;
public class Magnetic_RadioController : MonoBehaviour
{
    [Header("Referanslar")]
    public Magnetic_StationManager stationManager;
    public TMP_Text frequencyDisplayText;

    private float currentFrequency = 0f;
    private bool isRadioLocked = false;

    private void OnEnable()
    {
        if (stationManager != null)
            stationManager.OnPuzzleGenerated += InitializeRadio;
    }

    private void OnDisable()
    {
        if (stationManager != null)
            stationManager.OnPuzzleGenerated -= InitializeRadio;
    }

    private void Start()
    {
        if (stationManager != null && stationManager.isRoundActive.value)
        {
            InitializeRadio();
        }
    }

    private void InitializeRadio()
    {
        isRadioLocked = false;

        currentFrequency = Random.Range(30.000f, 70.000f);

        UpdateDisplay();
    }

    public void AdjustFrequency(float amount)
    {
        if (isRadioLocked || stationManager == null || !stationManager.isRoundActive.value) return;

        currentFrequency += amount;

        currentFrequency = Mathf.Clamp(currentFrequency, 0f, 99.999f);

        currentFrequency = (float)System.Math.Round(currentFrequency, 3);

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (frequencyDisplayText != null)
        {
            frequencyDisplayText.text = currentFrequency.ToString("00.000", CultureInfo.InvariantCulture) + "hz";
        }
    }

    public void SubmitFrequency()
    {
        if (isRadioLocked || stationManager == null || !stationManager.isRoundActive.value) return;

        string submissionString = currentFrequency.ToString("00.000", CultureInfo.InvariantCulture) + "hz";

        stationManager.SubmitRadioFrequencyRPC(submissionString);
    }
}