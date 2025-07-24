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
                    obj.GetComponent<PrimaryWeaponItem>().InitializeItem(player.Object, player.SwordBack, player.SwordHand);
                });

                break;
            case "002":
                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<PrimaryWeaponItem>().InitializeItem(player.Object, player.SpearBack, player.SpearHand);
                });

                break;
            case "007":

                tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());

                Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                {
                    obj.GetComponent<ArmorItem>().InitializeItem(player.Object, player.ArmorHand, true);
                });
                break;
            case "008":

                if (player.HealCount >= 4) return;

                player.HealCount++;

                break;
            case "009":

                if (player.ArmorRepairCount >= 4) return;

                player.ArmorRepairCount++;

                break;
            case "010":

                if (player.TrapCount >= 4) return;

                player.TrapCount++;

                break;
        }

        Weapons.Remove(itemkey);
    }
}
