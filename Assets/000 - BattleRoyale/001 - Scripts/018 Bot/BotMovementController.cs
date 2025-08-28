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
    [SerializeField] private Botdata botData;

    [Space]
    [SerializeField] private float detectionRadius = 10f;
    [SerializeField] private float detectionAngle = 90f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float minWanderDelay = 1f;
    [SerializeField] private float maxWanderDelay = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayer;

    [Space]
    [SerializeField] private float maxSlopeAngle = 45f; // max walkable slope
    [SerializeField] private LayerMask groundLayer;

    [Header("DEBUGGER")]
    [SerializeField] private Vector3 direction;
    public GameObject detectedTarget;
    public Botdata cacheDetectedTargetBotdata;
    public PlayerHealthV2 cacheDetectedTargetPlayerdata;

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
        if (detectedTarget == gameObject)
        {
            SetTarget(null);
        }

        if (cacheDetectedTargetBotdata == gameObject)
        {
            SetTarget(null);
        }

        // 1. If current target is still valid → keep it
        if (detectedTarget != null && (cacheDetectedTargetBotdata != null || cacheDetectedTargetPlayerdata != null))
        {
            Vector3 dirToTarget = (detectedTarget.transform.position - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, detectedTarget.transform.position);
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            bool inRadius = distance <= detectionRadius;
            bool inAngle = angle < detectionAngle / 2f;
            bool hasLineOfSight = !Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToTarget, distance, obstructionLayer);

            bool isInvalid = false;
            bool isDead = false;

            // ❗ Add your custom Botdata status check here

            if (detectedTarget.tag == "Bot")
            {
                isInvalid = (cacheDetectedTargetBotdata.Object.IsValid && (cacheDetectedTargetBotdata.IsStagger || cacheDetectedTargetBotdata.IsGettingUp || cacheDetectedTargetBotdata.IsDead)) || cacheDetectedTargetPlayerdata == transform.root;

                isDead = cacheDetectedTargetBotdata.IsDead;
            }
            else
            {
                isInvalid = cacheDetectedTargetPlayerdata.Object.IsValid && (cacheDetectedTargetPlayerdata.IsStagger || cacheDetectedTargetPlayerdata.IsGettingUp || cacheDetectedTargetPlayerdata.IsDead);

                isDead = cacheDetectedTargetPlayerdata.IsDead;
            }

            if (inRadius && inAngle && hasLineOfSight && !isInvalid)
                return; // Target still valid
            else
            {
                if (isDead)
                    botKCC.SetLookRotation(botKCC.TransformRotation * Quaternion.Euler(0f, 180f, 0f));

                SetTarget(null); // Lost target or no longer valid
            }
        }

        // 2. If recently damaged → chase attacker
        if (botData.DamageBy != null && !botData.DamageAwareness.Expired(Runner))
        {
            GameObject attacker = botData.DamageBy.gameObject;
            if (attacker != null)
            {
                float distance = Vector3.Distance(transform.position, attacker.transform.position);
                if (distance <= 50)
                {
                    SetTarget(attacker);
                    return;
                }
            }
        }
        else
            botData.DamageBy = null;

        // 3. Otherwise → normal sphere detection
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, playerLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root || hit.transform.gameObject == gameObject)
                continue;

            Vector3 dirToTarget = (hit.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToTarget);
            float distance = Vector3.Distance(transform.position, hit.transform.position);

            if (angle < detectionAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 0.5f, dirToTarget, distance, obstructionLayer))
                {
                    SetTarget(hit.transform.root.gameObject);
                    return;
                }
            }
        }

        // No valid targets found
        SetTarget(null);
    }

    private void SetTarget(GameObject target)
    {
        detectedTarget = target;

        // Clear old cached references
        cacheDetectedTargetBotdata = null;
        cacheDetectedTargetPlayerdata = null;

        if (target == null)
            return;

        if (target.CompareTag("Bot"))
            cacheDetectedTargetBotdata = target.GetComponent<Botdata>();
        else
            cacheDetectedTargetPlayerdata = target.GetComponent<PlayerHealthV2>();
    }

    public void MoveToTarget()
    {
        Vector3 moveDir = (detectedTarget.transform.position - transform.position);
        moveDir.y = 0;

        if (Vector3.Distance(botKCC.Position, detectedTarget.transform.position) > 1f)
        {
            botKCC.Move(moveDir.normalized * speed * Runner.DeltaTime);
            botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(moveDir), Runner.DeltaTime * 10f));
        }
    }

    public bool CanPunch()
    {
        if (detectedTarget == null) return false;

        if (detectedTarget.CompareTag("Bot"))
        {
            if (detectedTarget == transform.root)
            {
                SetTarget(null);
                return false;
            }

            if (cacheDetectedTargetBotdata.IsStagger || cacheDetectedTargetBotdata.IsGettingUp || cacheDetectedTargetBotdata.IsDead) return false;
        }
        else
        {
            if (cacheDetectedTargetPlayerdata.IsStagger || cacheDetectedTargetPlayerdata.IsGettingUp || cacheDetectedTargetPlayerdata.IsDead) return false;
        }

        if (Vector3.Distance(botKCC.Position, detectedTarget.transform.position) <= 1f) return true;

        return false;
    }

    public bool CanSwordAttack()
    {
        if (detectedTarget == null) return false;

        if (detectedTarget.CompareTag("Bot"))
        {
            if (detectedTarget == transform.root)
            {
                SetTarget(null);
                return false;
            }

            if (cacheDetectedTargetBotdata.IsStagger || cacheDetectedTargetBotdata.IsGettingUp || cacheDetectedTargetBotdata.IsDead) return false;
        }
        else
        {
            if (cacheDetectedTargetPlayerdata.IsStagger || cacheDetectedTargetPlayerdata.IsGettingUp || cacheDetectedTargetPlayerdata.IsDead) return false;
        }

        if (Vector3.Distance(botKCC.Position, detectedTarget.transform.position) <= 1.5f) return true;

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
        // 1. Check direct obstacle in front
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out RaycastHit hit, 1f, obstructionLayer))
        {
            Vector3 hitNormal = hit.normal;
            hitNormal.y = 0;
            direction = Vector3.Reflect(direction, hitNormal).normalized;
            return;
        }

        // 2. Check steep slope ahead
        Vector3 checkPos = transform.position + direction * 0.5f; // look slightly ahead
        if (Physics.Raycast(checkPos + Vector3.up * 0.5f, Vector3.down, out RaycastHit slopeHit, 2f, groundLayer))
        {
            float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);

            if (slopeAngle > maxSlopeAngle) // too steep, treat like an obstacle
            {
                Vector3 slopeNormal = slopeHit.normal;
                slopeNormal.y = 0;
                direction = Vector3.Reflect(direction, slopeNormal).normalized;
            }
        }

        // 3. Safe zone correction
        if (IsOutsideSafeZone(transform.position, botData.ServerManager.SafeZone.transform) && botData.ServerManager.CurrentGameState == GameState.ARENA)
        {
            Vector3 toSafeZone = GetSafeZoneDirection(transform.position, botData.ServerManager.SafeZone.transform);

            // Blend wander/avoid with safe zone direction
            direction = Vector3.Lerp(direction, toSafeZone, 0.6f).normalized;
        }
    }

    private bool IsOutsideSafeZone(Vector3 botPosition, Transform safeZone)
    {
        float radius = safeZone.localScale.x * 0.5f; // X = radius in world units
        Vector3 flatBotPos = new Vector3(botPosition.x, 0, botPosition.z);
        Vector3 flatZonePos = new Vector3(safeZone.position.x, 0, safeZone.position.z);

        float distance = Vector3.Distance(flatBotPos, flatZonePos);
        return distance > radius;
    }

    private Vector3 GetSafeZoneDirection(Vector3 botPosition, Transform safeZone)
    {
        Vector3 toCenter = safeZone.position - botPosition;
        toCenter.y = 0; // ignore height
        return toCenter.normalized;
    }
}
