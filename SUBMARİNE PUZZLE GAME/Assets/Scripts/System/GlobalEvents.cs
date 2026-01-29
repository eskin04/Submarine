using System;
using UnityEngine;

public static class GlobalEvents
{
    public static Action<StationController> OnRegisterMainStation;

    public static Action<StationController, UtilityEventData> OnRegisterUtilityStation;

    public static Action<StationController, StationState> OnStationStatusChanged;

    public static Action<float> OnAddStress;

    public static Action<float> OnReduceStress;

    public static Action<string, bool> OnShowSystemMessage;
    public static Action<float> OnAddFloodPenalty;

    public static Action<StatType, int> OnModifyStat;
}