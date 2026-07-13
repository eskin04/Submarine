using UnityEngine;
using System.Linq;
using TMPro; // TextMeshPro kütüphanesi eklendi

public class HullBreach_MapManager : MonoBehaviour
{
    [Header("References")]
    public HullBreach_StationManager stationManager;

    [Header("UI References")]
    public TextMeshProUGUI timerText;

    private HullBreach_MapSlot[] allSlots;

    private void Awake()
    {
        // Alt objelerdeki tüm kroki karelerini (slotları) bul ve diziye al
        allSlots = GetComponentsInChildren<HullBreach_MapSlot>(true);
    }

    private void Update()
    {
        // İstasyon yöneticisi yoksa veya raunt aktif değilse her şeyi gizle
        if (stationManager == null || !stationManager.isRoundActive.value)
        {
            ClearAllSlots();

            if (timerText != null && timerText.enabled)
                timerText.enabled = false;

            return;
        }

        // ==========================================
        // 1. ZAMANLAYICI GÜNCELLEMESİ
        // ==========================================
        if (timerText != null)
        {
            if (!timerText.enabled) timerText.enabled = true;

            timerText.text = $"UNFIXIBLE PROPERTY DAMAGE IN: {stationManager.displayTimeRemaining.value} s";

        }

        // ==========================================
        // 2. KROKİ (HARİTA) GÜNCELLEMESİ
        // ==========================================
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