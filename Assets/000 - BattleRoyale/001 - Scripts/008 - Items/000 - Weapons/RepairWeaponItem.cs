using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairWeaponItem : NetworkBehaviour, IPickupItem
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
        PickupPositionClients();
    }

    private void PickupPositionClients()
    {
        if (IsPickedUp) return;

        // If dropped, keep the position/rotation as-is (or update if needed)
        transform.position = Position;
        transform.rotation = Rotation != Quaternion.identity ? Rotation : Quaternion.Euler(dropRotation);
    }

    public void InitializeItem(Vector3 position, Quaternion rotation, int supplies)
    {
        IsPickedUp = false;

        Supplies = supplies;
        Position = position;
        Rotation = rotation;

        Object.RemoveInputAuthority();
    }


    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_PickupRepair(NetworkObject player)
    {
        PlayerInventoryV2 tempPlayerinventory = player.gameObject.GetComponent<PlayerInventoryV2>();

        if (tempPlayerinventory == null) return;

        if (tempPlayerinventory.ArmorRepairCount >= 4) return;

        tempPlayerinventory.ArmorRepairCount += Supplies;

        Runner.Despawn(Object);
    }
}
