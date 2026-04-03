using PurrLobby;
using TMPro;
using UnityEngine;
using PurrNet;

public class PlayerSpawnView : View
{
    [SerializeField] private TMP_Text roleText;
    [SerializeField] private TMP_Text levelText;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<PlayerSpawnView>();
    }
    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }

    public void SetRoleText(PlayerRole role)
    {
        if (roleText != null)
        {
            roleText.text = "You Are " + role.ToString();
        }
    }

    public void SetLevelText(int level)
    {
        if (levelText != null)
        {
            Debug.Log($"Setting Level Text: Level {level}");
            levelText.text = "Level " + level.ToString();
        }
    }

}