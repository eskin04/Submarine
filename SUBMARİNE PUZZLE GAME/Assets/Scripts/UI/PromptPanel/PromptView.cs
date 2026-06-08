using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using System.Linq;

public enum PromptGroup
{
    Item,
    Module
}

public struct PromptData
{
    public string id;
    public string key;
    public string action;
    public Sprite icon;
    public PromptGroup group;
}

public class PromptView : View
{
    [Header("Prompt Slot Ayarları")]
    [SerializeField] private List<PromptSlot> slots = new List<PromptSlot>();

    private Dictionary<string, PromptData> activePrompts = new Dictionary<string, PromptData>();
    private bool isDirty = false;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<PromptView>();
    }

    public override void OnShow() { }

    public override void OnHide()
    {
        ClearAllPrompts();
    }

    public void AddPrompt(string id, string key, string action, PromptGroup group = PromptGroup.Item, Sprite icon = null)
    {
        InstanceHandler.TryGetInstance<GameViewManager>(out var gameViewManager);
        if (gameViewManager != null && !gameViewManager.IsViewActive<PromptView>())
        {
            gameViewManager.ShowView<PromptView>(false);
        }
        activePrompts[id] = new PromptData { id = id, key = key, action = action, group = group, icon = icon };
        isDirty = true;
    }

    public void RemovePrompt(string id)
    {
        if (activePrompts.ContainsKey(id))
        {
            activePrompts.Remove(id);
            isDirty = true;
        }
    }

    public void RemovePromptsByGroup(PromptGroup targetGroup)
    {
        var keysToRemove = activePrompts.Where(kvp => kvp.Value.group == targetGroup).Select(kvp => kvp.Key).ToList();

        foreach (var key in keysToRemove)
        {
            activePrompts.Remove(key);
        }

        if (keysToRemove.Count > 0) isDirty = true;
    }

    public void ClearAllPrompts()
    {
        if (activePrompts.Count > 0)
        {
            activePrompts.Clear();
            isDirty = true;
        }
    }

    private void LateUpdate()
    {
        if (!isDirty) return;
        isDirty = false;
        SyncUIWithData();
    }

    private void SyncUIWithData()
    {
        bool hasModulePrompts = activePrompts.Values.Any(p => p.group == PromptGroup.Module);

        PromptGroup targetGroup = hasModulePrompts ? PromptGroup.Module : PromptGroup.Item;

        foreach (var slot in slots)
        {
            if (slot.gameObject.activeSelf && !string.IsNullOrEmpty(slot.CurrentId))
            {
                if (!activePrompts.TryGetValue(slot.CurrentId, out PromptData data) || data.group != targetGroup)
                {
                    slot.Hide();
                }
            }
        }

        foreach (var kvp in activePrompts)
        {
            if (kvp.Value.group != targetGroup) continue;

            string targetId = kvp.Key;
            PromptData targetData = kvp.Value;

            bool isAlreadyOnScreen = false;
            foreach (var slot in slots)
            {
                if (slot.gameObject.activeSelf && slot.CurrentId == targetId)
                {
                    slot.UpdateContent(targetData.key, targetData.action, targetData.icon);
                    isAlreadyOnScreen = true;
                    break;
                }
            }

            if (!isAlreadyOnScreen)
            {
                PromptSlot availableSlot = slots.FirstOrDefault(s => !s.gameObject.activeSelf && string.IsNullOrEmpty(s.CurrentId));
                if (availableSlot != null)
                {
                    availableSlot.Show(targetData.id, targetData.key, targetData.action, targetData.icon);
                }
            }
        }
    }
}