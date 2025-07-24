using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotMovementController : NetworkBehaviour
{
    [SerializeField] private SimpleKCC botKCC;

    public override void Spawned()
    {
        botKCC.SetGravity(Physics.gravity.y * 3f);
    }

    public override void FixedUpdateNetwork()
    {
        botKCC.Move(Vector3.zero, 0f);
    }
}
