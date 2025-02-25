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
    [SerializeField] private AudioSource bulletSource;
    [SerializeField] private AudioClip flybyClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip bodyHitClip;

    [field: Space]
    [field: SerializeField][Networked] public Vector3 TargetPos { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPoint { get; set; }
    [field: SerializeField][Networked] public Vector3 StartPos { get; set; }
    [field: SerializeField][Networked] public Vector3 HitEffectRotation { get; set; }
    [field: SerializeField][Networked] public bool CanTravel { get; set; }
    [field: SerializeField][Networked] public bool Hit { get; set; }
    [field: SerializeField][Networked] public int PoolIndex { get; set; }
    [field: SerializeField][Networked] public BulletObjectPool Pooler { get; set; }


    private float travelTime = 0.1f; // Bullet should reach the target in 0.1 seconds
    private float elapsedTime = 0f;

    //  =======================

    private ChangeDetector _changeDetector;

    public LagCompensatedHit TargetObj;

    //  =======================

    private void OnEnable()
    {
        if (Runner == null) return;

        transform.position = StartPos;
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Hit):
                    if (HasStateAuthority) return;

                    if (!Hit) return;

                    hitEffectObj.SetActive(true);

                    if (TargetObj.Hitbox != null)
                        bulletSource.PlayOneShot(bodyHitClip);
                    else
                        bulletSource.PlayOneShot(hitClip);

                    break;
                case nameof(CanTravel):
                    if (HasStateAuthority) return;

                    if (!CanTravel) return;

                    bulletSource.PlayOneShot(flybyClip);
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

        TargetObj = targetObj;
        CanTravel = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (!CanTravel) return;

        if (transform.position != TargetPoint) return;

        if (Hit) return;

        Hit = true;

        if ((1 << TargetObj.GameObject.layer) == playerCollisionLayers)
        {
            if (Vector3.Distance(TargetObj.Point, TargetPos) <= 1f)
            {

                HitEffectRotation = TargetObj.Normal;

                transform.position = TargetObj.Point;

                Invoke(nameof(DestroyObject), 3f);
            }
            else
            {

                transform.position = TargetObj.Point;

                Invoke(nameof(DestroyObject), 3f);
            }
        }
        else
        {

            transform.position = TargetObj.Point;

            Invoke(nameof(DestroyObject), 3f);
        }
    }

    private void DestroyObject()
    {
        CanTravel = false;
        Hit = false;
        Pooler.DisableBullet(PoolIndex);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bulletRadius);
    }
}
