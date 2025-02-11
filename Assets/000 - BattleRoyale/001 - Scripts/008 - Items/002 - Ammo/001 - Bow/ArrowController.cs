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
    [field: SerializeField][Networked] public Vector3 Rotation { get; set; }
    [field: SerializeField][Networked] public bool CanTravel { get; set; }


    private float travelTime = 0.1f; // Bullet should reach the target in 0.1 seconds
    private float elapsedTime = 0f;

    //  =======================

    public LagCompensatedHit TargetObj;

    //  =======================

    public override void Spawned()
    {
        transform.position = StartPos;
    }

    public override void Render()
    {
        transform.rotation = Quaternion.LookRotation(Rotation, Vector3.up);
    }

    public void Fire(PlayerRef firedByPlayerRef, Vector3 startPos, Vector3 rotation, NetworkObject firedByNetworkObject, LagCompensatedHit targetObj, string playerUName)
    {
        StartPos = startPos;
        TargetPoint = targetObj.Point;
        TargetPos = targetObj.GameObject.transform.position;
        Distance = Vector3.Distance(transform.position, targetObj.Point);
        RemainingDistance = Distance;
        Rotation = rotation;

        this.firedByPlayerRef = firedByPlayerRef;
        firedByNO = firedByNetworkObject;
        firedByPlayerUName = playerUName;
        TargetObj = targetObj;
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
}
