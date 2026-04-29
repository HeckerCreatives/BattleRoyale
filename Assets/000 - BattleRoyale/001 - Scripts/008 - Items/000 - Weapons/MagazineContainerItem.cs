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

    //  ====================

    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;

    [Space]
    [SerializeField] private Vector3 backRotation;

    [field: Header("NETWORK")]
    [Networked][field: SerializeField] public int Supplies { get; set; }
    [Networked][field: SerializeField] public bool IsPickedUp { get; set; }
    [Networked][field: SerializeField] public NetworkObject CurrentPlayer { get; set; }
    [Networked][field: SerializeField] public PlayerOwnObjectEnabler PlayerCore { get; set; }
    [Networked][field: SerializeField] public Vector3 Position { get; set; }
    [Networked][field: SerializeField] public NetworkObject Back { get; set; }
    [Networked][field: SerializeField] public Quaternion Rotation { get; set; }


    public override void Render()
    {
        if (IsPickedUp)
        {
            transform.parent = PlayerCore.Inventory.BowAmmoBack;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(backRotation);
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Rotation;
        }
    }

    public void InitializeOnStart(Vector3 position, Quaternion rotation, int supplies)
    {
        Supplies = supplies;
        Position = position;
        Rotation = rotation;

        Object.RemoveInputAuthority();
    }

    public void InitializeItem(NetworkObject player, Action finalAction = null)
    {
        PlayerInventoryV2 tempPlayerinventory = player.GetComponent<PlayerInventoryV2>();

        Object.AssignInputAuthority(player.InputAuthority);

        CurrentPlayer = player;

        tempPlayerinventory.MagazineContainer = this;

        tempPlayerinventory.BowMagazine += Supplies;

        Supplies = 0;

        IsPickedUp = true;

        PlayerOwnObjectEnabler tempcore = player.GetComponent<PlayerOwnObjectEnabler>();
        PlayerCore = tempcore;

        finalAction?.Invoke();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupMagazineContainer(NetworkObject player)
    {
        PlayerInventoryV2 tempPlayerinventory = player.GetComponent<PlayerInventoryV2>();

        if (tempPlayerinventory.MagazineContainer == null)
            InitializeItem(player);
        else
        {
            tempPlayerinventory.BowMagazine += Supplies;
            Runner.Despawn(Object);
        }
    }

    public void DropWeapon()
    {
        IsPickedUp = false;
        transform.parent = null;

        Position = CurrentPlayer.transform.position + new Vector3(0f, 0.1f, 0f);
        Rotation = Quaternion.identity;

        CurrentPlayer.GetComponent<PlayerInventoryV2>().SecondaryWeapon = null;

        CurrentPlayer = null;
        PlayerCore = null;
        Object.RemoveInputAuthority();
    }
}
