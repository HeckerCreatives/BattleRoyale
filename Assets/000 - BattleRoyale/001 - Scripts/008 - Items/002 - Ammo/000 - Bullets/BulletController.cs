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
    [SerializeField] private float travelTime = 0.1f; // Bullet should reach the target in 0.1 seconds
    [SerializeField] private float bulletRadius;
    [SerializeField] private LayerMask environmentLayers;
    [SerializeField] private LayerMask playerCollisionLayers;
    [SerializeField] private AudioSource bulletSource;
    [SerializeField] private AudioClip flybyClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip bodyHitClip;

    [Header("LOCAL DEBUGGER")]
    [SerializeField] private bool alreadyHit;

    [field: Space]
    [field: SerializeField][Networked] public Vector3 TargetPoint { get; set; }
    [field: SerializeField][Networked] public Vector3 StartPos { get; set; }
    [field: SerializeField][Networked] public Vector3 HitEffectRotation { get; set; }
    [field: SerializeField][Networked] public bool CanTravel { get; set; }
    [field: SerializeField][Networked] public bool Hit { get; set; }
    [field: SerializeField][Networked] public int PoolIndex { get; set; }
    [field: SerializeField][Networked] public BulletObjectPool Pooler { get; set; }
    [field: SerializeField][Networked] public float TickRateAnimation { get; set; }
    [field: SerializeField][Networked] public float HitTimer { get; set; }
    [field: SerializeField][Networked] public float DecayTimer { get; set; }
    [field: SerializeField][Networked] public int HitIndex { get; set; }



    private float elapsedTime = 0f;

    //  =======================

    private ChangeDetector _changeDetector;

    public LagCompensatedHit TargetObj;

    //  =======================

    private void OnDisable()
    {
        alreadyHit = false;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        StartPos = Vector3.zero;
        DecayTimer = 0f;
        CanTravel = false;
    }

    public override void Render()
    {
        if (HasStateAuthority) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CanTravel):

                    if (!CanTravel) return;

                    bulletSource.PlayOneShot(flybyClip);
                    break;
                case nameof(HitIndex):

                    if (HitIndex == 1)
                        bulletSource.PlayOneShot(bodyHitClip);
                    else if (HitIndex == 2)
                        bulletSource.PlayOneShot(hitClip);

                    break;
            }
        }

        //if (!alreadyHit && CanTravel && StartPos != Vector3.zero)
        //{
        //    Debug.Log($"LOCAL BULLET START POS: {StartPos}");
        //    elapsedTime += Time.deltaTime;
        //    float t = Mathf.Clamp01(elapsedTime / travelTime); // Normalize to 0-1 range

        //    transform.position = Vector3.Lerp(StartPos, TargetPoint, t);

        //    if (Vector3.Distance(transform.position, TargetPoint) <= 1f)
        //    {
        //        hitEffectObj.SetActive(true);
        //        HitEffectRotation = TargetObj.Normal;

        //        if (TargetObj.Hitbox != null)
        //            bulletSource.PlayOneShot(bodyHitClip);
        //        else
        //            bulletSource.PlayOneShot(hitClip);

        //        alreadyHit = true;
        //    }
        //}
    }

    public void Fire(Vector3 startPos, LagCompensatedHit targetObj, float additionalTimer = 5f)
    {
        Debug.Log($"START POS BULLET: {startPos}");
        HitIndex = 0;
        StartPos = startPos;
        TargetPoint = targetObj.Point;
        transform.position = startPos;

        TargetObj = targetObj;

        CanTravel = true;

        TickRateAnimation = Runner.Tick * Runner.DeltaTime;

        DecayTimer = TickRateAnimation + additionalTimer;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            TickRateAnimation = Runner.Tick * Runner.DeltaTime;
            DestroyObject();
        }

        ServerMoveBullet();
    }

    private void ServerMoveBullet()
    {
        if (!HasStateAuthority) return;

        if (CanTravel && StartPos != Vector3.zero)
        {
            Debug.Log($"SERVER BULLET START POS: {StartPos}");
            elapsedTime += Runner.DeltaTime;
            float t = Mathf.Clamp01(elapsedTime / travelTime); // Normalize to 0-1 range

            transform.position = Vector3.Lerp(StartPos, TargetPoint, t);

            if (Vector3.Distance(transform.position, TargetPoint) <= 1f)
            {
                hitEffectObj.SetActive(true);
                HitEffectRotation = TargetObj.Normal;

                if (TargetObj.Hitbox != null)
                    HitIndex = 1;
                else
                    HitIndex = 2;
                    bulletSource.PlayOneShot(hitClip);

                DestroyObject();
            }
        }
    }

    private void DestroyObject()
    {
        if (CanTravel && TickRateAnimation < DecayTimer) return;

        Runner.Despawn(Object);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bulletRadius);
    }
}
