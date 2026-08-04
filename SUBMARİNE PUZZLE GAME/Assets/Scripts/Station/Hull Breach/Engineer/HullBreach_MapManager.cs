using UnityEngine;
using System.Linq;
using TMPro;

public class HullBreach_MapManager : MonoBehaviour
{
    [Header("References")]
    public HullBreach_StationManager stationManager;

    [Header("UI References")]
    public TextMeshProUGUI depthText;

    private HullBreach_MapSlot[] allSlots;

    private void Awake()
    {
        allSlots = GetComponentsInChildren<HullBreach_MapSlot>(true);
        stationManager.currentDepth.onChanged += HandleDepthChanged;
    }

    private void OnDestroy()
    {
        if (stationManager != null)
        {
            stationManager.currentDepth.onChanged -= HandleDepthChanged;
        }
    }

    private void HandleDepthChanged(int newDepth)
    {
        if (depthText != null)
        {
            depthText.text = $"DEPTH: {newDepth} m";
        }
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