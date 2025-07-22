using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmorItem : NetworkBehaviour, IPickupItem
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
    [SerializeField] private Vector3 pickedUpRotation;
    [SerializeField] private Vector3 dropRotation;
    [SerializeField] private Vector3 dropSize;
    [SerializeField] private Vector3 pickUpSize;

    [field: Header("NETWORK")]
    [Networked][field: SerializeField] public int Supplies { get; set; }
    [Networked][field: SerializeField] public bool IsPickedUp { get; set; }
    [Networked][field: SerializeField] public NetworkObject CurrentPlayer { get; set; }
    [Networked][field: SerializeField] public Vector3 Position { get; set; }
    [Networked][field: SerializeField] public NetworkObject Parent { get; set; }


    public override void Render()
    {
        if (IsPickedUp)
        {
            transform.parent = Parent.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(pickedUpRotation);
            transform.localScale = pickUpSize;
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
            transform.localScale = dropSize;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsPickedUp)
        {
            transform.parent = Parent.transform;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Euler(pickedUpRotation);
            transform.localScale = pickUpSize;
        }
        else
        {
            transform.parent = null;
            transform.position = Position;
            transform.rotation = Quaternion.Euler(dropRotation);
            transform.localScale = dropSize;
        }
    }

    public void InitializeItem(NetworkObject player, NetworkObject armorParent, bool isSpawn = false)
    {
        PlayerInventoryV2 tempinventory = player.GetComponent<PlayerInventoryV2>();

        if (tempinventory.Armor != null) tempinventory.Armor.DropArmor();

        Object.AssignInputAuthority(player.InputAuthority);

        CurrentPlayer = player;
        tempinventory.Armor = this;

        Position = armorParent.transform.position;
        Parent = armorParent;

        IsPickedUp = true;

        Supplies = isSpawn ? 100 : Supplies;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupArmor(NetworkObject player, NetworkObject armorParent)
    {
        InitializeItem(player, armorParent);
    }


    public void DropArmor()
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
