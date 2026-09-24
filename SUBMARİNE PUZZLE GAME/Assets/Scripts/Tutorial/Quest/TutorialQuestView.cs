using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using PurrNet;
using PurrLobby;
using System.Threading;
using FMODUnity;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System;

public class TutorialQuestView : View
{
    public static event Action OnIntroSequenceCompleted;
    public static event Action<TutorialAction> OnTaskUnlockedLocal;

    [Header("UI References")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI subtitleText;
    public Transform taskContainer;
    public GameObject taskTextPrefab;
    public TextMeshProUGUI waitingStatusText;


    private PlayerRole myRole;
    private TutorialQuestData currentQuestData;
    private bool isWaitingForPartner = false;
    private int completedTasksCount = 0;
    private CancellationTokenSource sequenceCancellationToken;

    private FMOD.Studio.EventInstance megaphoneInstance;
    private FMOD.Studio.EVENT_CALLBACK markerCallback;

    // Thread-Safe (Güvenli) komut kuyruğu
    private ConcurrentQueue<string> fmodCommandQueue = new ConcurrentQueue<string>();
    private Dictionary<TutorialAction, bool> unlockedTasks = new Dictionary<TutorialAction, bool>();

    private Dictionary<TutorialAction, int> currentProgress = new Dictionary<TutorialAction, int>();
    private Dictionary<TutorialAction, QuestTask> activeTasks = new Dictionary<TutorialAction, QuestTask>();
    private Dictionary<TutorialAction, TextMeshProUGUI> taskUIElements = new Dictionary<TutorialAction, TextMeshProUGUI>();

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        markerCallback = new FMOD.Studio.EVENT_CALLBACK(MegaphoneCallback);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<TutorialQuestView>();
    }

    private void OnEnable() => TutorialManager.OnPlayerProgressUpdated += HandlePartnerProgress;

    private void OnDisable() => TutorialManager.OnPlayerProgressUpdated -= HandlePartnerProgress;

    public override void OnShow() { }
    public override void OnHide() { }

    public void SetPlayerRole(PlayerRole role)
    {
        myRole = role;
        Debug.Log($"<color=green>[Tutorial]</color> Player role set to: {myRole}");
    }

    public void LoadQuest(TutorialQuestData newQuest)
    {
        currentQuestData = newQuest;
        List<GameObject> oldTaskObjects = new List<GameObject>();
        foreach (Transform child in taskContainer)
        {
            oldTaskObjects.Add(child.gameObject);
        }
        string oldTitle = questTitleText != null ? questTitleText.text : "";
        DOVirtual.DelayedCall(2.5f, () =>
        {
            foreach (var oldObj in oldTaskObjects)
            {
                if (oldObj != null)
                {
                    var tmp = oldObj.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.DOFade(0f, 0.5f).OnComplete(() => Destroy(oldObj));
                    }
                    else
                    {
                        Destroy(oldObj);
                    }
                }
            }
            DOVirtual.DelayedCall(0.3f, () =>
            {
                taskContainer.gameObject.SetActive(false);

            });

            if (taskContainer.childCount <= oldTaskObjects.Count && questTitleText != null)
            {
                questTitleText.DOFade(0f, 0.5f);
            }
        });
        activeTasks.Clear();
        currentProgress.Clear();
        taskUIElements.Clear();
        isWaitingForPartner = false;
        completedTasksCount = 0;
        unlockedTasks.Clear();
        if (waitingStatusText != null) waitingStatusText.gameObject.SetActive(false);

        if (megaphoneInstance.isValid())
        {
            megaphoneInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            megaphoneInstance.release();
        }

        fmodCommandQueue = new ConcurrentQueue<string>();

