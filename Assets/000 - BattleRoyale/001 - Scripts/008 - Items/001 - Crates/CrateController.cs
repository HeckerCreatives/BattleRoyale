using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrateController : NetworkBehaviour
{
    [SerializeField] private NetworkObject swordObj;
    [SerializeField] private NetworkObject spearObj;
    [SerializeField] private NetworkObject shieldObj;

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

        Weapons.Remove(itemkey);

        NetworkObject tempweapon = null;

        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();

        switch (itemkey.ToString())
        {
            case "001":

                if (playerInventory.PrimaryWeapon != null)
                    playerInventory.PrimaryWeapon.DropWeapon();

                tempweapon = Runner.Spawn(swordObj, Vector3.zero, Quaternion.identity, player.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
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

                playerInventory.PrimaryWeapon = tempweapon.GetComponent<WeaponItem>();
                playerInventory.WeaponIndex = 2;
                break;
            case "002":

                if (playerInventory.PrimaryWeapon != null)
                    playerInventory.PrimaryWeapon.DropWeapon();

                tempweapon = Runner.Spawn(spearObj, Vector3.zero, Quaternion.identity, player.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
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

                playerInventory.PrimaryWeapon = tempweapon.GetComponent<WeaponItem>();
                playerInventory.WeaponIndex = 2;
                break;
            case "007":

                if (playerInventory.Shield != null)
                    playerInventory.Shield.DropWeapon();

                tempweapon = Runner.Spawn(shieldObj, Vector3.zero, Quaternion.identity, player.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<WeaponItem>().InitializeItem(
                        itemname,
                        itemkey.ToString(),
                        player,
                        back,
                        hand,
                        tempAmmo,
                        true
                    );
                });

                playerInventory.Shield = tempweapon.GetComponent<WeaponItem>();

                break;
            case "008":

                if (playerInventory.HealCount >= 4)
                    break;

                playerInventory.HealCount++;

                break;
            case "009":

                if (playerInventory.ArmorRepairCount >= 4) break;

                playerInventory.ArmorRepairCount++;

                break;
        }
    }
}
