using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShieldController : NetworkBehaviour
{
    [Networked][SerializeField] public string ItemID { get; set; }
    [Networked][SerializeField] public float ArmorLeft { get; set; }

    public void InitializeArmor(string itemId, float armorLeft)
    {
        ItemID = itemId;
        ArmorLeft = armorLeft;
    }
}
