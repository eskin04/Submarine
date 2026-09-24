using PurrNet;
using UnityEngine;
using PurrLobby;


public class PlayerStats : NetworkBehaviour
{
    public static PlayerStats LocalInstance { get; private set; }
    [SerializeField] private PlayerRole role;
    public PlayerRole Role => role;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!isOwner) return;
        LocalInstance = this;
        Debug.Log($"<color=green>[PlayerStats]</color> Local player spawned with role: {Role}");
        if (InstanceHandler.TryGetInstance<TutorialQuestView>(out var tutorialView))
            tutorialView.SetPlayerRole(role);
        if (InstanceHandler.TryGetInstance<PlayerSpawnView>(out var spawnView))
            spawnView.SetRoleText(role);

        if (InstanceHandler.TryGetInstance<MainGameView>(out var mainView))
            mainView.SetRoleText(role);

    }
}
