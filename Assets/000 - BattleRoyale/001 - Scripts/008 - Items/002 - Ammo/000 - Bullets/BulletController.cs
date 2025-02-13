using Fusion;
using Fusion.Addons.Physics;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class BulletController : NetworkBehaviour
{
    [SerializeField] private GameObject hitEffectObj;

    [Space]
    [SerializeField] private float bulletSpeed;
    [SerializeField] private float bulletRadius;
    [SerializeField] private LayerMask environmentLayers;
    [SerializeField] private LayerMask playerCollisionLayers;

    [field: Space]
    [field: SerializeField][Networked] public bool Destroyed { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPos { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPoint { get; set; }
    [field: SerializeField][Networked] public Vector3 StartPos { get; set; }
    [field: SerializeField][Networked] public float Distance { get; set; }
    [field: SerializeField][Networked] public float RemainingDistance { get; set; }
    [field: SerializeField][Networked] public Vector3 HitEffectRotation { get; set; }
    [field: SerializeField][Networked] public bool CanTravel { get; set; }
    [field: SerializeField][Networked] public int PoolIndex { get; set; }
    [field: SerializeField][Networked] public BulletObjectPool Pooler { get; set; }


    private float travelTime = 0.1f; // Bullet should reach the target in 0.1 seconds
    private float elapsedTime = 0f;

    //  =======================

    private ChangeDetector _changeDetector;

    public LagCompensatedHit TargetObj;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        transform.position = StartPos;
        Debug.Log($"Bullet controller {Pooler == null}");
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Destroyed):
                    if (Destroyed)
                    {
                        hitEffectObj.SetActive(true);
                    }
                    break;
            }
        }
    }

    private void Update()
    {
        if (CanTravel)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / travelTime); // Normalize to 0-1 range

            transform.position = Vector3.Lerp(StartPos, TargetPoint, t);
        }
    }

    public void Fire(Vector3 startPos, LagCompensatedHit targetObj)
    {
        transform.position = startPos;
        StartPos = startPos;
        TargetPoint = targetObj.Point;
        TargetPos = targetObj.GameObject.transform.position;
        Distance = Vector3.Distance(transform.position, targetObj.Point);
        RemainingDistance = Distance;

        TargetObj = targetObj;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!CanTravel) return;

        if (RemainingDistance > 0)
        {
            RemainingDistance -= bulletSpeed * Runner.DeltaTime;
        }
        else
        {
            if (Destroyed) return;

            if ((1 << TargetObj.GameObject.layer) == playerCollisionLayers)
            {
                if (Vector3.Distance(TargetObj.Point, TargetPos) <= 1f)
                {
                    Destroyed = true;

                    HitEffectRotation = TargetObj.Normal;

                    transform.position = TargetObj.Point;

                    Invoke(nameof(DestroyObject), 3f);
                }
                else
                {
                    Destroyed = true;

                    transform.position = TargetObj.Point;

                    Invoke(nameof(DestroyObject), 3f);
                }
            }
            else
            {
                Destroyed = true;

                transform.position = TargetObj.Point;

                Invoke(nameof(DestroyObject), 3f);
            }
        }
    }

    private void ContinueBulletTrajectory()
    {
        Vector3 moveDirection = (TargetPos - StartPos).normalized; // Get the last moving direction

        // Extend bullet movement past the original target
        TargetPos += moveDirection * 5f; // Move it forward by 5 units
        RemainingDistance = 5f; // Give it some more travel distance
    }

    private void DestroyObject()
    {
        CanTravel = false;
        Destroyed = false;
        Debug.Log($"is pooler null ? {Pooler == null}");
        Pooler.DisableBullet(PoolIndex);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bulletRadius);
    }
}
