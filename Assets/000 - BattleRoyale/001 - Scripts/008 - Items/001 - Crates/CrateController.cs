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

        Weapons.Remove(itemkey);
    }
}
