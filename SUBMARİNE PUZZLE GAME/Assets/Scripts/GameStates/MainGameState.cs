using System.Collections.Generic;
using PurrNet;
using PurrNet.StateMachine;
using StarterAssets;
using UnityEngine;

public class MainGameState : StateNode<List<PlayerInventory>>
{

    public override void Enter(List<PlayerInventory> data, bool asServer)
    {
        base.Enter(asServer);
        if (!asServer) return;
    }



    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }
}
