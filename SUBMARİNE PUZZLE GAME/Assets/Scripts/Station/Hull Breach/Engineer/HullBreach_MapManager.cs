using UnityEngine;
using System.Linq;
using TMPro;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class HullBreach_MapManager : MonoBehaviour
{
    [Header("References")]
    public HullBreach_StationManager stationManager;

    [Header("UI References")]
    public TextMeshProUGUI depthText;

    private HullBreach_MapSlot[] allSlots;

    private LocalizedString uiDepthString = new LocalizedString { TableReference = "UI_General" };
    private int currentDisplayDepth = 0;

    private void Awake()
    {
        allSlots = GetComponentsInChildren<HullBreach_MapSlot>(true);
        stationManager.currentDepth.onChanged += HandleDepthChanged;

        uiDepthString.StringChanged += OnTranslatedDepthReady;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
    }

    private void OnDestroy()
    {
        if (stationManager != null)
        {
            stationManager.currentDepth.onChanged -= HandleDepthChanged;
        }
        uiDepthString.StringChanged -= OnTranslatedDepthReady;
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnTranslatedDepthReady(string text)
    {
        if (depthText != null)
        {
            depthText.text = text;
        }
    }

    private void OnLanguageChanged(Locale newLocale)
    {
        UpdateDepthLocalization();
    }

    private void UpdateDepthLocalization()
    {
        uiDepthString.Arguments = new object[] { new Dictionary<string, string> { { "Depth", currentDisplayDepth.ToString() } } };

        uiDepthString.TableEntryReference = "hull_depth";

        uiDepthString.RefreshString();
    }

    private void HandleDepthChanged(int newDepth)
    {
        currentDisplayDepth = newDepth;
        UpdateDepthLocalization();
    }

    private void Update()
    {
        if (stationManager == null || !stationManager.isRoundActive.value)
        {
            ClearAllSlots();



            return;
        }



        foreach (var slot in allSlots)
        {
            bool hasCrack = stationManager.allSockets.Any(s =>
                s.floorIndex == slot.floorIndex &&
                s.zone == slot.zone &&
                s.spawnPointIndex == slot.spawnPointIndex &&
                s.isCrackSpawned && !s.isFixed);

            slot.SetWarningState(hasCrack);
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in allSlots)
        {
            slot.SetWarningState(false);
        }
    }
}