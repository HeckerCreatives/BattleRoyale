using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion.Sockets;

public class AOIManager : NetworkBehaviour
{
    //[SerializeField] private float visibilityRadius = 30f;

    //public bool IsVisibleTo(NetworkRunner runner, PlayerRef player)
    //{
    //    // Get the player's NetworkObject
    //    NetworkObject playerObject = runner.GetPlayerObject(player);
    //    if (playerObject == null)
    //        return false;

    //    // Don't hide from yourself
    //    if (playerObject == Object)
    //        return true;

    //    float distance = Vector3.Distance(transform.position, playerObject.transform.position);
    //    return distance <= visibilityRadius;
    //}

    //public override void Spawned()
    //{
    //    if (HasStateAuthority)
    //    {
    //        Runner.AddVisibilityCallback(this);
    //    }
    //}
}
