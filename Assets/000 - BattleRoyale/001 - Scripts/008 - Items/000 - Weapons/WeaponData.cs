using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class WeaponData : NetworkBehaviour
{
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int AnimatorID { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string WeaponID { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public string WeaponName { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public int Ammo { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Owner { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject WeaponObject { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Parent { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkObject Hand { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool IsPickedUp { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public bool IsHand { get; set; }
}
