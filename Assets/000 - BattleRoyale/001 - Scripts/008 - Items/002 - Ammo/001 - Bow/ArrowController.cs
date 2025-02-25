using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fusion.NetworkBehaviour;

public class ArrowController : NetworkBehaviour
{
    [SerializeField] private GameObject arrowObject;

    [Space]
    [SerializeField] private float bulletSpeed;
    [SerializeField] private LayerMask environmentLayers;
    [SerializeField] private LayerMask playerCollisionLayers;

    [Space]
    [SerializeField] private AudioSource bulletSource;
    [SerializeField] private AudioClip flybyClip;
    [SerializeField] private AudioClip hitClip;
    [SerializeField] private AudioClip bodyHitClip;

    [field: Space]
    [field: SerializeField][Networked] public NetworkObject firedByNO { get; set; }
    [field: SerializeField][Networked] public PlayerRef firedByPlayerRef { get; set; }
    [field: SerializeField][Networked] public string firedByPlayerUName { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPos { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPoint { get; set; }
    [field: SerializeField][Networked] public Vector3 StartPos { get; set; }
    [field: SerializeField][Networked] public Vector3 HitEffectRotation { get; set; }
    [field: SerializeField][Networked] public Vector3 Rotation { get; set; }
    [field: SerializeField][Networked] public bool CanTravel { get; set; }
    [field: SerializeField][Networked] public int PoolIndex { get; set; }
    [field: SerializeField][Networked] public bool Hit { get; set; }
    [field: SerializeField][Networked] public BulletObjectPool Pooler { get; set; }


    private float travelTime = 0.1f; // Bullet should reach the target in 0.1 seconds
    private float elapsedTime = 0f;

    private ChangeDetector _changeDetector;

    //  =======================

    public LagCompensatedHit TargetObj;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    private void OnEnable()
    {
        if (Runner == null) return;

        transform.position = StartPos;
        transform.rotation = Quaternion.LookRotation(Rotation, Vector3.up);
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

                    //hitEffectObj.SetActive(true);

                    arrowObject.SetActive(false);

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

    public void Fire(Vector3 startPos, Vector3 rotation, LagCompensatedHit targetObj)
    {
        transform.position = startPos;
        StartPos = startPos;
        TargetPoint = targetObj.Point;
        TargetPos = targetObj.GameObject.transform.position;
        Rotation = rotation;
        TargetObj = targetObj;
        CanTravel = true;
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
                Invoke(nameof(DestroyObject), 3f);
            }
        }
        else
        {
            Invoke(nameof(DestroyObject), 3f);
        }
    }

    private void DestroyObject()
    {
        CanTravel = false;
        Hit = false;
        Pooler.DisableArrow(PoolIndex);
    }
}
