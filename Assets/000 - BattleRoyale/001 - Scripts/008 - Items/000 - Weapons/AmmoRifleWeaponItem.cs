using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmmoRifleWeaponItem : NetworkBehaviour, IPickupItem
{
    public string WeaponID => weaponID;

    public string WeaponName => weaponName;

    //  ===================

    [Space]
    [SerializeField] private string weaponID;
    [SerializeField] private string weaponName;
    [SerializeField] private Vector3 dropRotation;

    [field: Header("NETWORK")]
    [Networked][field: SerializeField] public int Supplies { get; set; }
    [Networked][field: SerializeField] public bool IsPickedUp { get; set; }
    [Networked][field: SerializeField] public Vector3 Position { get; set; }
    [Networked][field: SerializeField] public Quaternion Rotation { get; set; }

    public override void Render()
    {
        transform.position = Position;
        transform.rotation = Rotation;
    }

    public void InitializeOnStart(Vector3 position, Quaternion rotation, int supplies)
    {
        Supplies = supplies;
        Position = position;
        Rotation = rotation;

        Object.RemoveInputAuthority();
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupAmmoRifle(NetworkObject player)
    {
        // Layer-1 race guard: a second RPC for the same item must no-op.
        if (IsPickedUp) return;

        PlayerInventoryV2 tempPlayerinventory = player.gameObject.GetComponent<PlayerInventoryV2>();

        if (tempPlayerinventory == null) return;

        IsPickedUp = true;
        tempPlayerinventory.RifleMagazine += Supplies;

        Runner.Despawn(Object);
    }
}
