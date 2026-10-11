using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using PurrNet;
using PurrLobby;
using FMODUnity;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class TutorialQuestView : View
{
    public static event Action OnIntroSequenceCompleted;
    internal static event Action<TutorialQuestView, int> IntroSequenceCompleted;
    public static event Action<TutorialAction> OnTaskUnlockedLocal;

    [Header("UI References")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI subtitleText;
    public Transform taskContainer;
    public GameObject taskTextPrefab;
    public TextMeshProUGUI waitingStatusText;

    private PlayerRole myRole;
    private bool hasPlayerRole;
    internal bool HasPlayerRole => hasPlayerRole && PlayerStats.LocalInstance != null &&
        PlayerStats.LocalInstance.isSpawned && PlayerStats.LocalInstance.isOwner &&
        PlayerStats.LocalInstance.Role == myRole;
    private TutorialQuestData currentQuestData;
    private bool isWaitingForPartner = false;
    private int completedTasksCount = 0;
    private sealed class QuestSequence
    {
        public readonly ConcurrentQueue<string> Commands = new ConcurrentQueue<string>();
        public bool IntroCompleted;
    }

    private QuestSequence activeSequence;
    private int sequenceId;
    private TutorialManager questOwner;
    private int originatingQuestIndex;
    private ulong questEntryId;
    private TutorialManager latestEntryOwner;
    private ulong latestEntryId;
    private int partnerCompletedTasks;
    private readonly List<Tween> sequenceTweens = new List<Tween>();
    private Tween subtitleTween;
    private Tween titleCleanupTween;
    internal int SequenceId => sequenceId;

    private FMOD.Studio.EventInstance megaphoneInstance;
    private static readonly FMOD.Studio.EVENT_CALLBACK markerCallback = MegaphoneCallback;
    private Dictionary<TutorialAction, bool> unlockedTasks = new Dictionary<TutorialAction, bool>();

    private Dictionary<TutorialAction, int> currentProgress = new Dictionary<TutorialAction, int>();
    private Dictionary<TutorialAction, QuestTask> activeTasks = new Dictionary<TutorialAction, QuestTask>();
    private Dictionary<TutorialAction, TextMeshProUGUI> taskUIElements = new Dictionary<TutorialAction, TextMeshProUGUI>();

    private int currentSubtitleIndex = -1;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        CancelSequence();
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var current) && ReferenceEquals(current, this))
            InstanceHandler.UnregisterInstance<TutorialQuestView>();
    }

    private void OnEnable()
    {
        TutorialManager.OnPlayerProgressUpdated += HandlePartnerProgress;
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;
        var player = PlayerStats.LocalInstance;
        if (player != null && player.isSpawned && player.isOwner) SetPlayerRole(player.Role);
    }

    private void OnDisable()
    {
        CancelSequence();
        TutorialManager.OnPlayerProgressUpdated -= HandlePartnerProgress;
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    public override void OnShow() { }
    public override void OnHide() { CancelSequence(); }

    private void OnLanguageChanged(Locale newLocale)
    {
        RefreshAllTexts();
    }

    private void RefreshAllTexts()
    {
        if (currentQuestData == null) return;

        if (questTitleText != null && questTitleText.alpha > 0)
        {
            questTitleText.text = currentQuestData.localizedQuestTitle.GetLocalizedString();
        }

        if (currentSubtitleIndex >= 0 && subtitleText != null && subtitleText.alpha > 0)
        {
            subtitleText.text = currentQuestData.localizedIntroSubtitles[currentSubtitleIndex].GetLocalizedString();
        }

        foreach (var action in activeTasks.Keys)
        {
            UpdateTaskUIText(action);
        }

        if (isWaitingForPartner && waitingStatusText != null && waitingStatusText.gameObject.activeSelf)
        {
            UpdateWaitingTextUI(partnerCompletedTasks);
        }
    }

    private void UpdateTaskUIText(TutorialAction actionType)
    {
        if (!activeTasks.ContainsKey(actionType) || !taskUIElements.ContainsKey(actionType)) return;

        QuestTask task = activeTasks[actionType];
        TextMeshProUGUI tmp = taskUIElements[actionType];
        int current = currentProgress[actionType];

        string baseDesc = task.localizedTaskDescription.GetLocalizedString();

        if (current >= task.requiredAmount)
        {
            tmp.text = task.requiredAmount > 1
                ? $"<sprite name=check> {baseDesc} ({task.requiredAmount}/{task.requiredAmount})"
                : $"<sprite name=check> {baseDesc}";
        }
        else
        {
            tmp.text = task.requiredAmount > 1
                ? $"{baseDesc} ({current}/{task.requiredAmount})"
                : baseDesc;
        }
    }

    public void SetPlayerRole(PlayerRole role)
    {
        if (role != PlayerRole.Engineer && role != PlayerRole.Technician) return;
        myRole = role;
        hasPlayerRole = true;
        Debug.Log($"<color=green>[Tutorial]</color> Player role set to: {myRole}");
        TutorialManager.Instance?.TryPresentCurrentQuest();
    }

    public bool LoadQuest(TutorialQuestData newQuest, TutorialManager owner, int questIndex, ulong entryId)
    {
        if (!isActiveAndEnabled || !HasPlayerRole || owner == null || owner != TutorialManager.Instance || !owner.isSpawned ||
            newQuest == null || newQuest.questIndex != questIndex || entryId == 0 ||
            owner.CurrentQuestState == null || owner.CurrentQuestState.questData != newQuest ||
            !owner.MatchesPresentationContext(owner.CurrentQuestState, questIndex, entryId) ||
            (latestEntryOwner == owner && entryId <= latestEntryId)) return false;
        CancelSequence();
        questOwner = owner;
        originatingQuestIndex = questIndex;
        questEntryId = entryId;
        latestEntryOwner = owner;
        latestEntryId = entryId;
        var sequence = new QuestSequence();
        activeSequence = sequence;
        sequenceId++;
        currentQuestData = newQuest;
        currentSubtitleIndex = -1;

        List<GameObject> oldTaskObjects = new List<GameObject>();
        foreach (Transform child in taskContainer)
        {
            oldTaskObjects.Add(child.gameObject);
        }

        TrackTween(DOVirtual.DelayedCall(2.5f, () =>
        {
            if (!IsCurrentSequence(sequence)) return;
            foreach (var oldObj in oldTaskObjects)
            {
                if (oldObj != null)
                {
                    var tmp = oldObj.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        TrackTween(tmp.DOFade(0f, 0.5f).OnComplete(() =>
                        {
                            if (IsCurrentSequence(sequence) && oldObj != null) Destroy(oldObj);
                        }));
                    }
                    else
                    {
                        Destroy(oldObj);
                    }
                }
            }
            TrackTween(DOVirtual.DelayedCall(0.3f, () =>
            {
                if (IsCurrentSequence(sequence) && !sequence.IntroCompleted)
                    taskContainer.gameObject.SetActive(false);
            }));

            if (!sequence.IntroCompleted && taskContainer.childCount <= oldTaskObjects.Count && questTitleText != null)
            {
                titleCleanupTween = questTitleText.DOFade(0f, 0.5f);
                TrackTween(titleCleanupTween);
            }
        }));

        activeTasks.Clear();
        currentProgress.Clear();
        taskUIElements.Clear();
        isWaitingForPartner = false;
        completedTasksCount = 0;
        unlockedTasks.Clear();
        if (waitingStatusText != null) waitingStatusText.gameObject.SetActive(false);

        if (!currentQuestData.megaphoneAudio.IsNull)
        {
            megaphoneInstance = RuntimeManager.CreateInstance(currentQuestData.megaphoneAudio);
            var handle = GCHandle.Alloc(sequence);
            if (!CheckAudioResult(megaphoneInstance.setUserData(GCHandle.ToIntPtr(handle)), "setUserData"))
            {
                handle.Free();
                ReleaseAudio(false);
                return true;
            }
            if (!CheckAudioResult(megaphoneInstance.setCallback(markerCallback,
                FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER | FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED |
                FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED), "setCallback"))
            {
                megaphoneInstance.setUserData(IntPtr.Zero);
                handle.Free();
                ReleaseAudio(false);
                return true;
            }
            if (!CheckAudioResult(megaphoneInstance.start(), "start")) ReleaseAudio(true);
        }
        else
        {
            sequence.Commands.Enqueue("AUDIO_STOPPED");
        }
        return true;
    }

    public bool TryGetQuestContext(out TutorialManager owner, out int questIndex, out ulong entryId)
    {
        owner = questOwner;
        questIndex = originatingQuestIndex;
        entryId = questEntryId;
        return MatchesQuestContext(owner, questIndex, entryId);
    }

    public bool MatchesQuestContext(TutorialManager owner, int questIndex, ulong entryId)
    {
        return activeSequence != null && owner != null && owner == questOwner &&
            owner == TutorialManager.Instance && owner.isActiveAndEnabled && owner.isSpawned &&
            entryId != 0 && questEntryId == entryId && originatingQuestIndex == questIndex &&
            currentQuestData != null && currentQuestData.questIndex == questIndex &&
            owner.CurrentQuestState != null && owner.CurrentQuestState.isActiveAndEnabled &&
            owner.CurrentQuestState.questData == currentQuestData;
    }

    internal bool HasSequence(int id) => activeSequence != null && sequenceId == id;

    internal void CancelQuest(int id)
    {
        if (HasSequence(id)) CancelSequence();
    }

    private bool IsCurrentSequence(QuestSequence sequence) => this != null &&
        sequence != null && ReferenceEquals(activeSequence, sequence);

    private void TrackTween(Tween tween)
    {
        sequenceTweens.RemoveAll(existing => existing == null || !existing.IsActive());
        // Retained handles must never be recycled into a different quest's tween.
        tween.SetRecyclable(false);
        sequenceTweens.Add(tween);
    }

    private void CancelSequence()
    {
        // Invalidate first: queued native callbacks and killed tweens must not own a new quest.
        var sequence = activeSequence;
        activeSequence = null;
        questOwner = null;
        questEntryId = 0;
        partnerCompletedTasks = 0;
        foreach (var tween in sequenceTweens) tween?.Kill(false);
        sequenceTweens.Clear();
        // Exit can interrupt the final task's green tween before its first visible update.
        if (sequence != null)
        {
            foreach (var task in activeTasks.Values)
            {
                if (currentProgress.TryGetValue(task.actionType, out var progress) && progress >= task.requiredAmount &&
                    taskUIElements.TryGetValue(task.actionType, out var tmp) && tmp != null)
                    tmp.color = Color.green;
            }
        }
        subtitleTween = null;
        titleCleanupTween = null;
        ReleaseAudio(true);
    }

    private void ReleaseAudio(bool stop)
    {
        var instance = megaphoneInstance;
        megaphoneInstance.clearHandle();
        if (!instance.isValid()) return;
        if (stop) CheckAudioResult(instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE), "stop");
        CheckAudioResult(instance.release(), "release");
        // The native DESTROYED callback owns the user-data GCHandle until FMOD is done with it.
    }

    private static bool CheckAudioResult(FMOD.RESULT result, string operation)
    {
        if (result == FMOD.RESULT.OK) return true;
        Debug.LogError($"[Tutorial] Megaphone {operation} failed: {result}");
        return false;
    }

    private void PopulateTasks()
    {
        if (questTitleText != null && currentQuestData != null)
        {
            questTitleText.text = currentQuestData.localizedQuestTitle.GetLocalizedString();
        }

        var myTasks = (myRole == PlayerRole.Technician) ? currentQuestData.technicianTasks : currentQuestData.engineerTasks;

        foreach (var task in myTasks)
        {
            activeTasks.Add(task.actionType, task);
            currentProgress.Add(task.actionType, 0);
            unlockedTasks.Add(task.actionType, !task.isHiddenInitially);

            GameObject newTaskUI = Instantiate(taskTextPrefab, taskContainer);
            TextMeshProUGUI tmp = newTaskUI.GetComponent<TextMeshProUGUI>();

            taskUIElements.Add(task.actionType, tmp);

            UpdateTaskUIText(task.actionType);

            if (task.isHiddenInitially) newTaskUI.SetActive(false);
        }
    }

    private void Update()
    {
        var sequence = activeSequence;
        while (IsCurrentSequence(sequence) && sequence.Commands.TryDequeue(out string command))
        {
            if (command == "AUDIO_STOPPED")
            {
                if (sequence.IntroCompleted) continue;
                sequence.IntroCompleted = true;
                ReleaseAudio(false);
                subtitleTween?.Kill(false);
                titleCleanupTween?.Kill(false);
                currentSubtitleIndex = -1;
                subtitleText.text = "";
                if (questTitleText != null && currentQuestData != null)
                {
                    questTitleText.text = currentQuestData.localizedQuestTitle.GetLocalizedString();
                    TrackTween(questTitleText.DOFade(1f, 0.5f));
                }
                PopulateTasks();
                taskContainer.gameObject.SetActive(true);
                foreach (var task in new List<QuestTask>(activeTasks.Values))
                {
                    if (!IsCurrentSequence(sequence)) break;
                    if (!task.isHiddenInitially)
                    {
                        AnimateTaskEntry(taskUIElements[task.actionType]);
                        OnTaskUnlockedLocal?.Invoke(task.actionType);
                    }
                }
                if (!IsCurrentSequence(sequence)) continue;
                IntroSequenceCompleted?.Invoke(this, sequenceId);
                if (IsCurrentSequence(sequence)) OnIntroSequenceCompleted?.Invoke();
            }
            else if (!sequence.IntroCompleted && int.TryParse(command, out int markerIndex))
            {
                var quest = currentQuestData;
                if (quest.localizedIntroSubtitles != null && markerIndex >= 0 && markerIndex < quest.localizedIntroSubtitles.Count)
                {
                    currentSubtitleIndex = markerIndex;

                    subtitleTween?.Kill(false);
                    subtitleTween = subtitleText.DOFade(0, 0.3f).OnComplete(() =>
                    {
                        if (!IsCurrentSequence(sequence)) return;
                        subtitleText.text = quest.localizedIntroSubtitles[markerIndex].GetLocalizedString();
                        subtitleTween = subtitleText.DOFade(1, 0.3f);
                        TrackTween(subtitleTween);
                    });
                    TrackTween(subtitleTween);
                }
            }
        }

        if (!IsCurrentSequence(sequence) || currentQuestData == null || canvasGroup.alpha == 0 || isWaitingForPartner) return;

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
        if (!TryGetQuestContext(out var owner, out var questIndex, out var entryId)) return;
        OnActionPerformed(actionType, owner, questIndex, entryId);
    }

    public void OnActionPerformed(TutorialAction actionType, TutorialManager owner, int questIndex, ulong entryId)
    {
        if (!MatchesQuestContext(owner, questIndex, entryId)) return;
        var sequence = activeSequence;
        if (!IsCurrentSequence(sequence) || !sequence.IntroCompleted) return;
        if (unlockedTasks.ContainsKey(actionType) && !unlockedTasks[actionType]) return;
        if (!activeTasks.ContainsKey(actionType) || currentProgress[actionType] >= activeTasks[actionType].requiredAmount) return;

        currentProgress[actionType]++;
        QuestTask task = activeTasks[actionType];
        TextMeshProUGUI tmp = taskUIElements[actionType];

        UpdateTaskUIText(actionType);

        if (currentProgress[actionType] >= task.requiredAmount)
        {
            TrackTween(tmp.DOColor(Color.green, 0.3f));
            TrackTween(tmp.transform.DOPunchScale(Vector3.one * 0.15f, 0.2f, 10, .5f));

            TrackTween(DOVirtual.DelayedCall(1.5f, () => RevealTasks(sequence, actionType)));

            completedTasksCount++;
            owner.UpdateProgressServerRpc(questIndex, entryId, (int)myRole, completedTasksCount);
            if (IsCurrentSequence(sequence) && MatchesQuestContext(owner, questIndex, entryId))
                CheckAllTasksCompleted(owner, questIndex, entryId);
        }
    }

    private void RevealTasks(QuestSequence sequence, TutorialAction actionType)
    {
        if (!IsCurrentSequence(sequence)) return;
        foreach (var checkTask in new List<QuestTask>(activeTasks.Values))
        {
            if (!IsCurrentSequence(sequence)) return;
            if (checkTask.isHiddenInitially && checkTask.revealsAfter == actionType && !unlockedTasks[checkTask.actionType])
            {
                unlockedTasks[checkTask.actionType] = true;
                var revealedTmp = taskUIElements[checkTask.actionType];
                revealedTmp.gameObject.SetActive(true);
                AnimateTaskEntry(revealedTmp);
                OnTaskUnlockedLocal?.Invoke(checkTask.actionType);
            }
        }
    }

    private void CheckAllTasksCompleted(TutorialManager owner, int questIndex, ulong entryId)
    {
        if (completedTasksCount < activeTasks.Count) return;

        isWaitingForPartner = true;

        if (waitingStatusText != null)
        {
            waitingStatusText.gameObject.SetActive(true);
            UpdateWaitingTextUI(partnerCompletedTasks);
        }

        owner.PlayerReadyServerRpc(questIndex, entryId, (int)myRole);
    }

    private void HandlePartnerProgress(TutorialManager owner, int questIndex, ulong entryId, PlayerRole role, int progress)
    {
        if (!MatchesQuestContext(owner, questIndex, entryId) || role == myRole ||
            (role != PlayerRole.Engineer && role != PlayerRole.Technician)) return;
        partnerCompletedTasks = progress;
        if (isWaitingForPartner)
        {
            UpdateWaitingTextUI(progress);
        }
    }

    private void UpdateWaitingTextUI(int partnerCompletedTasks)
    {
        if (waitingStatusText == null || currentQuestData == null) return;

        string baseText = (myRole == PlayerRole.Engineer)
            ? currentQuestData.localizedWaitingForTechText.GetLocalizedString()
            : currentQuestData.localizedWaitingForEngText.GetLocalizedString();

        int partnerTotalTasks = (myRole == PlayerRole.Engineer)
            ? currentQuestData.technicianTasks.Count
            : currentQuestData.engineerTasks.Count;

        waitingStatusText.text = $"{baseText} ({partnerCompletedTasks}/{partnerTotalTasks})";
    }

    public bool IsTaskActive(TutorialAction actionType)
    {
        return activeSequence != null && activeTasks.ContainsKey(actionType) && unlockedTasks.ContainsKey(actionType) && unlockedTasks[actionType];
    }

    public bool IsTaskCompleted(TutorialAction actionType)
    {
        return activeSequence != null && activeTasks.ContainsKey(actionType) && currentProgress[actionType] >= activeTasks[actionType].requiredAmount;
    }

    public bool ShouldBlockAction(TutorialAction actionType)
    {
        if (activeSequence == null) return false;
        if (activeTasks.ContainsKey(actionType) && unlockedTasks.ContainsKey(actionType))
        {
            return !unlockedTasks[actionType];
        }

        return false;
    }

    [AOT.MonoPInvokeCallback(typeof(FMOD.Studio.EVENT_CALLBACK))]
    private static FMOD.RESULT MegaphoneCallback(FMOD.Studio.EVENT_CALLBACK_TYPE type, IntPtr instancePtr, IntPtr parameterPtr)
    {
        var instance = new FMOD.Studio.EventInstance(instancePtr);
        var result = instance.getUserData(out var userData);
        if (result != FMOD.RESULT.OK || userData == IntPtr.Zero) return result;
        var handle = GCHandle.FromIntPtr(userData);
        if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.DESTROYED)
        {
            instance.setUserData(IntPtr.Zero);
            handle.Free();
        }
        else if (handle.Target is QuestSequence sequence)
        {
            if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.TIMELINE_MARKER)
            {
                var parameter = (FMOD.Studio.TIMELINE_MARKER_PROPERTIES)Marshal.PtrToStructure(parameterPtr, typeof(FMOD.Studio.TIMELINE_MARKER_PROPERTIES));
                sequence.Commands.Enqueue(parameter.name);
            }
            else if (type == FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED)
            {
                sequence.Commands.Enqueue("AUDIO_STOPPED");
            }
        }
        return FMOD.RESULT.OK;
    }

    private void AnimateTaskEntry(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;

        Color c = tmp.color; c.a = 0; tmp.color = c;
        TrackTween(tmp.DOFade(1f, 0.6f).From(0).SetEase(Ease.OutQuad));

        Vector4 originalMargin = tmp.margin;
        tmp.margin = new Vector4(originalMargin.x + 50f, originalMargin.y, originalMargin.z, originalMargin.w);
        TrackTween(DOTween.To(() => tmp.margin, x => tmp.margin = x, originalMargin, 0.6f).SetEase(Ease.OutQuad));
    }
}
