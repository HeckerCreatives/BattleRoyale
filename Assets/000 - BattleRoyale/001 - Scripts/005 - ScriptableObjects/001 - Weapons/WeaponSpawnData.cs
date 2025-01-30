using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSpawnData", menuName = "Battle Royale/Weapon/SpawnData")]
public class WeaponSpawnData : ScriptableObject
{
    [Header("ITEM LIST PICKUP")]
    [SerializeField] private Sprite swordList;
    [SerializeField] private Sprite spearList;
    [SerializeField] private Sprite rifleList;
    [SerializeField] private Sprite bowList;
    [SerializeField] private Sprite rifleAmmoList;
    [SerializeField] private Sprite bowAmmoList;

    [Header("ITEM BUTTON")]
    [SerializeField] private Sprite swordBtn;
    [SerializeField] private Sprite spearBtn;
    [SerializeField] private Sprite rifleBtn;
    [SerializeField] private Sprite bowBtn;

    [Header("ITEM SLOT")]
    [SerializeField] private Sprite swordSlot;
    [SerializeField] private Sprite spearSlot;
    [SerializeField] private Sprite rifleSlot;
    [SerializeField] private Sprite bowSlot;

    [Header("ITEM OBJECT")]
    [SerializeField] private NetworkObject swordObject;
    [SerializeField] private NetworkObject spearObject;
    [SerializeField] private NetworkObject rifleObject;
    [SerializeField] private NetworkObject bowObject;
    [SerializeField] private NetworkObject arrowContainerObject;

    public Sprite GetItemButtonSprite(string itemID)
    {
        switch (itemID)
        {
            case "001":
                return swordBtn;
            case "002":
                return spearBtn;
            case "003":
                return rifleBtn;
            case "004":
                return bowBtn;
            default: return null;
        }
    }

    public Sprite GetItemListSprite(string itemID)
    {
        switch (itemID)
        {
            case "001":
                return swordList;
            case "002":
                return spearList;
            case "003":
                return rifleList;
            case "004":
                return bowList;
            case "005":
                return rifleAmmoList;
            case "006":
                return bowAmmoList;
            default: return null;
        }
    }

    public string GetItemName(string itemID)
    {
        switch (itemID)
        {
            case "001":
                return "Sword";
            case "002":
                return "Spear";
            case "003":
                return "Rifle";
            case "004":
                return "Bow";
            case "005":
                return "Rifle Ammo";
            case "006":
                return "Bow Ammo";
            case "007":
                return "Armor";
            case "008":
                return "Heal";
            case "009":
                return "Armor Repair";
            default: return "";
        }
    }

    public NetworkObject GetItemObject(string itemID)
    {
        switch (itemID)
        {
            case "001":
                return swordObject;
            case "002":
                return spearObject;
            case "003":
                return rifleObject;
            case "004":
                return bowObject;
            case "arrowcontainer":
                return arrowContainerObject;
            default: return null;
        }
    }

    public int GetItemAnimatorIndex(string itemID)
    {
        switch (itemID)
        {
            case "001":
                return 2;
            case "002":
                return 3;
            case "003":
                return 4;
            case "004":
                return 5;
            default: return 0;
        }
    }

    public string GetItemType(string itemID)
    {
        switch (itemID)
        {
            case "001":
            case "002":
                return "Primary";
            case "003":
            case "004":
                return "Secondary";
            case "005":
                return "RifleAmmo";
            case "006":
                return "BowAmmo";
            default: return "";
        }
    }
}
