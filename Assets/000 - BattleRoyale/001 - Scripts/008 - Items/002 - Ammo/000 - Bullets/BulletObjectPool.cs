using Fusion;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        if (HasStateAuthority) return;

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
        if (HasStateAuthority || HasInputAuthority) return;

        if (!DoneInitialize) return;

        if (BulletQueue.Count <= 0 || ArrowQueue.Count <= 0) return;

        foreach(var item in BulletQueue)
        {
            if (item.Key == null) continue;
            item.Key.gameObject.SetActive(item.Value);
        }

        foreach(var item in ArrowQueue)
        {
            if (item.Key == null) continue;
            item.Key.gameObject.SetActive(item.Value);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!DoneInitialize) return;

        if (BulletQueue.Count <= 0 || ArrowQueue.Count <= 0) return;

        foreach (var item in BulletQueue)
        {
            if (item.Key == null) continue;
            item.Key.gameObject.SetActive(item.Value);
        }

        foreach (var item in ArrowQueue)
        {
            if (item.Key == null) continue;
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


    public void SetEnabledArrow()
    {
        ArrowQueue.Set(TempArrows[CurrentArrowIndex], true);

        CurrentArrowIndex++;

        if (CurrentArrowIndex >= ArrowQueue.Count - 1)
            CurrentArrowIndex = 0;
    }

    public void DisableBullet(int index) => BulletQueue.Set(TempBullets[index], false);

    public void DisableArrow(int index) => ArrowQueue.Set(TempArrows[index], false);
}
