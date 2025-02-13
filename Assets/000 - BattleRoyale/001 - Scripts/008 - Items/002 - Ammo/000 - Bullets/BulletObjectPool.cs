using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BulletObjectPool : NetworkBehaviour
{
    [field: Header("DEBUGGER")]
    [Networked][Capacity(15)][UnitySerializeField] public NetworkDictionary<NetworkObject, bool> BulletQueue => default;
    [Networked][Capacity(15)][UnitySerializeField] public NetworkDictionary<NetworkObject, bool> ArrowQueue => default;
    [field: SerializeField][Networked] public int CurrentBulletIndex { get; set; }
    [field: SerializeField][Networked] public int LastBulletIndex { get; set; }
    [field: SerializeField][Networked] public int CurrentArrowIndex { get; set; }
    [field: SerializeField][Networked] public bool DoneInitialize { get; set; }

    //  =======================

    public List<NetworkObject> TempBullets = new List<NetworkObject>();
    public List<NetworkObject> TempArrows = new List<NetworkObject>();

    //  =======================

    public override void Spawned()
    {
        foreach(var item in BulletQueue)
        {
            TempBullets.Add(item.Key);
        }

        foreach(var item in ArrowQueue)
        {
            TempArrows.Add(item.Key);
        }
    }

    public override void Render()
    {
        if (!DoneInitialize) return;

        foreach(var item in BulletQueue)
        {
            item.Key.gameObject.SetActive(item.Value);
        }

        foreach(var item in ArrowQueue)
        {
            item.Key.gameObject.SetActive(item.Value);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!DoneInitialize) return;

        foreach (var item in BulletQueue)
        {
            item.Key.gameObject.SetActive(item.Value);
        }

        foreach (var item in ArrowQueue)
        {
            item.Key.gameObject.SetActive(item.Value);
        }
    }

    public void SetEnabledBullet()
    {
        BulletQueue.Set(TempBullets[CurrentBulletIndex], true);

        CurrentBulletIndex++;

        if (CurrentBulletIndex >= BulletQueue.Count - 1)
            CurrentBulletIndex = 0;
    }

    public void DisableBullet(int index) => BulletQueue.Set(TempBullets[index], false);
}
