using UnityEngine;
using System.Collections.Generic;
using FMODUnity;

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
    FixOverrideStation

}



[System.Serializable]
public class QuestTask
{
    public string taskDescription;
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
    public string questTitle;

    [Header("Megaphone Subtitles")]
    [TextArea] public List<string> introSubtitles;
    [Header("Megaphone (Global)")]
    public EventReference megaphoneAudio;

    [Header("Tasks")]
    public List<QuestTask> technicianTasks;
    public List<QuestTask> engineerTasks;

    [Header("Waiting Phase")]

    public string waitingForTechnicianText = "Waiting for Technician";
    public string waitingForEngineerText = "Waiting for Engineer";
}