using PurrNet;
using UnityEngine;
using PurrLobby;


public class PlayerStats : NetworkBehaviour
{
    [SerializeField] private PlayerRole role;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!isOwner) return;
        InstanceHandler.GetInstance<MainGameView>()?.SetRoleText(role);

    }
}
