using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CrateController : NetworkBehaviour
{
    private event EventHandler<ItemListChangeEventArgs> ItemListChange;
    public event EventHandler<ItemListChangeEventArgs> OnItemListChange
    {
        add
        {
            if (ItemListChange == null || !ItemListChange.GetInvocationList().Contains(value))
                ItemListChange += value;
        }
        remove { ItemListChange -= value; }
    }

    [field: SerializeField][field: MyBox.ReadOnly][Networked, Capacity(10)] public NetworkDictionary<NetworkString<_4>, int> Weapons => default;

    public void SetDatas(Dictionary<string, int> data)
    {
        if (!HasStateAuthority) return;

        foreach (var item in data)
        {
            Weapons.Set(item.Key, item.Value);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_RemoveItemFromObject(string data)
    {
        TempItemWeaponData weaponData = JsonConvert.DeserializeObject<TempItemWeaponData>(data);

        if (HasStateAuthority)
            Weapons.Remove(Weapons.ElementAt(weaponData.index).Key);

        ItemListChange?.Invoke(this, new ItemListChangeEventArgs(weaponData.index, data));
    }
}

public class ItemListChangeEventArgs : EventArgs
{
    public int Index { get; }
    public string ItemData { get; }

    public ItemListChangeEventArgs(int index, string itemData)
    {
        Index = index;
        ItemData = itemData;
    }
}