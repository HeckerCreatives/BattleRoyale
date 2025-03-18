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
    public void RPC_RemoveItem(NetworkString<_4> itemkey, string itemname, NetworkObject player, NetworkObject back, NetworkObject hand, NetworkObject ammoContainer = null)
    {
        if (!HasStateAuthority) return;

        if (!Weapons.ContainsKey(itemkey)) return;

        PlayerInventory playerInventory = player.GetComponent<PlayerInventory>();

        Debug.Log($"Pickup item: {itemkey}");

        int tempAmmo = Weapons[itemkey]; 

        NetworkObject tempweapon = weaponSpawnData.GetItemObject(itemkey.ToString());


        if (tempweapon != null)
        {
            bool isHand = false;

            if (itemkey.ToString() == "001" || itemkey.ToString() == "002")
            {
                if (playerInventory.PrimaryWeapon != null)
                    playerInventory.PrimaryWeapon.DropPrimaryWeapon();

                isHand = playerInventory.WeaponIndex == 2 || playerInventory.WeaponIndex == 1;

                if (isHand)
                    playerInventory.WeaponIndex = 2;
            }
            else if (itemkey.ToString() == "003")
            {
                if (playerInventory.ArrowHolder != null)
                {
                    if (playerInventory.SecondaryWeapon != null) playerInventory.SecondaryWeapon.DropSecondaryWithAmmoCaseWeapon();
                }
                else
                {
                    if (playerInventory.SecondaryWeapon != null) playerInventory.SecondaryWeapon.DropSecondaryWeapon();
                }

                isHand = playerInventory.WeaponIndex == 3 || playerInventory.WeaponIndex == 1;

                if (isHand)
                    playerInventory.WeaponIndex = 3;
            }
            else if (itemkey.ToString() == "004")
            {
                if (playerInventory.SecondaryWeapon != null) playerInventory.SecondaryWeapon.DropSecondaryWithAmmoCaseWeapon();

                if (ammoContainer != null)
                    Runner.Spawn(weaponSpawnData.GetItemObject("arrowcontainer"), ammoContainer.transform.position, Quaternion.identity, player.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
                    {
                        playerInventory.ArrowHolder = obj;
                        obj.GetComponent<AmmoContainerController>().Initialize(ammoContainer);
                    });

                isHand = playerInventory.WeaponIndex == 3 || playerInventory.WeaponIndex == 1;

                if (isHand)
                    playerInventory.WeaponIndex = 3;
            }
            else if (itemkey.ToString() == "007" && playerInventory.Shield != null)
            {
                playerInventory.Shield.DropShield();

                isHand = true;
            }

            NetworkObject weapon = Runner.Spawn(tempweapon, Vector3.zero, Quaternion.identity, player.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<WeaponItem>().InitializeItem(
                    itemname,
                    itemkey.ToString(),
                    player,
                    back,
                    hand,
                    tempAmmo,
                    isHand
                );
            });

            if (itemkey.ToString() == "001" || itemkey.ToString() == "002")
            {
                playerInventory.PrimaryWeapon = weapon.GetComponent<WeaponItem>();
                playerInventory.PrimaryWeaponSFX = weapon.GetComponent<MeleeSoundController>();
            }
            else if (itemkey.ToString() == "003" || itemkey.ToString() == "004")
            {
                playerInventory.SecondaryWeapon = weapon.GetComponent<WeaponItem>();
                playerInventory.SecondaryWeaponSFX = weapon.GetComponent<GunSoundController>();
            }
            else if (itemkey.ToString() == "007")
                playerInventory.Shield = weapon.GetComponent<WeaponItem>();

            Weapons.Remove(itemkey);
        }
        else
        {
            if (itemkey.ToString() == "005")
            {
                if (playerInventory.RifleAmmoCount >= 999) return;

                playerInventory.RifleAmmoCount += tempAmmo;

                if (playerInventory.RifleAmmoCount >= 999) playerInventory.RifleAmmoCount = 999;
            }
            else if (itemkey.ToString() == "006")
            {
                if (playerInventory.ArrowAmmoCount >= 999) return;

                playerInventory.ArrowAmmoCount += tempAmmo;

                if (playerInventory.ArrowAmmoCount >= 999) playerInventory.ArrowAmmoCount = 999;
            }
            else if (itemkey.ToString() == "008")
            {
                if (playerInventory.HealCount >= 4) return;

                playerInventory.HealCount++;
            }
            else if (itemkey.ToString() == "009")
            {
                if (playerInventory.ArmorRepairCount >= 4) return;

                playerInventory.ArmorRepairCount++;
            }
            else if (itemkey.ToString() == "010")
            {
                if (playerInventory.TrapCount >= 4) return;

                playerInventory.TrapCount++;
            }
        }

        Weapons.Remove(itemkey);
    }
}
