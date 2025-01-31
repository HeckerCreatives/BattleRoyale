using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrateController : NetworkBehaviour
{
    [SerializeField] private WeaponSpawnData weaponSpawnData;

    //  ====================

    [Networked, Capacity(10)] public NetworkDictionary<NetworkString<_4>, int> Weapons => default;

    //  ====================

    public void SetDatas(Dictionary<string, int> data)
    {
        if (!HasStateAuthority) return;

        foreach (var item in data)
        {
            Weapons.Set(item.Key, item.Value);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RemoveItem(NetworkString<_4> itemkey, string itemname, NetworkObject player, NetworkObject back, NetworkObject hand)
    {
        if (!HasStateAuthority) return;

        if (!Weapons.ContainsKey(itemkey)) return;

        int tempAmmo = Weapons[itemkey]; 

        NetworkObject tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();

        if (tempweapon != null)
        {
            if ((itemkey.ToString() == "001" || itemkey.ToString() == "002") && playerInventory.PrimaryWeapon != null)
                playerInventory.PrimaryWeapon.DropWeapon();
            else if (itemkey.ToString() == "007")
                playerInventory.Shield.DropShield();

            NetworkObject weapon = Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<WeaponItem>().InitializeItem(
                    itemname,
                    itemkey.ToString(),
                    player,
                    back,
                    hand,
                    0,
                    true
                );
            });

            if (itemkey.ToString() == "001" || itemkey.ToString() == "002")
            {
                playerInventory.PrimaryWeapon = weapon.GetComponent<WeaponItem>();
                playerInventory.WeaponIndex = 2;
            }
            else if (itemkey.ToString() == "007")
            {
                playerInventory.Shield = weapon.GetComponent<WeaponItem>();
            }

            Weapons.Remove(itemkey);
        }
        else
        {
            if (itemkey.ToString() == "008")
            {
                if (playerInventory.HealCount >= 4) return;

                playerInventory.HealCount++;
            }
            else if (itemkey.ToString() == "009")
            {
                if (playerInventory.ArmorRepairCount >= 4) return;

                playerInventory.ArmorRepairCount++;
            }
        }

        Weapons.Remove(itemkey);
    }
}
