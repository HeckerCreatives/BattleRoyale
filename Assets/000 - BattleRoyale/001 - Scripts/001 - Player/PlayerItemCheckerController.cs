using Fusion;
using NUnit.Framework.Interfaces;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerItemCheckerController : NetworkBehaviour
{
    [SerializeField] private LayerMask objecMask;
    [SerializeField] private float itemDetectorRadius;

    //  ========================

    private List<Collider> currentColliders = new List<Collider>();

    private Dictionary<string, int> _localCache = new Dictionary<string, int>();

    //  =========================


    private void FixedUpdate()
    {
        CollisionEnter();
    }

    private void CollisionEnter()
    {
        if (!HasInputAuthority) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, itemDetectorRadius, objecMask);

        currentColliders.Clear();
        currentColliders.AddRange(hitColliders);

        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Crate"))
            {
                HandleCrateEnter(hit.gameObject);
            }
            else if (hit.CompareTag("Weapon"))
            {
                //if (!WeaponsPickupDetector.Contains(hit.gameObject))
                //{
                //    WeaponsPickupDetector.Add(hit.gameObject);
                //    HandleWeaponEnter(hit.gameObject);
                //}
            }
        }
    }

    private bool CheckItemCrateDictionary(NetworkDictionary<NetworkString<_4>, int> dict1, Dictionary<string, int> dict2)
    {
        if (dict1.Count != dict2.Count) return false;

        foreach (var pair in dict1)
        {
            if (!dict2.TryGetValue(pair.Key.ToString(), out var value) || !value.Equals(pair.Value))
                return false;
        }

        return true;
    }

    private void UpdateLocalCache()
    {
        //_localCache.Clear();
        //foreach (var pair in Items)
        //{
        //    _localCache[pair.Key] = pair.Value;
        //}
    }

    private void HandleCrateEnter(GameObject other)
    {
        if (!HasInputAuthority) return;

    }
}
