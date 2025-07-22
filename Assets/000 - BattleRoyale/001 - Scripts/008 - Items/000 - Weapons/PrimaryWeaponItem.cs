using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrimaryWeaponItem : NetworkBehaviour, IPickupItem
{
    public string WeaponID
    {
        get => weaponID;
    }

    public string WeaponName
    {
        get => weaponName;
    }

    //  ======================

    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;

    [Space]
    [SerializeField] private Vector3 backRotation;
    [SerializeField] private Vector3 handRotation;
    [SerializeField] private Vector3 dropRotation;

    [field: Header("NETWORK")]
    [Networked][field: SerializeField] public int Supplies { get; set; }
    [Networked][field: SerializeField] public bool IsPickedUp { get; set; }
    [Networked][field: SerializeField] public bool IsEquipped { get; set; }
    [Networked][field: SerializeField] public NetworkObject CurrentPlayer { get; set; }
    [Networked][field: SerializeField] public Vector3 Position { get; set; }
    [Networked][field: SerializeField] public NetworkObject Back { get; set; }
    [Networked][field: SerializeField] public NetworkObject Hand { get; set; }


    public override void Render()
    {
        if (IsPickedUp)
        {
            if (IsEquipped)
            {
                transform.parent = Hand.transform;
                transform.localRotation = Quaternion.Euler(handRotation);
            }
            else
            {
                transform.parent = Back.transform;
                transform.localRotation = Quaternion.Euler(backRotation);
            }

            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsPickedUp)
        {
            if (IsEquipped)
            {
                transform.parent = Hand.transform;
                transform.localRotation = Quaternion.Euler(handRotation);
            }
            else
            {
                transform.parent = Back.transform;
                transform.localRotation = Quaternion.Euler(backRotation);
            }

            transform.localPosition = Vector3.zero;
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
        }
    }

    public void InitializeItem(NetworkObject player, NetworkObject backParent, NetworkObject handParent)
    {
        PlayerInventoryV2 tempinventory = player.GetComponent<PlayerInventoryV2>();

        if (tempinventory.PrimaryWeapon != null) tempinventory.PrimaryWeapon.DropWeapon();

        Object.AssignInputAuthority(player.InputAuthority);

        CurrentPlayer = player;
        tempinventory.PrimaryWeapon = this;

        Position = backParent.transform.position;
        Back = backParent;
        Hand = handParent;

        IsPickedUp = true;

        Supplies = 1;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupPrimaryWeapon(NetworkObject player, NetworkObject backParent, NetworkObject handParent)
    {
        InitializeItem(player, backParent, handParent);
    }


    public void DropWeapon()
    {
        IsPickedUp = false;
        transform.parent = null;

        Position = CurrentPlayer.transform.position + new Vector3(0f, 0.1f, 0f);
        CurrentPlayer.GetComponent<PlayerInventoryV2>().Armor = null;
        CurrentPlayer = null;

        IsPickedUp = false;
        Object.RemoveInputAuthority();
    }
}
