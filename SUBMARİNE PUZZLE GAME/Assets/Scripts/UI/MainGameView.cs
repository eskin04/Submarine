using PurrLobby;
using TMPro;
using UnityEngine;
using PurrNet;

public class MainGameView : View
{
    [SerializeField] private TMP_Text roleText;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<MainGameView>();
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
            roleText.text = role.ToString();
        }
    }

}