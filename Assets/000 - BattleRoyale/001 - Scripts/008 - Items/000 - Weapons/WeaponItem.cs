using Fusion;
using Fusion.Addons.SimpleKCC;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;

public class WeaponItem : WeaponData
{
    public event EventHandler<WeaponItemListChangeEventArgs> WeaponPickupChange;
    public event EventHandler<WeaponItemListChangeEventArgs> OnWeaponPickupChange
    {
        add
        {
            if (WeaponPickupChange == null || !WeaponPickupChange.GetInvocationList().Contains(value))
                WeaponPickupChange += value;
        }
        remove { WeaponPickupChange -= value; }
    }

    //  ===========================

    [SerializeField] private Vector3 dropRotation;

    //  ===========================

    public void InitializeItem(string name, string id, int animationID, NetworkObject networkObject, NetworkObject parent, NetworkObject hand, int ammo, bool isHand = false)
    {
        AnimatorID = animationID;
        WeaponID = id;
        WeaponName = name;
        Owner = networkObject;
        WeaponObject = Object;
        Parent = parent;
        Hand = hand;
        Ammo = ammo;
        IsPickedUp = true;
        IsHand = isHand;
    }

    public override void Render()
    {
        SetWeaponParent();
    }

    private void SetWeaponParent()
    {
        if (IsPickedUp)
        {
            if (Parent != null && Hand != null && Owner != null)
            {
                if (!IsHand)
                    WeaponObject.transform.parent = Parent.transform;
                else
                    WeaponObject.transform.parent = Hand.transform;

                WeaponObject.transform.localPosition = Vector3.zero;
                WeaponObject.transform.localRotation = Quaternion.identity;
            }
        }
        else
        {

        }
    }

    [Rpc]
    public void Rpc_SendPickupEvent(int index, NetworkId objectId)
    {
        WeaponPickupChange?.Invoke(this, new WeaponItemListChangeEventArgs(index, objectId));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_ReassignParentPlayer(NetworkObject owner, NetworkId parent, NetworkId hand, bool isHand)
    {

        var tempParent = Runner.FindObject(parent);
        var tempHand = Runner.FindObject(hand);

        IsHand = isHand;
        Owner = owner;
        Parent = tempParent ? tempParent : null;
        Hand = tempHand ? tempHand : null;

        IsPickedUp = true;
    }

    [Rpc]
    public void Rpc_DropWeaponClients(Vector3 dropPosition)
    {
        IsPickedUp = false;

        Owner = null;
        Parent = null;
        Hand = null;

        WeaponObject.transform.parent = null;
        WeaponObject.transform.rotation = Quaternion.Euler(dropRotation);
        WeaponObject.transform.position = dropPosition;
    }

    public void LocalActivateWeapon()
    {
        //WeaponObject.transform.parent = Hand.transform;
        //WeaponObject.transform.localPosition = Vector3.zero;
        //WeaponObject.transform.localRotation = Quaternion.identity;
    }
}

[System.Serializable]
public class WeaponItemListChangeEventArgs : EventArgs
{
    public int Index { get; }
    public NetworkId Objectid { get; }

    public WeaponItemListChangeEventArgs(int index, NetworkId objectid)
    {
        Index = index;
        Objectid = objectid;
    }
}