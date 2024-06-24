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

    [Header("ITEM SLOT")]
    [SerializeField] private Sprite swordSlot;
    [SerializeField] private Sprite spearSlot;
    [SerializeField] private Sprite rifleSlot;
    [SerializeField] private Sprite bowSlot;

    [Header("ITEM OBJECT")]
    [SerializeField] private NetworkObject swordObject;

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
                return swordObject;
            case "003":
                return swordObject;
            case "004":
                return swordObject;
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
}
