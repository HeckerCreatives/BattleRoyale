using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrateController : NetworkBehaviour
{
    [field: SerializeField][field: MyBox.ReadOnly][Networked, Capacity(10)] public NetworkDictionary<NetworkString<_4>, int> Weapons => default;

    public void SetDatas(Dictionary<string, int> data)
    {
        if (!HasStateAuthority) return;

        foreach (var item in data)
        {
            Weapons.Set(item.Key, item.Value);
        }
    }


}
