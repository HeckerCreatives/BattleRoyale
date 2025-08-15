using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MarketItems", menuName = "Battle Royale/Marketplace/MarketItems")]
public class MarketItems : ScriptableObject
{
    [field: SerializeField] public string ItemID { get; private set; }
    [field: SerializeField] public string ItemName { get; private set; }
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public float Price { get; private set; }
    [field: SerializeField] public float Amount { get; private set; }
    [field: SerializeField] public string Currency { get; private set; }
    [field: SerializeField] public int Consumable { get; private set; }
    [field: SerializeField] public string ItemType { get; private set; }
    [field: SerializeField] public string Rarity { get; private set; }
    [field: SerializeField] public Sprite ItemIcon { get; private set; }
}
