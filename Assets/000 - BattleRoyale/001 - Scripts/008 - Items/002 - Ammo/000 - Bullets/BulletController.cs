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
    [field: SerializeField][Networked] public NetworkObject firedByNO { get; set; }
    [field: SerializeField][Networked] public PlayerRef firedByPlayerRef { get; set; }
    [field: SerializeField][Networked] public string firedByPlayerUName { get; set; }
    [field: SerializeField][Networked] public bool Destroyed { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPos { get; set; }
    [field: SerializeField][Networked] public Vector3 TargetPoint { get; set; }
    [field: SerializeField][Networked] public Vector3 StartPos { get; set; }
    [field: SerializeField][Networked] public float Distance { get; set; }
    [field: SerializeField][Networked] public float RemainingDistance { get; set; }
    [field: SerializeField][Networked] public Vector3 HitEffectRotation { get; set; }
    [field: SerializeField][Networked] public bool CanTravel { get; set; }


    //  =======================

    private ChangeDetector _changeDetector;

    public LagCompensatedHit TargetObj;

    //  =======================

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
                case nameof(Destroyed):
                    if (Destroyed)
                    {
                        hitEffectObj.SetActive(true);
                    }
                    break;
            }
        }

        if (CanTravel)
        {
            transform.position = Vector3.Lerp(StartPos, TargetPoint, 1 - (RemainingDistance / Distance));
        }
        else
        {
            transform.position = StartPos;
        }
    }

    public void Fire(PlayerRef firedByPlayerRef, Vector3 startPos, NetworkObject firedByNetworkObject, LagCompensatedHit targetObj, string playerUName)
    {
        StartPos = startPos;
        TargetPoint = targetObj.Point;
        TargetPos = targetObj.GameObject.transform.position;
        Distance = Vector3.Distance(transform.position, targetObj.Point);
        RemainingDistance = Distance;

        this.firedByPlayerRef = firedByPlayerRef;
        firedByNO = firedByNetworkObject;
        firedByPlayerUName = playerUName;
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
                    HitEffectRotation = TargetObj.Normal;

                    Hitbox temphitbox = TargetObj.Hitbox;

                    PlayerHealth playerHealth = temphitbox.Root.GetComponent<PlayerHealth>();

                    string tag = temphitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 55f,
                        "Body" => 35f,
                        "Thigh" => 25f,
                        "Shin" => 20f,
                        "Foot" => 15f,
                        "Arm" => 30f,
                        "Forearm" => 20f,
                        _ => 0f
                    };

                    Debug.Log($"Hit by {firedByPlayerUName} to {temphitbox.Root.GetBehaviour<PlayerNetworkLoader>().Username}, damage: {tempdamage} in {tag}");

                    playerHealth.ReduceHealth(tempdamage, firedByPlayerUName, firedByNO);

                    transform.position = TargetObj.Point;

                    DestroyObject();
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
        if (Object == null) return;

        Runner.Despawn(Object);
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bulletRadius);
    }
}
