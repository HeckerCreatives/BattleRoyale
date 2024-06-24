using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponItem : WeaponData
{
    public override void Spawned()
    {
        WeaponObject.transform.parent = Parent.transform;
        WeaponObject.transform.localPosition = Vector3.zero;
        WeaponObject.transform.localRotation = Quaternion.identity;
    }

    public void InitializeItem(string name, string id, int animationID, NetworkObject networkObject, NetworkObject parent, NetworkObject hand, int ammo)
    {
        AnimatorID = animationID;
        WeaponID = id;
        WeaponName = name;
        Owner = networkObject;
        WeaponObject = Object;
        Parent = parent;
        Hand = hand;
        Ammo = ammo;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_ActivateWeapon()
    {
        WeaponObject.transform.parent = Hand.transform;
        WeaponObject.transform.localPosition = Vector3.zero;
        WeaponObject.transform.localRotation = Quaternion.identity;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void Rpc_SheatWeapon()
    {
        WeaponObject.transform.parent = Parent.transform;
        WeaponObject.transform.localPosition = Vector3.zero;
        WeaponObject.transform.localRotation = Quaternion.identity;
    }
}
