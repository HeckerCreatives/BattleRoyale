using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BotMovementController : NetworkBehaviour
{
    public float MinWanderDelay
    {
        get => minWanderDelay;
    }

    public float MaxWanderDelay
    {
        get => maxWanderDelay;
    }

    //  ===============

    [SerializeField] private SimpleKCC botKCC;
    [SerializeField] private BotPlayables playables;

    [Space]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float detectionAngle = 90f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float minWanderDelay = 1f;
    [SerializeField] private float maxWanderDelay = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayer;

    [Header("DEBUGGER")]
    [SerializeField] private Vector3 direction;
    public GameObject detectedTarget;

    //  Bot States
    //  1 = Idle
    //  2 = run

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField] [Networked] public TickTimer WanderTimer { get; set; }
    [field: SerializeField] [Networked] public TickTimer IdleBeforeWanderTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(minWanderDelay, maxWanderDelay));
        }
    }

    public void DetectTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);

        foreach (var hit in hits)
        {
            // 🔒 Skip self
            if (hit.transform.root == transform.root)
                continue;

            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            if (angle < detectionAngle / 2f)
            {
                float distance = Vector3.Distance(transform.position, hit.transform.position);

                // 🔍 Check if there's a clear line of sight
                if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToTarget, distance, obstructionLayer))
                {
                    detectedTarget = hit.gameObject;
                    return;
                }
            }
        }

        detectedTarget = null; // No valid target found
    }

    public void MoveToTarget()
    {
        Vector3 moveDir = (detectedTarget.transform.position - transform.position);
        moveDir.y = 0;

        if (Vector3.Distance(botKCC.Position, detectedTarget.transform.position) > 1f)
        {
            botKCC.Move(moveDir.normalized * speed * Runner.DeltaTime);
            transform.forward = moveDir.normalized;
        }
    }

    public bool CanPunch()
    {
        if (Vector3.Distance(botKCC.Position, detectedTarget.transform.position) <= 1f) return true;

        return false;
    }

    public void PickNewWanderDirection()
    {
        direction = new Vector3(Random.Range(-500f, 500f), 0f, Random.Range(-500f, 500f)).normalized;
    }

    public void MoveInDirection()
    {
        AvoidObstacles();
        botKCC.Move(direction * speed * Runner.DeltaTime, 0f);
        botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(direction), Runner.DeltaTime * 10f));
    }

    private void AvoidObstacles()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out RaycastHit hit, 1f, obstructionLayer))
        {
            Vector3 hitNormal = hit.normal;
            hitNormal.y = 0;
            direction = Vector3.Reflect(direction, hitNormal).normalized;
        }
    }
}
