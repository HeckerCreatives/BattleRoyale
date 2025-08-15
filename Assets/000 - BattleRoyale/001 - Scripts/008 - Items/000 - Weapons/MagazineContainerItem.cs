using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class MagazineContainerItem : NetworkBehaviour, IPickupItem
{
    public string WeaponName
    {
        get => weaponName;
    }

    public string WeaponID => weaponID;

    public int Supplies => 0;

    //  ====================

    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;

    [Space]
    [SerializeField] private Vector3 backRotation;

    [field: Header("NETWORK")]
    [Networked][field: SerializeField] public bool IsPickedUp { get; set; }
    [Networked][field: SerializeField] public NetworkObject CurrentPlayer { get; set; }
    [Networked][field: SerializeField] public PlayerOwnObjectEnabler PlayerCore { get; set; }
    [Networked][field: SerializeField] public Vector3 Position { get; set; }
    [Networked][field: SerializeField] public NetworkObject Back { get; set; }


    public override void Render()
    {
        if (IsPickedUp)
        {
            transform.parent = PlayerCore.Inventory.SecondaryWeaponID() == "004" ? PlayerCore.Inventory.BowAmmoBack : null;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(backRotation);
        }
    }

    public void InitializeItem(NetworkObject player, Action finalAction = null)
    {
        PlayerInventoryV2 tempPlayerinventory = player.GetComponent<PlayerInventoryV2>();

        Object.AssignInputAuthority(player.InputAuthority);

        CurrentPlayer = player;

        tempPlayerinventory.MagazineContainer = this;

        IsPickedUp = true;

        PlayerOwnObjectEnabler tempcore = player.GetComponent<PlayerOwnObjectEnabler>();
        PlayerCore = tempcore;

        finalAction?.Invoke();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupMagazineContainer(NetworkObject player)
    {
        InitializeItem(player);
    }

    public void DropWeapon()
    {
        Runner.Despawn(Object);
    }
}