        if (!currentQuestData.megaphoneAudio.IsNull)
        {
            megaphoneInstance = RuntimeManager.CreateInstance(currentQuestData.megaphoneAudio);

            megaphoneInstance.setCallback(markerCallback, FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER | FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED);
            megaphoneInstance.start();
            megaphoneInstance.release();
        }
        else
        {
            fmodCommandQueue.Enqueue("AUDIO_STOPPED");
        }
    }



    private void PopulateTasks()
    {
        if (questTitleText != null && currentQuestData != null)
        {
            questTitleText.text = currentQuestData.questTitle;
        }
        var myTasks = (myRole == PlayerRole.Technician) ? currentQuestData.technicianTasks : currentQuestData.engineerTasks;

        foreach (var task in myTasks)
        {
            activeTasks.Add(task.actionType, task);
            currentProgress.Add(task.actionType, 0);
            unlockedTasks.Add(task.actionType, !task.isHiddenInitially);

            GameObject newTaskUI = Instantiate(taskTextPrefab, taskContainer);
            TextMeshProUGUI tmp = newTaskUI.GetComponent<TextMeshProUGUI>();
            tmp.text = task.requiredAmount > 1 ? $"{task.taskDescription} (0/{task.requiredAmount})" : task.taskDescription;

            if (task.isHiddenInitially) newTaskUI.SetActive(false);
            taskUIElements.Add(task.actionType, tmp);
        }
    }

    private void Update()
    {
        while (fmodCommandQueue.TryDequeue(out string command))
        {
            if (command == "AUDIO_STOPPED")
            {
                subtitleText.text = "";
                if (questTitleText != null && currentQuestData != null)
                {
                    questTitleText.text = currentQuestData.questTitle;
                    questTitleText.DOFade(1f, 0.5f);
                }
                PopulateTasks();
                taskContainer.gameObject.SetActive(true);
                foreach (var task in activeTasks.Values)
                {
                    if (!task.isHiddenInitially)
                    {
                        AnimateTaskEntry(taskUIElements[task.actionType]);
                        OnTaskUnlockedLocal?.Invoke(task.actionType);
                    }
                }
                OnIntroSequenceCompleted?.Invoke();
            }
            else if (int.TryParse(command, out int markerIndex))
            {
                if (currentQuestData.introSubtitles != null && markerIndex < currentQuestData.introSubtitles.Count)
                {

                    subtitleText.DOFade(0, 0.3f).OnComplete(() =>
                    {
                        subtitleText.text = $"{currentQuestData.introSubtitles[markerIndex]}";
                        subtitleText.DOFade(1, 0.3f);
                    });

                }
            }
        }
        if (currentQuestData == null || canvasGroup.alpha == 0 || isWaitingForPartner) return;

        if (unlockedTasks.ContainsKey(TutorialAction.WalkWASD) && unlockedTasks[TutorialAction.WalkWASD] && currentProgress[TutorialAction.WalkWASD] < activeTasks[TutorialAction.WalkWASD].requiredAmount)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
                OnActionPerformed(TutorialAction.WalkWASD);
        }

        if (unlockedTasks.ContainsKey(TutorialAction.Jump) && unlockedTasks[TutorialAction.Jump] && currentProgress[TutorialAction.Jump] < activeTasks[TutorialAction.Jump].requiredAmount)
        {
            if (Input.GetKeyDown(KeyCode.Space))
                OnActionPerformed(TutorialAction.Jump);
        }
    }

    public void OnActionPerformed(TutorialAction actionType)
    {
        if (unlockedTasks.ContainsKey(actionType) && !unlockedTasks[actionType]) return;
        if (!activeTasks.ContainsKey(actionType) || currentProgress[actionType] >= activeTasks[actionType].requiredAmount) return;

        currentProgress[actionType]++;
        QuestTask task = activeTasks[actionType];
        TextMeshProUGUI tmp = taskUIElements[actionType];

        if (currentProgress[actionType] >= task.requiredAmount)
        {
            tmp.text = task.requiredAmount > 1
                ? $"<sprite name=check> {task.taskDescription} ({task.requiredAmount}/{task.requiredAmount}) "
                : $"<sprite name=check> {task.taskDescription}";
            tmp.DOColor(Color.green, 0.3f);
            tmp.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 10, .5f);

            foreach (var checkTask in activeTasks.Values)
            {
                if (checkTask.isHiddenInitially && checkTask.revealsAfter == actionType)
                {
                    if (!unlockedTasks[checkTask.actionType])
                    {
                        unlockedTasks[checkTask.actionType] = true;

                        var revealedTmp = taskUIElements[checkTask.actionType];
                        revealedTmp.gameObject.SetActive(true);

                        AnimateTaskEntry(revealedTmp);
                        OnTaskUnlockedLocal?.Invoke(checkTask.actionType);
                    }
                }
            }

            completedTasksCount++;
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.UpdateProgressServerRpc((int)myRole, completedTasksCount);
            }

            CheckAllTasksCompleted();
        }
        else
        {
            tmp.text = $"{task.taskDescription} ({currentProgress[actionType]}/{task.requiredAmount})";
        }
    }

    private void CheckAllTasksCompleted()
    {
        if (completedTasksCount < activeTasks.Count) return;

        isWaitingForPartner = true;

        if (waitingStatusText != null)
        {
            waitingStatusText.gameObject.SetActive(true);
            int partnerProgress = (myRole == PlayerRole.Engineer)
                ? TutorialManager.Instance.technicianTaskProgress.value
                : TutorialManager.Instance.engineerTaskProgress.value;

            UpdateWaitingTextUI(partnerProgress);
        }

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.PlayerReadyServerRpc((int)myRole);
        }
    }

    private void HandlePartnerProgress(PlayerRole role, int progress)
    {
        if (isWaitingForPartner && role != myRole)
        {
            UpdateWaitingTextUI(progress);
        }
    }

    private void UpdateWaitingTextUI(int partnerCompletedTasks)
    {
        if (waitingStatusText == null || currentQuestData == null) return;

        string baseText = (myRole == PlayerRole.Engineer)
            ? currentQuestData.waitingForTechnicianText
            : currentQuestData.waitingForEngineerText;

        int partnerTotalTasks = (myRole == PlayerRole.Engineer)
            ? currentQuestData.technicianTasks.Count
            : currentQuestData.engineerTasks.Count;

        waitingStatusText.text = $"{baseText} ({partnerCompletedTasks}/{partnerTotalTasks})";
    }

    public bool IsTaskActive(TutorialAction actionType)
    {
        return activeTasks.ContainsKey(actionType) && unlockedTasks.ContainsKey(actionType) && unlockedTasks[actionType];
    }

    public bool IsTaskCompleted(TutorialAction actionType)
    {
        return activeTasks.ContainsKey(actionType) && currentProgress[actionType] >= activeTasks[actionType].requiredAmount;
    }

    public bool ShouldBlockAction(TutorialAction actionType)
    {
        if (activeTasks.ContainsKey(actionType) && unlockedTasks.ContainsKey(actionType))
        {
            return !unlockedTasks[actionType];
        }

        return false;
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    private static FMOD.RESULT MegaphoneCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var view))
        {
            if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
            {
                var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                view.fmodCommandQueue.Enqueue(parameter.name);
            }
            else if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED)
            {
                view.fmodCommandQueue.Enqueue("AUDIO_STOPPED");
            }
        }
        return FMOD.RESULT.OK;
    }

    private void AnimateTaskEntry(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        Color c = tmp.color; c.a = 0; tmp.color = c;
        tmp.DOFade(1f, 0.6f).From(0).SetEase(Ease.OutQuad);

        Vector4 originalMargin = tmp.margin;
        tmp.margin = new Vector4(originalMargin.x + 50f, originalMargin.y, originalMargin.z, originalMargin.w);
        DOTween.To(() => tmp.margin, x => tmp.margin = x, originalMargin, 0.6f).SetEase(Ease.OutQuad);
    }
}