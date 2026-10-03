using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine.Localization;

public enum TutorialAction
{
    WalkWASD, Jump, InteractNotebook, DrawNotebook, TurnPage, RipPage, CloseNotebook,
    UseElevator, AttachPage,

    CycleInventory,
    SendElevatorItem,
    WaitForPartner,
    PickUpItem,
    ViewAttachPoint,
    RadioTalk,
    RadioListen,
    OpenDoorLever,
    WaitDoorOpen,
    PickUpManual,
    OpenManual,
    TurnManualPage,
    FixOverrideStation,
    ReadContract,
    SignContract

}



[System.Serializable]
public class QuestTask
{
    public LocalizedString localizedTaskDescription;
    public TutorialAction actionType;
    public int requiredAmount = 1;
    public bool isHiddenInitially = false;
    public TutorialAction revealsAfter = TutorialAction.InteractNotebook;
}

[CreateAssetMenu(fileName = "NewQuestData", menuName = "Tutorial/Quest Data")]
public class TutorialQuestData : ScriptableObject
{
    [Header("Quest Info")]
    public int questIndex;
    public LocalizedString localizedQuestTitle;

    [Header("Megaphone Subtitles")]
    public List<LocalizedString> localizedIntroSubtitles;
    [Header("Megaphone (Global)")]
    public EventReference megaphoneAudio;

    [Header("Tasks")]
    public List<QuestTask> technicianTasks;
    public List<QuestTask> engineerTasks;

    [Header("Waiting Phase")]

    public LocalizedString localizedWaitingForTechText;
    public LocalizedString localizedWaitingForEngText;
}