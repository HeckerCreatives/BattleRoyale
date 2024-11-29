using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponHandler : NetworkBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    public void MeleeDamageEnemy()
    {
        if (!HasStateAuthority) return;

        if (inventory.PrimaryWeapon == null) return;

        if (inventory.PrimaryWeapon.WeaponID == "001")
            inventory.PrimaryWeapon.GetComponent<WeaponDamageHandlerBox>().PerformFirstAttack(Object);
        else if (inventory.PrimaryWeapon.WeaponID == "002")
            inventory.PrimaryWeapon.GetComponent<WeaponDamageHandler>().PerformFirstAttack(Object);
    }

    public void ResetMeleeDamage()
    {
        if (!HasStateAuthority) return;

        if (inventory.PrimaryWeapon == null) return;

        if (inventory.PrimaryWeapon.WeaponID == "001")
            inventory.PrimaryWeapon.GetComponent<WeaponDamageHandlerBox>().ResetFirstAttack();
        else if (inventory.PrimaryWeapon.WeaponID == "002")
            inventory.PrimaryWeapon.GetComponent<WeaponDamageHandler>().ResetFirstAttack();
    }
}
