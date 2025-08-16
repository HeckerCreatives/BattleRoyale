using System;
using System.Collections.Generic;
using Fusion;
using Unity.VisualScripting;
using UnityEngine;

public class PoolObjectProvider : Fusion.Behaviour, INetworkObjectProvider
{
    /// <summary>
    /// If enabled, the provider will delay acquiring a prefab instance if the scene manager is busy.
    /// </summary>
    [InlineHelp]
    public bool DelayIfSceneManagerIsBusy = true;
    public NetworkPrefabRef bulletPrefabRef;
    public NetworkPrefabRef arrowsPrefabRef;

    /// <summary>
    /// If enabled, the provider will disable object when they return to the pool. If disabled they will keep enabled and the material will be changed (This is for easy visualization only, not a good practice)
    /// </summary>
    private bool _disableObjectInPool = true;
    
    private Dictionary<NetworkPrefabId, Queue<NetworkObject>> _freeBullets = new Dictionary<NetworkPrefabId, Queue<NetworkObject>>();
    private Dictionary<NetworkPrefabId, Queue<NetworkObject>> _freeArrows = new Dictionary<NetworkPrefabId, Queue<NetworkObject>>();

    private NetworkPrefabId _bulletId;
    private NetworkPrefabId _arrowsId;

    /// <summary>
    /// How many objects are going to be kept on the pools, 0 or negative means to pool all released objects.
    /// </summary>
    private int _maxPoolCount = 0;
    
    /// The base <see cref="NetworkObjectProviderDefault"/> by default simply instantiate a new game object.
    /// Let's create a method to use a custom logic that will pool objects.
    protected NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab,
        NetworkPrefabId contextPrefabId)
    {
        var result = default(NetworkObject);

        // Found free queue for prefab AND the queue is not empty. Return free object.

        if (bulletPrefabRef.IsValid && !_bulletId.IsValid)
        {
            var guid = (NetworkObjectGuid)bulletPrefabRef;
            _bulletId = runner.Prefabs.GetId(guid);
            if (!_bulletId.IsValid) Debug.LogWarning("Bullet prefab not found in prefab table.");
        }

        if (arrowsPrefabRef.IsValid && !_arrowsId.IsValid)
        {
            var guid = (NetworkObjectGuid)arrowsPrefabRef;
            _arrowsId = runner.Prefabs.GetId(guid);
            if (!_arrowsId.IsValid) Debug.LogWarning("Bullet prefab not found in prefab table.");
        }

        if (contextPrefabId == _bulletId)
        {
            if (_freeBullets.TryGetValue(contextPrefabId, out var freeQ))
            {
                if (freeQ.Count > 0)
                {
                    result = freeQ.Dequeue();
                    result.gameObject.SetActive(true);

                    return result;
                }
            }
            else
            {
                _freeBullets.Add(contextPrefabId, new Queue<NetworkObject>());
            }
        }
        else if (contextPrefabId == _arrowsId)
        {
            if (_freeArrows.TryGetValue(contextPrefabId, out var freeQ))
            {
                if (freeQ.Count > 0)
                {
                    result = freeQ.Dequeue();
                    result.gameObject.SetActive(true);

                    return result;
                }
            }
            else
            {
                _freeArrows.Add(contextPrefabId, new Queue<NetworkObject>());
            }
        }

        // -- At this point a free queue was not yet created or were empty. Create new object.
        result = Instantiate(prefab);

        result.gameObject.SetActive(true);

        return result;
    }

    protected void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        // No free queue for this prefab OR the pool already have the max amount of object we defined. Should be destroyed.

        if (prefabId == _bulletId)
        {
            if (_freeBullets.TryGetValue(prefabId, out var freeQ) == false || (_maxPoolCount > 0 && freeQ.Count >= _maxPoolCount))
            {
                Destroy(instance.gameObject);
                return;
            }

            // Free queue found. Should cache.
            freeQ.Enqueue(instance);
            instance.gameObject.SetActive(false);

            return;
        }
        else if (prefabId == _arrowsId)
        {
            if (_freeArrows.TryGetValue(prefabId, out var freeQ) == false || (_maxPoolCount > 0 && freeQ.Count >= _maxPoolCount))
            {
                Destroy(instance.gameObject);
                return;
            }

            // Free queue found. Should cache.
            freeQ.Enqueue(instance);
            instance.gameObject.SetActive(false);

            return;
        }

        Destroy(instance.gameObject);
    }

    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context,
        out NetworkObject instance)
    {
        
        instance = null;

        if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy) {
            return NetworkObjectAcquireResult.Retry;
        }

        NetworkObject prefab;
        try {
            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
        } catch (Exception ex) {
            Log.Error($"Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

        if (!prefab) {
            // this is ok, as long as Fusion does not require the prefab to be loaded immediately;
            // if an instance for this prefab is still needed, this method will be called again next update
            return NetworkObjectAcquireResult.Retry;
        }

        instance = InstantiatePrefab(runner, prefab, context.PrefabId);
        Assert.Check(instance);

        if (context.DontDestroyOnLoad) {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        } else {
            runner.MoveToRunnerScene(instance.gameObject);
        }

        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

        // Only pool prefabs.
        if (!context.IsBeingDestroyed) {
            if (context.TypeId.IsPrefab) {
                DestroyPrefabInstance(runner, context.TypeId.AsPrefabId, instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        if (context.TypeId.IsPrefab) {
            runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }

    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
    {
        return runner.Prefabs.GetId(prefabGuid);
    }

    private void DisableObjectsAsDefaultReleaseBehaviour(bool value)
    {
        _disableObjectInPool = value;
    }

    public bool SwitchDefaultReleaseBehaviour()
    {
        DisableObjectsAsDefaultReleaseBehaviour(!_disableObjectInPool);

        return _disableObjectInPool;
    }

    public void SetMaxPoolCount(int count)
    {
        _maxPoolCount = count;
    }
}
