using Fusion;
using System;
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

    public void PickupItemForBot(string itemkey, Botdata bot)
    {
        if (!HasStateAuthority) return;
        if (!Weapons.ContainsKey(itemkey)) return;

        var inv = bot.Inventory;
        NetworkObject tempweapon;

        switch (itemkey)
        {
            case "001":
            case "002":
                tempweapon = weaponSpawnData.GetItemObject(itemkey);
                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (runner, obj) =>
                {
                    obj.GetComponent<PrimaryWeaponItem>().InitializeItem(bot.Object, true, () => Weapons.Remove(itemkey));
                });
                break;
            case "003":
            case "004":
                tempweapon = weaponSpawnData.GetItemObject(itemkey);
                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (runner, obj) =>
                {
                    obj.GetComponent<SecondaryWeaponItem>().InitializeItem(bot.Object, Weapons[itemkey], true, () => Weapons.Remove(itemkey));
                    inv.SwitchToSecondary();
                });
                break;
            case "005":
                inv.RifleMagazine = System.Math.Min(inv.RifleMagazine + Weapons[itemkey], 999);
                Weapons.Remove(itemkey);
                break;
            case "006":
                inv.BowMagazine = System.Math.Min(inv.BowMagazine + Weapons[itemkey], 999);
                Weapons.Remove(itemkey);
                break;
            case "007":
                tempweapon = weaponSpawnData.GetItemObject(itemkey);
                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, PlayerRef.None, onBeforeSpawned: (runner, obj) =>
                {
                    obj.GetComponent<ArmorItem>().InitializeItem(bot.Object, inv.ArmorBack, false, true, () => Weapons.Remove(itemkey));
                });
                break;
            case "008":
                if (inv.HealCount >= 4) return;
                inv.HealCount++;
                Weapons.Remove(itemkey);
                break;
            case "009":
                if (inv.RepairCount >= 4) return;
                inv.RepairCount++;
                Weapons.Remove(itemkey);
                break;
            case "010":
                if (inv.TrapCount >= 4) return;
                inv.TrapCount++;
                Weapons.Remove(itemkey);
                break;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RemoveItem(NetworkString<_4> itemkey, PlayerInventoryV2 player)
    {
        if (!HasStateAuthority) return;

        if (!Weapons.ContainsKey(itemkey)) return;

        NetworkObject tempweapon = null;

        switch (itemkey.ToString())
        {
            case "001":
                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<PrimaryWeaponItem>().InitializeItem(player.Object, false, () => Weapons.Remove(itemkey));
                });

                break;
            case "002":
                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<PrimaryWeaponItem>().InitializeItem(player.Object, false, () => Weapons.Remove(itemkey));
                });

                break;
            case "003":
                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<SecondaryWeaponItem>().InitializeItem(player.Object, Weapons[itemkey], false, () => Weapons.Remove(itemkey));
                });

                break;
            case "004":
                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<SecondaryWeaponItem>().InitializeItem(player.Object, Weapons[itemkey], false, () => Weapons.Remove(itemkey));
                });

                break;
            case "005":

                player.RifleMagazine = Math.Min(player.RifleMagazine + Weapons[itemkey], 999);

                Weapons.Remove(itemkey);

                break;
            case "006":

                player.BowMagazine = Math.Min(player.BowMagazine + Weapons[itemkey], 999);

                Weapons.Remove(itemkey);

                break;
            case "007":

                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<ArmorItem>().InitializeItem(player.Object, player.ArmorHand, true, false, () => Weapons.Remove(itemkey));
                });
                break;
            case "008":

                if (player.HealCount >= 4) return;

                player.HealCount++;

                Weapons.Remove(itemkey);

                break;
            case "009":

                if (player.ArmorRepairCount >= 4) return;

                player.ArmorRepairCount++;

                Weapons.Remove(itemkey);

                break;
            case "010":

                if (player.TrapCount >= 4) return;

                player.TrapCount++;

                Weapons.Remove(itemkey);

                break;
        }
    }
}
