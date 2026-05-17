using System;
using Fusion;
using Fusion.Addons.SimpleKCC;
using UnityEngine;
using UnityEngine.AI;

public class BotMovementController : NetworkBehaviour
{
    private static string NormalizeWeaponId(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        id = id.Trim();
        if (int.TryParse(id, out int n))
            return n.ToString("000");
        return id;
    }

    public float MinWanderDelay => minWanderDelay;
    public float MaxWanderDelay => maxWanderDelay;

    public Botdata AIBotdata => botData;
    public Vector3 AIMotorPosition => botKCC != null ? botKCC.Position : transform.position;
    /// <summary>Optional designer policy for <see cref="BotAIStrategicBrain"/>. Null is valid.</summary>
    public BotAIPolicy StrategicAiPolicy => strategicAiPolicy;

    public float FistMeleeEvadeRadius => meleeRange + 0.45f;
    public float SwordMeleeEvadeRadius => meleeRange + 0.55f;
    public float SpearMeleeEvadeRadius => spearRange + 0.6f;

    //  ===============

    [SerializeField] private SimpleKCC botKCC;
    [SerializeField] private BotPlayables playables;
    [SerializeField] private Botdata botData;
    [SerializeField] private NavMeshAgent navAgent;

    [Space]
    [SerializeField] private float detectionRadius = 20f;
    [SerializeField] private float detectionAngle = 100f;
    [Tooltip("When an out-of-FOV but visible enemy is detected, chance (0-1) to immediately attack.")]
    [SerializeField] private float peripheralAttackChance = 0.9f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float minWanderDelay = 1f;
    [SerializeField] private float maxWanderDelay = 5f;
    [SerializeField] private float meleeRange = 1.5f;
    [SerializeField] private float spearRange = 2.2f;
    [SerializeField] private float minRifleRange = 10f;
    [SerializeField] private float minBowRange = 7f;
    [SerializeField] private float rangedEngageDistance = 15f;
    [SerializeField] private float lootScanRadius = 10f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private LayerMask obstructionLayer;
    [SerializeField] private LayerMask itemLayer;

    [Header("Strategic AI")]
    [Tooltip("Optional. When empty, bots use BuiltIn defaults (legacy behaviour).")]
    [SerializeField] private BotAIPolicy strategicAiPolicy;

    [Header("DEBUGGER")]
    public GameObject detectedTarget;
    public Botdata cacheDetectedTargetBotdata;
    public PlayerHealthV2 cacheDetectedTargetPlayerdata;

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField][Networked] public TickTimer WanderTimer { get; set; }
    [field: SerializeField][Networked] public TickTimer IdleBeforeWanderTimer { get; set; }
    [field: SerializeField][Networked] public TickTimer LootScanTimer { get; set; }

    //  ===============

    private Vector3 _wanderDir;
    private int _strafeDir = 1;
    private float _strafeChangeTimer;

    // Wander destination (NavMesh-sampled)
    private Vector3 _wanderDestination;
    private bool _hasWanderDestination;

    // Simulated camera — virtual gaze direction for FOV-based detection
    private Vector3 _simCamForward;
    private float _simCamScanTimer;

    // Non-networked loot state — recomputed on authority each scan
    private Transform _lootTarget;
    private CrateController _lootCrate;
    private PrimaryWeaponItem _lootPrimary;
    private SecondaryWeaponItem _lootSecondary;
    private float _lootNoProgressTimer;
    private float _lootLastDistance = float.MaxValue;
    private int _lootBlacklistInstanceId = -1;
    private float _lootBlacklistTimer;

    // Stagger bot melee initiation so mirrored bots rarely enter the exact same punch state on one tick.
    private int _nextMeleeAggressionTick;
    private Vector3 _lastNavProbePosition;
    private Vector3 _lastNavDestination;
    private Vector3 _activeNavDestination = Vector3.positiveInfinity;
    private Vector3 _smoothedNavDir;
    private float _navDirGraceTimer;
    private float _peripheralScanTimer;
    private float _stuckTimer;
    private BuildingExit _exitTarget; // non-null while routing through a building exit
    private float _unstuckBiasTimer;
    private int _unstuckBiasSign = 1;
    // Melee lunge stuck recovery (when driving directly into walls during attack)
    private Vector3 _lungeLastProbePosition;
    private float _lungeStuckTimer;
    private float _lungeEscapeTimer;
    private Vector3 _lungeEscapeDir = Vector3.zero;
    private int _lungeEscapeSign = 1;
    private int _pathPendingFrames;

    private BotAIStrategicBrain _strategicBrain;

    //  ===============

    private void Awake()
    {
        _strategicBrain = new BotAIStrategicBrain(this);
    }

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        navAgent.updatePosition = false;
        navAgent.updateRotation = false;
        navAgent.speed = speed;

        // Snap the agent onto the NavMesh at spawn so isOnNavMesh is true from the first tick.
        // Without this the agent never places itself and NavigateTo() always falls back to direct movement.
        if (NavMesh.SamplePosition(botKCC.Position, out NavMeshHit spawnHit, 5f, NavMesh.AllAreas))
            navAgent.Warp(spawnHit.position);

        _simCamForward = transform.forward;

        IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(Runner, UnityEngine.Random.Range(minWanderDelay, maxWanderDelay));
        LootScanTimer = TickTimer.CreateFromSeconds(Runner, 2f);

        ScheduleMeleeAggressionKickoff();
        _strategicBrain?.Bootstrap(Runner != null ? Runner.Tick : 0);
    }

    private void ScheduleMeleeAggressionKickoff()
    {
        int idx = Mathf.Max(0, botData.BotIndex);
        _nextMeleeAggressionTick = Runner.Tick + 3 + Mathf.Abs((idx * 37 + idx * idx) % 22);
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // Keep NavMeshAgent in sync with KCC position.
        // If the agent has drifted off the NavMesh (e.g. after a fall or teleport), re-snap it
        // via Warp so isOnNavMesh recovers and path queries work again next tick.
        if (navAgent.isOnNavMesh)
        {
            navAgent.nextPosition = botKCC.Position;
        }
        else if (NavMesh.SamplePosition(botKCC.Position, out NavMeshHit rewarpHit, 20f, NavMesh.AllAreas))
        {
            navAgent.Warp(rewarpHit.position);
        }

        _strategicBrain?.TickReplan();
    }

    //  ===================== TARGET DETECTION =====================

    public void DetectTarget()
    {
        UpdateSimulatedCamera();

        // Self-reference guard
        if (detectedTarget == gameObject || (cacheDetectedTargetBotdata != null && cacheDetectedTargetBotdata.gameObject == gameObject))
            SetTarget(null);

        // Keep valid current target
        if (detectedTarget != null)
        {
            if (IsTargetValid())
            {
                float d = Vector3.Distance(botKCC.Position, detectedTarget.transform.position);
                if (d <= detectionRadius * 1.5f)
                {
                    // Re-check LOS — release target if it has stepped behind a wall
                    Vector3 origin = botKCC.Position + Vector3.up * 0.5f;
                    Vector3 toTarget = (detectedTarget.transform.position + Vector3.up * 0.5f - origin).normalized;
                    int losMask = obstructionLayer.value != 0 ? obstructionLayer.value : ~playerLayer.value;
                    if (!Physics.Raycast(origin, toTarget, d, losMask))
                        return; // still visible — keep target
                    // LOS broken — fall through to clear target and re-scan
                }
            }

            if (CanReadNetworkedCombatState(cacheDetectedTargetBotdata) && cacheDetectedTargetBotdata.IsDead)
                botKCC.SetLookRotation(botKCC.TransformRotation * Quaternion.Euler(0f, 180f, 0f));

            SetTarget(null);
        }

        // Damage awareness — pursue attacker regardless of angle
        if (botData.DamageBy != null && !botData.DamageAwareness.Expired(Runner))
        {
            GameObject attacker = botData.DamageBy.gameObject;
            if (IsAliveCombatCandidate(attacker) && Vector3.Distance(botKCC.Position, attacker.transform.position) <= 60f)
            {
                SetTarget(attacker);
                return;
            }
        }
        else
        {
            botData.DamageBy = null;
        }

        // FOV cone sphere — pick closest visible target within simulated gaze
        Collider[] hits = Physics.OverlapSphere(botKCC.Position, detectionRadius, playerLayer);
        GameObject closest = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            GameObject root = hit.transform.root.gameObject;
            if (root == gameObject) continue;
            if (!IsAliveCombatCandidate(root)) continue;

            float dist = Vector3.Distance(botKCC.Position, root.transform.position);
            if (dist >= closestDist) continue;

            Vector3 origin = botKCC.Position + Vector3.up * 0.5f;
            Vector3 dir = (root.transform.position + Vector3.up * 0.5f - origin).normalized;

            float angleToTarget = Vector3.Angle(_simCamForward, dir);
            if (angleToTarget > detectionAngle * 0.5f) continue;

            int mask = obstructionLayer.value != 0 ? obstructionLayer.value : ~playerLayer.value;
            if (Physics.Raycast(origin, dir, dist, mask)) continue;

            closest = root;
            closestDist = dist;
        }

        // Peripheral awareness — re-use the same collider list for out-of-FOV detection.
        // Only runs every ~0.5 s (with random jitter) so the probability feels natural.
        if (closest == null)
            closest = CheckPeripheralAwareness(hits);

        SetTarget(closest);
    }

    private GameObject CheckPeripheralAwareness(Collider[] hits)
    {
        _peripheralScanTimer -= Runner.DeltaTime;
        if (_peripheralScanTimer > 0f) return null;
        _peripheralScanTimer = 0.45f + UnityEngine.Random.Range(0f, 0.2f);

        Vector3 origin = botKCC.Position + Vector3.up * 0.5f;
        int mask = obstructionLayer.value != 0 ? obstructionLayer.value : ~playerLayer.value;

        foreach (var hit in hits)
        {
            GameObject root = hit.transform.root.gameObject;
            if (root == gameObject) continue;
            if (!IsAliveCombatCandidate(root)) continue;

            float dist = Vector3.Distance(botKCC.Position, root.transform.position);
            Vector3 dir = (root.transform.position + Vector3.up * 0.5f - origin).normalized;

            // Only consider entities OUTSIDE the FOV cone; those inside are handled by the main scan.
            float angle = Vector3.Angle(_simCamForward, dir);
            if (angle <= detectionAngle * 0.5f) continue;

            // Require clear LOS even for peripheral detection.
            if (Physics.Raycast(origin, dir, dist, mask)) continue;

            // Visible vicinity/peripheral target:
            // 90% attack immediately, 10% ignore and keep current movement plan.
            if (UnityEngine.Random.value <= Mathf.Clamp01(peripheralAttackChance))
                return root;
            return null;
        }
        return null;
    }

    public void PickFleeDirection(Vector3 threatPosition)
    {
        Vector3 away = botKCC.Position - threatPosition;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = botKCC.TransformRotation * Vector3.back;
        away = away.normalized;

        for (int i = 0; i < 4; i++)
        {
            Vector3 candidate = botKCC.Position + away * UnityEngine.Random.Range(8f, 15f);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (navAgent.isOnNavMesh && navAgent.CalculatePath(hit.position, path)
                    && path.status == NavMeshPathStatus.PathComplete)
                {
                    _wanderDestination = hit.position;
                    _hasWanderDestination = true;
                    _wanderDir = away;
                    WanderTimer = TickTimer.CreateFromSeconds(Runner, UnityEngine.Random.Range(3f, 5f));
                    return;
                }
            }
            away = Quaternion.Euler(0f, UnityEngine.Random.Range(-40f, 40f), 0f) * away;
        }
    }


    private bool IsAliveCombatCandidate(GameObject targetRoot)
    {
        if (targetRoot == null) return false;

        if (targetRoot.CompareTag("Bot"))
        {
            Botdata bd = targetRoot.GetComponent<Botdata>();
            return CanReadNetworkedCombatState(bd) && !bd.IsDead;
        }

        PlayerHealthV2 ph = targetRoot.GetComponent<PlayerHealthV2>();
        return CanReadNetworkedCombatState(ph) && !ph.IsDead;
    }

    private bool IsTargetValid()
    {
        if (detectedTarget == null) return false;

        if (detectedTarget.CompareTag("Bot"))
        {
            if (!CanReadNetworkedCombatState(cacheDetectedTargetBotdata))
                return false;
            return !cacheDetectedTargetBotdata.IsDead;
        }
        else
        {
            if (!CanReadNetworkedCombatState(cacheDetectedTargetPlayerdata))
                return false;
            return !cacheDetectedTargetPlayerdata.IsDead;
        }
    }

    /// <summary>
    /// Fusion throws if <see cref="NetworkBehaviour"/> networked fields are read before simulation state is ready.
    /// <see cref="NetworkObject.IsValid"/> can be true slightly earlier than that window.
    /// </summary>
    private static bool CanReadNetworkedCombatState(NetworkBehaviour nb)
    {
        return nb != null && nb.Object != null && nb.Object.IsValid && nb.Object.IsInSimulation;
    }

    private void SetTarget(GameObject target)
    {
        if (detectedTarget == target) return;

        detectedTarget = target;
        cacheDetectedTargetBotdata = null;
        cacheDetectedTargetPlayerdata = null;

        if (target == null) return;

        if (target.CompareTag("Bot"))
            cacheDetectedTargetBotdata = target.GetComponent<Botdata>();
        else
            cacheDetectedTargetPlayerdata = target.GetComponent<PlayerHealthV2>();
    }

    private void UpdateSimulatedCamera()
    {
        if (detectedTarget != null)
        {
            Vector3 toTarget = detectedTarget.transform.position - botKCC.Position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.01f)
                _simCamForward = Vector3.Lerp(_simCamForward, toTarget.normalized, Runner.DeltaTime * 8f).normalized;
        }
        else
        {
            _simCamScanTimer += Runner.DeltaTime;
            float sweepAngle = Mathf.Sin(_simCamScanTimer * 0.6f) * 65f;
            _simCamForward = Quaternion.Euler(0f, sweepAngle, 0f) * (botKCC.TransformRotation * Vector3.forward);
        }
    }

    //  ===================== MOVEMENT =====================

    public void MoveToTarget()
    {
        if (detectedTarget == null) return;
        if (!IsTargetValid()) { SetTarget(null); return; }

        float stopDist = GetEngageStopDistance();
        float currentDist = Vector3.Distance(botKCC.Position, detectedTarget.transform.position);

        Vector3 awayDir = botKCC.Position - detectedTarget.transform.position;
        awayDir.y = 0f;
        if (awayDir.sqrMagnitude < 0.01f)
            awayDir = botKCC.TransformRotation * Vector3.forward;
        awayDir.Normalize();

        // Too close — back away while still facing target
        if (currentDist < stopDist - 0.25f)
        {
            botKCC.Move(awayDir * speed * 0.7f * Runner.DeltaTime, 0f);
            // Flee/disengage should face movement direction (run forward), not backpedal.
            botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(awayDir), Runner.DeltaTime * 12f));
            return;
        }

        // At sweet spot — hold position, just face target.
        // Still run the separation pass so a third bot that wandered too close gets pushed away.
        if (currentDist <= stopDist + 0.2f)
        {
            // Include the current target in separation while in sweet-spot,
            // otherwise two melee bots can lock nose-to-nose forever.
            Vector3 sep = ComputeSeparationOffset(includeDetectedTarget: true);
            if (sep.sqrMagnitude > 0.001f)
            {
                botKCC.Move(sep, 0f);
                botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(sep.normalized), Runner.DeltaTime * 10f));
            }
            else
            {
                // If no overlap push is available, keep micro-orbiting so melee pairs don't freeze.
                FaceTarget();
                ApplyStrafe();
            }
            return;
        }

        // Too far — navigate toward target stopping at engagement distance
        NavigateTo(detectedTarget.transform.position + awayDir * stopDist);
    }

    private Vector3 ComputeSeparationOffset(bool includeDetectedTarget = false)
    {
        const float sepRadius = 0.75f;
        Collider[] near = Physics.OverlapSphere(botKCC.Position, sepRadius, playerLayer);
        Vector3 push = Vector3.zero;
        foreach (var c in near)
        {
            GameObject root = c.transform.root.gameObject;
            if (root == gameObject) continue;
            if (!includeDetectedTarget && root == detectedTarget) continue;
            Vector3 away = botKCC.Position - root.transform.position;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist < 0.001f) away = botKCC.TransformRotation * Vector3.right;
            else away /= dist;
            push += away * (sepRadius - Mathf.Min(dist, sepRadius));
        }
        if (push.sqrMagnitude < 0.001f) return Vector3.zero;
        return push.normalized * speed * 0.6f * Runner.DeltaTime;
    }

    /// <summary>Forward lunge for melee attack states — suppressed when already inside the engagement bubble.
    /// Also separates from any overlapping bot/player so lunge does not drive bots through each other.</summary>
    public void TryLungeForward(float magnitude)
    {
        // Separation pass: if any entity on the player layer is within overlap radius, push away
        // instead of lunging. Handles cases where KCC layers are not configured to block each other.
        Vector3 sep = ComputeSeparationOffset();
        if (sep.sqrMagnitude > 0.001f)
        {
            botKCC.Move(sep, 0f);
            return;
        }

        // Anti-stuck for melee lunges (bots can "pin" against a wall because they drive directly).
        if (Runner != null)
        {
            if (_lungeEscapeTimer > 0f)
            {
                _lungeEscapeTimer -= Runner.DeltaTime;
                if (_lungeEscapeTimer > 0f && _lungeEscapeDir.sqrMagnitude > 0.001f)
                {
                    botKCC.Move(_lungeEscapeDir * magnitude, 0f);
                    return;
                }
            }

            if (_lungeLastProbePosition == Vector3.zero)
                _lungeLastProbePosition = botKCC.Position;

            float moved = Vector3.Distance(new Vector3(_lungeLastProbePosition.x, 0f, _lungeLastProbePosition.z),
                new Vector3(botKCC.Position.x, 0f, botKCC.Position.z));

            if (moved < 0.015f)
                _lungeStuckTimer += Runner.DeltaTime;
            else
                _lungeStuckTimer = Mathf.Max(0f, _lungeStuckTimer - Runner.DeltaTime * 0.6f);

            _lungeLastProbePosition = botKCC.Position;

            if (_lungeStuckTimer > 0.4f)
            {
                _lungeStuckTimer = 0f;
                _lungeEscapeTimer = 0.45f;
                _lungeEscapeSign = UnityEngine.Random.value > 0.5f ? 1 : -1;

                Vector3 desiredDir = botKCC.TransformDirection;
                desiredDir.y = 0f;
                if (desiredDir.sqrMagnitude < 0.001f) desiredDir = botKCC.Transform.forward;
                desiredDir.Normalize();

                Vector3 side = Vector3.Cross(Vector3.up, desiredDir).normalized;
                Vector3 escapeCandidate = (desiredDir * 0.25f + side * _lungeEscapeSign * 0.75f).normalized;

                // Probe both sides using current obstruction mask; if mask is empty, probe all layers.
                int mask = obstructionLayer.value == 0 ? Physics.AllLayers : obstructionLayer.value;
                Vector3 rayStart = botKCC.Position + Vector3.up * 0.45f;
                bool blocked = Physics.Raycast(rayStart, escapeCandidate, 1.0f, mask);
                if (blocked)
                {
                    Vector3 other = (desiredDir * 0.25f - side * _lungeEscapeSign * 0.75f).normalized;
                    if (!Physics.Raycast(rayStart, other, 1.0f, mask))
                        escapeCandidate = other;
                }

                _lungeEscapeDir = escapeCandidate;
                botKCC.Move(_lungeEscapeDir * magnitude, 0f);
                return;
            }
        }

        if (detectedTarget == null)
        {
            botKCC.Move(botKCC.TransformDirection * magnitude, 0f);
            return;
        }

        float currentDist = Vector3.Distance(botKCC.Position, detectedTarget.transform.position);
        float minSpacing = GetEngageStopDistance() * 0.65f;

        if (currentDist > minSpacing)
            botKCC.Move(botKCC.TransformDirection * magnitude, 0f);
    }

    private float GetEngageStopDistance()
    {
        var inv = botData.Inventory;
        if (inv.WeaponIndex == 3) return 6f; // comfortable shooting range — close enough to hit, far enough to stay safe
        if (inv.WeaponIndex == 2)
        {
            string id = NormalizeWeaponId(inv.GetPrimaryWeaponID());
            return id == "002" ? spearRange * 0.8f : meleeRange * 0.85f;
        }
        return meleeRange * 0.6f;
    }

    public void MoveInDirection()
    {
        if (MultiplayerServerManager.Instance != null
            && MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA
            && IsOutsideSafeZone())
        {
            NavigateToSafeZone();
            return;
        }

        if (HasStateAuthority && _strategicBrain != null && _strategicBrain.TryDriveExplorationMovement())
            return;

        if (_wanderDir == Vector3.zero || !_hasWanderDestination)
            PickNewWanderDirection();

        if (_hasWanderDestination)
        {
            // Reached destination — stop and pick a new one next tick.
            if (Vector3.Distance(botKCC.Position, _wanderDestination) < 1.2f)
            {
                _hasWanderDestination = false;
                botKCC.Move(Vector3.zero, 0f);
                return;
            }
            NavigateTo(_wanderDestination);
        }
        else
        {
            // No safe NavMesh destination — hold position rather than risk a cliff.
            botKCC.Move(Vector3.zero, 0f);
        }
    }

    /// <summary>Idle stand: look toward likely enemy approach when holding a camp.</summary>
    public bool TryStrategicIdleBehaviour()
    {
        return _strategicBrain != null && _strategicBrain.TryIdleHoldCampFace();
    }

    public void FacePositionOnPlane(Vector3 world)
    {
        if (Runner == null) return;

        Vector3 d = world - botKCC.Position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;

        Quaternion look = Quaternion.LookRotation(d.normalized);
        botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, look, Mathf.Clamp01(Runner.DeltaTime * 5f)));
    }

    /// <summary>Used by modular AI brains: Navigate using XZ from a flat planning space.</summary>
    public void NavigateFlatXZ(Vector3 flatXZPosition)
    {
        Vector3 dest = flatXZPosition;
        dest.y = botKCC.Position.y;
        NavigateTo(dest);
    }

    public void ApplyStrafePublic() => ApplyStrafe();

    public void PickNewWanderDirection()
    {
        _wanderDir = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f)).normalized;

        // Try several long-range candidate distances (50–100m) to keep bots roaming broadly.
        // Falling back to a raw direction can walk the bot off cliffs/ledges.
        for (int attempt = 0; attempt < 4; attempt++)
        {
            Vector3 candidate = botKCC.Position + _wanderDir * UnityEngine.Random.Range(50f, 100f);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            {
                NavMeshPath path = new NavMeshPath();
                if (navAgent.isOnNavMesh && navAgent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                {
                    _wanderDestination = hit.position;
                    _hasWanderDestination = true;
                    return;
                }
            }
            // Rotate the direction and try again
            _wanderDir = Quaternion.Euler(0f, UnityEngine.Random.Range(45f, 135f), 0f) * _wanderDir;
        }

        // Long-range search failed — try a short-range fallback (1–3 m) so the bot
        // always has somewhere to step rather than freezing in the run animation.
        for (int f = 0; f < 4; f++)
        {
            Vector3 shortDir = Quaternion.Euler(0f, f * 90f + UnityEngine.Random.Range(-30f, 30f), 0f) * Vector3.forward;
            Vector3 shortCandidate = botKCC.Position + shortDir * UnityEngine.Random.Range(1f, 3f);
            if (NavMesh.SamplePosition(shortCandidate, out NavMeshHit shortHit, 2f, NavMesh.AllAreas))
            {
                _wanderDestination = shortHit.position;
                _hasWanderDestination = true;
                return;
            }
        }

        // All wander candidates failed — bot is likely on a disconnected NavMesh island
        // (inside a building). Route toward the nearest reachable building exit so the bot
        // walks out rather than freezing indefinitely inside.
        BuildingExit nearestExit = BuildingExit.FindNearest(navAgent, botKCC.Position);
        if (nearestExit != null)
        {
            _wanderDestination = nearestExit.Position;
            _hasWanderDestination = true;
            return;
        }

        _hasWanderDestination = false;
        _wanderDestination = botKCC.Position;
    }

    public void NavigateTo(Vector3 destination)
    {
        Vector3 from = botKCC.Position;
        Vector3 toDest = destination - from;
        toDest.y = 0f;
        if (toDest.sqrMagnitude <= 0.04f)
        {
            _stuckTimer = 0f;
            _smoothedNavDir = Vector3.zero;
            _navDirGraceTimer = 0f;
            botKCC.Move(Vector3.zero, 0f);
            return;
        }

        Vector3 moveDir = Vector3.zero;

        if (navAgent.isOnNavMesh)
        {
            // Only push a new destination when it has moved meaningfully — SetDestination cancels
            // the in-progress path calculation, so calling it every tick means the path never
            // finishes and desiredVelocity stays zero, causing the direct-line fallback to always fire.
            if ((destination - _activeNavDestination).sqrMagnitude > 1.44f)
            {
                navAgent.SetDestination(destination);
                _activeNavDestination = destination;
            }

            if (navAgent.pathPending)
            {
                if (++_pathPendingFrames <= 3)
                {
                    if (_smoothedNavDir.sqrMagnitude > 0.001f)
                    {
                        botKCC.Move(_smoothedNavDir * speed * 0.8f * Runner.DeltaTime, 0f);
                        botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(_smoothedNavDir), Runner.DeltaTime * 8f));
                    }
                    else
                    {
                        botKCC.Move(Vector3.zero, 0f);
                    }
                    return;
                }
                _pathPendingFrames = 0; // took too long — proceed with whatever the agent has
            }
            else
            {
                _pathPendingFrames = 0;
            }

            // Use post-RVO velocity so bots steer around each other naturally.
            // Fall back to desiredVelocity if the agent hasn't started moving yet.
            Vector3 desired = navAgent.velocity;
            if (desired.sqrMagnitude <= 0.01f)
                desired = navAgent.desiredVelocity;
            if (desired.sqrMagnitude > 0.01f)
            {
                desired.y = 0f;
                moveDir = desired.normalized;
            }
        }

        // Only fall back to direct-line movement when genuinely off the NavMesh.
        // On the NavMesh with zero desiredVelocity means the path is blocked or destination
        // is reached — hold position rather than ignoring NavMesh obstacles.
        if (moveDir.sqrMagnitude <= 0.01f)
        {
            if (navAgent.isOnNavMesh)
            {
                if (_smoothedNavDir.sqrMagnitude > 0.001f && _navDirGraceTimer > 0f)
                {
                    _navDirGraceTimer -= Runner.DeltaTime;
                    moveDir = _smoothedNavDir;
                }
                else
                {
                    _smoothedNavDir = Vector3.zero;
                    botKCC.Move(Vector3.zero, 0f);
                    return;
                }
            }
            // Off NavMesh (water, void, etc.) — steer toward the nearest walkable surface
            // instead of the destination. Walking straight toward the target ignores terrain.
            if (NavMesh.SamplePosition(botKCC.Position, out NavMeshHit meshHit, 20f, NavMesh.AllAreas))
            {
                Vector3 toMesh = meshHit.position - botKCC.Position;
                toMesh.y = 0f;
                if (toMesh.sqrMagnitude > 0.01f)
                    moveDir = toMesh.normalized;
            }
            if (moveDir.sqrMagnitude <= 0.01f)
            {
                botKCC.Move(Vector3.zero, 0f);
                return;
            }
        }

        if (moveDir.sqrMagnitude <= 0.01f)
            return;

        moveDir = ResolveStuckSteering(moveDir, destination);
        _smoothedNavDir = _smoothedNavDir.sqrMagnitude > 0.001f
            ? Vector3.Slerp(_smoothedNavDir, moveDir, Mathf.Clamp01(Runner.DeltaTime * 12f))
            : moveDir;
        if (_smoothedNavDir.sqrMagnitude > 0.001f)
            _smoothedNavDir.Normalize();
        _navDirGraceTimer = 0.18f;

        botKCC.Move(_smoothedNavDir * speed * Runner.DeltaTime, 0f);
        botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(_smoothedNavDir), Runner.DeltaTime * 8f));
    }

    /// <summary>
    /// Prevents bots from pinning into walls when path and collision layer separation is unavailable.
    /// It detects low progress toward the same destination and temporarily injects strafe/back-off steering.
    /// </summary>
    private Vector3 ResolveStuckSteering(Vector3 desiredDir, Vector3 destination)
    {
        if (Runner == null)
            return desiredDir;

        Vector3 now = botKCC.Position;
        Vector3 nowFlat = new Vector3(now.x, 0f, now.z);
        Vector3 lastFlat = new Vector3(_lastNavProbePosition.x, 0f, _lastNavProbePosition.z);
        Vector3 destFlat = new Vector3(destination.x, 0f, destination.z);

        float moved = Vector3.Distance(nowFlat, lastFlat);
        float dist = Vector3.Distance(nowFlat, destFlat);
        bool sameDestination = Vector3.Distance(destFlat, _lastNavDestination) < 1.6f;

        if (sameDestination && dist > 1.2f && moved < 0.02f)
            _stuckTimer += Runner.DeltaTime;
        else
            _stuckTimer = Mathf.Max(0f, _stuckTimer - Runner.DeltaTime * 0.75f);

        _lastNavProbePosition = now;
        _lastNavDestination = destFlat;

        if (_stuckTimer < 0.8f)
            return desiredDir;

        _unstuckBiasTimer -= Runner.DeltaTime;
        if (_unstuckBiasTimer <= 0f)
        {
            _unstuckBiasSign = UnityEngine.Random.value > 0.5f ? 1 : -1;
            _unstuckBiasTimer = UnityEngine.Random.Range(0.35f, 0.8f);
        }

        Vector3 side = Vector3.Cross(Vector3.up, desiredDir).normalized * _unstuckBiasSign;
        Vector3 candidate = (side + desiredDir * 0.3f).normalized;

        Vector3 rayStart = now + Vector3.up * 0.45f;
        const float probe = 1.05f;
        int mask = obstructionLayer.value != 0 ? obstructionLayer.value : ~playerLayer.value;
        if (Physics.Raycast(rayStart, candidate, probe, mask))
        {
            Vector3 opposite = (-side + desiredDir * 0.3f).normalized;
            if (!Physics.Raycast(rayStart, opposite, probe, mask))
                candidate = opposite;
            else
                candidate = (-desiredDir + side * 0.15f).normalized;
        }

        return candidate;
    }

    public void FaceTarget()
    {
        if (detectedTarget == null) return;
        Vector3 dir = detectedTarget.transform.position - botKCC.Position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, Quaternion.LookRotation(dir), Runner.DeltaTime * 15f));
    }

    public void StopMovement()
    {
        botKCC.Move(Vector3.zero, 0f);
    }

    public void ApplyStrafe()
    {
        _strafeChangeTimer -= Runner.DeltaTime;
        if (_strafeChangeTimer <= 0f)
        {
            _strafeDir = UnityEngine.Random.value > 0.5f ? 1 : -1;
            _strafeChangeTimer = UnityEngine.Random.Range(1f, 2f);
        }

        Vector3 right = botKCC.TransformRotation * Vector3.right;
        botKCC.Move(right * _strafeDir * 0.35f * speed * Runner.DeltaTime, 0f);
    }

    /// <summary>Reads other bot playable type (local changer), not networked PlayableState string.</summary>
    public bool IsOpponentExecutingMeleeStrike()
    {
        if (!HasStateAuthority || detectedTarget == null || !IsTargetValid()) return false;

        BotPlayables oppPb = detectedTarget.GetComponent<BotPlayables>();
        BotAnimationPlayable oppState = oppPb != null ? oppPb.changer?.CurrentState : null;
        if (oppState == null) return false;

        string n = oppState.GetType().Name;
        return n.IndexOf("Attack", StringComparison.OrdinalIgnoreCase) >= 0
            || n.IndexOf("Punch", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public bool TryEvadeEnemyMeleeStrike(float maxProximityFromTarget)
    {
        if (!HasStateAuthority || Runner == null) return false;

        float dist = DistanceToTarget();
        if (dist > maxProximityFromTarget || float.IsPositiveInfinity(dist)) return false;
        if (!IsOpponentExecutingMeleeStrike()) return false;

        PerformMeleeEvadeStep();
        return true;
    }

    public void PerformMeleeEvadeStep()
    {
        if (detectedTarget == null || !HasStateAuthority) return;

        _strafeChangeTimer -= Runner.DeltaTime;

        Vector3 origin = botKCC.Position;

        Vector3 away = origin - detectedTarget.transform.position;
        away.y = 0f;
        float sqrAway = Mathf.Max(away.sqrMagnitude, 4e-4f);
        away.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, away).normalized;

        Vector3 dodge = (-away * 0.78f + right * (_strafeDir * 0.62f)).normalized;
        botKCC.Move(dodge * speed * 1.25f * Runner.DeltaTime, 0f);

        Vector3 toTarget = detectedTarget.transform.position - origin;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude > 0.0009f)
        {
            Quaternion look = Quaternion.LookRotation(toTarget.normalized);
            botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation, look, Mathf.Clamp01(Runner.DeltaTime * 12f)));
        }
    }

    public bool CanInitiateMeleeAttack()
    {
        if (!HasStateAuthority)
            return true;

        return Runner != null && Runner.Tick >= _nextMeleeAggressionTick;
    }

    public void RegisterMeleeAttackCommitted()
    {
        if (!HasStateAuthority || Runner == null || botData == null) return;

        int jitter = Mathf.Abs((botData.BotIndex ^ 59) % 13);
        _nextMeleeAggressionTick = Runner.Tick + 5 + jitter;
    }

    public bool HasLineOfSightToTarget()
    {
        if (detectedTarget == null || !IsTargetValid()) return false;

        Vector3 origin = botKCC.Position + Vector3.up * 0.5f;
        Vector3 target = detectedTarget.transform.position + Vector3.up * 0.5f;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;
        if (dist <= 0.01f) return true;

        dir /= dist;
        int mask = obstructionLayer.value != 0 ? obstructionLayer.value : ~playerLayer.value;
        return !Physics.Raycast(origin, dir, dist, mask);
    }

    public bool HasRifleAmmo()
    {
        var inv = botData.Inventory;
        if (inv.SecondaryWeapon == null || !inv.SecondaryWeapon.IsRifle) return false;
        return inv.RifleMagazine > 0 || inv.SecondaryWeapon.Supplies > 0;
    }

    public bool HasBowAmmo()
    {
        var inv = botData.Inventory;
        if (inv.SecondaryWeapon == null || inv.SecondaryWeapon.IsRifle) return false;
        return inv.BowMagazine > 0 || inv.SecondaryWeapon.Supplies > 0;
    }

    public void EvaluateCombatLoadout()
    {
        var inv = botData.Inventory;
        if (inv == null) return;

        // No target: prefer having a weapon equipped over bare hands.
        if (detectedTarget == null || !IsTargetValid())
        {
            string secId = NormalizeWeaponId(inv.GetSecondaryWeaponID());
            if (inv.SecondaryWeapon != null && ((secId == "003" && HasRifleAmmo()) || (secId == "004" && HasBowAmmo())))
                inv.SwitchToSecondary();
            else if (inv.PrimaryWeapon != null)
                inv.SwitchToPrimary();
            else
                inv.SwitchToHands();
            return;
        }

        float dist = DistanceToTarget();
        string secWeaponId = NormalizeWeaponId(inv.GetSecondaryWeaponID());
        bool hasRanged =
            inv.SecondaryWeapon != null &&
            ((secWeaponId == "003" && HasRifleAmmo()) ||
             (secWeaponId == "004" && HasBowAmmo()));

        bool hasMelee = inv.PrimaryWeapon != null;
        float meleeSwitchDistance = meleeRange + 0.8f; // ~2.3m — switch to melee only when target is right on top of us

        // Prefer ranged whenever the target isn't in immediate melee range.
        // Players don't pull out a sword when they have a loaded rifle and the enemy is 5m away.
        if (hasRanged && dist > meleeSwitchDistance && HasLineOfSightToTarget())
            inv.SwitchToSecondary();
        else if (hasMelee)
            inv.SwitchToPrimary();
        else if (hasRanged)
            inv.SwitchToSecondary(); // last resort — point-blank shot beats fists
        else
            inv.SwitchToHands();
    }

    public void MaintainRangedSpacing()
    {
        if (detectedTarget == null) return;

        float dist = DistanceToTarget();
        float minRanged = meleeRange + 1.5f;     // ~3m — only back away when target is very close
        float maxRanged = rangedEngageDistance;  // ~15m — close in if too far
        bool usingBow = botData != null && botData.Inventory != null
            && botData.Inventory.SecondaryWeapon != null
            && !botData.Inventory.SecondaryWeapon.IsRifle;
        float minShootRange = usingBow ? minBowRange : minRifleRange;

        Vector3 awayFromTarget = botKCC.Position - detectedTarget.transform.position;
        awayFromTarget.y = 0f;
        if (awayFromTarget.sqrMagnitude > 0.01f) awayFromTarget.Normalize();
        else awayFromTarget = botKCC.TransformRotation * Vector3.back;

        if (dist < minRanged)
        {
            // Way too close — direct back-away so the bot doesn't stand inside the target.
            botKCC.Move(awayFromTarget * speed * Runner.DeltaTime, 0f);
            botKCC.SetLookRotation(Quaternion.Slerp(botKCC.TransformRotation,
                Quaternion.LookRotation(awayFromTarget), Runner.DeltaTime * 12f));
        }
        else if (dist > maxRanged)
        {
            MoveToTarget();
        }
        else if (!HasLineOfSightToTarget())
        {
            // Has range but no LOS (behind cover) — reposition toward the target to find a clear angle.
            FaceTarget();
            NavigateTo(detectedTarget.transform.position);
        }
        else if (dist < minShootRange)
        {
            // Has LOS but target is closer than the minimum fire range — back away via NavMesh
            // until the bot reaches a position where CanRifleShoot() / CanBowShoot() will pass.
            Vector3 backTarget = detectedTarget.transform.position + awayFromTarget * (minShootRange + 1f);
            NavigateTo(backTarget);
        }
        else
        {
            // In range, has LOS — just face the target. CanRifleShoot/CanBowShoot should pass
            // next tick so the state machine will transition out on its own.
            FaceTarget();
        }
    }

    //  ===================== COMBAT RANGE CHECKS =====================

    public bool CanPunch()
    {
        if (detectedTarget == null || !IsTargetValid()) return false;
        return Vector3.Distance(botKCC.Position, detectedTarget.transform.position) <= meleeRange - 0.3f;
    }

    public bool CanSwordAttack()
    {
        if (detectedTarget == null || !IsTargetValid()) return false;
        return Vector3.Distance(botKCC.Position, detectedTarget.transform.position) <= meleeRange;
    }

    public bool CanSpearAttack()
    {
        if (detectedTarget == null || !IsTargetValid()) return false;
        return Vector3.Distance(botKCC.Position, detectedTarget.transform.position) <= spearRange;
    }

    public bool CanRifleShoot()
    {
        if (detectedTarget == null || !IsTargetValid()) return false;
        float dist = Vector3.Distance(botKCC.Position, detectedTarget.transform.position);
        return dist >= minRifleRange && HasRifleAmmo() && HasLineOfSightToTarget();
    }

    public bool CanBowShoot()
    {
        if (detectedTarget == null || !IsTargetValid()) return false;
        float dist = Vector3.Distance(botKCC.Position, detectedTarget.transform.position);
        return dist >= minBowRange && HasBowAmmo() && HasLineOfSightToTarget();
    }

    public bool IsInRangedRange()
    {
        if (detectedTarget == null) return false;
        return Vector3.Distance(botKCC.Position, detectedTarget.transform.position) >= rangedEngageDistance;
    }

    public bool IsTargetTooClose(float threshold = 5f)
    {
        return DistanceToTarget() < threshold;
    }

    public float DistanceToTarget()
    {
        if (detectedTarget == null) return float.MaxValue;
        return Vector3.Distance(botKCC.Position, detectedTarget.transform.position);
    }

    public Vector3 GetTargetAimPosition()
    {
        if (detectedTarget == null) return botKCC.Position + transform.forward * 10f;
        return detectedTarget.transform.position + Vector3.up * 1.2f;
    }

    public bool IsMovingHorizontally(float threshold = 0.1f)
    {
        Vector3 horizontal = new Vector3(botKCC.RealVelocity.x, 0f, botKCC.RealVelocity.z);
        return horizontal.sqrMagnitude > threshold * threshold;
    }

    //  ===================== LOOT SCANNING =====================

    public bool ShouldScanLoot() => LootScanTimer.Expired(Runner);

    public void ResetLootScanTimer()
    {
        LootScanTimer = TickTimer.CreateFromSeconds(Runner, 3f);
    }

    public void ScanForLoot()
    {
        _lootTarget = null;
        _lootCrate = null;
        _lootPrimary = null;
        _lootSecondary = null;

        var inv = botData.Inventory;
        int mask = itemLayer.value == 0 ? Physics.AllLayers : itemLayer.value;
        Collider[] hits = Physics.OverlapSphere(botKCC.Position, lootScanRadius, mask);

        int bestPriority = -1;
        float bestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            if (_lootBlacklistTimer > 0f && hit.transform.GetInstanceID() == _lootBlacklistInstanceId)
                continue;

            float d = Vector3.Distance(botKCC.Position, hit.transform.position);

            var crate = hit.GetComponent<CrateController>();
            if (crate != null)
            {
                foreach (var kv in crate.Weapons)
                {
                    int prio = GetLootPriority(kv.Key.ToString(), inv);
                    if (prio < 0) continue;
                    if (prio > bestPriority || (prio == bestPriority && d < bestDist))
                    {
                        bestPriority = prio;
                        bestDist = d;
                        _lootTarget = hit.transform;
                        _lootCrate = crate;
                        _lootPrimary = null;
                        _lootSecondary = null;
                    }
                }
                continue;
            }

            var pri = hit.GetComponent<PrimaryWeaponItem>();
            if (pri != null && !pri.IsPickedUp)
            {
                int prio = GetLootPriority(pri.WeaponID, inv);
                if (prio < 0) continue;
                if (prio > bestPriority || (prio == bestPriority && d < bestDist))
                {
                    bestPriority = prio;
                    bestDist = d;
                    _lootTarget = hit.transform;
                    _lootCrate = null;
                    _lootPrimary = pri;
                    _lootSecondary = null;
                }
                continue;
            }

            // Skip secondary weapons with no ammo loaded — a depleted weapon on the ground is
            // worthless and is likely one this bot (or another) already emptied. If a player
            // later picks it up, reloads it, and drops it, Supplies will be > 0 again and the
            // bot will be able to target it on the next scan.
            var sec = hit.GetComponent<SecondaryWeaponItem>();
            if (sec != null && !sec.IsPickedUp && sec.Supplies > 0)
            {
                int prio = GetLootPriority(sec.WeaponID, inv);
                if (prio < 0) continue;
                if (prio > bestPriority || (prio == bestPriority && d < bestDist))
                {
                    bestPriority = prio;
                    bestDist = d;
                    _lootTarget = hit.transform;
                    _lootCrate = null;
                    _lootPrimary = null;
                    _lootSecondary = sec;
                }
            }
        }
    }

    public bool HasLootTarget() => _lootTarget != null;

    public bool TryRunLootBehaviour()
    {
        if (_lootBlacklistTimer > 0f)
            _lootBlacklistTimer = Mathf.Max(0f, _lootBlacklistTimer - Runner.DeltaTime);

        if (HasLootTarget())
        {
            if (HasLootTargetBecomeUnreachable())
            {
                AbandonCurrentLootTarget();
                ResetLootScanTimer();
                return false;
            }

            NavigateToLootTarget();
            if (IsAtLootTarget())
            {
                TryPickupLoot();
                ResetLootScanTimer();
            }
            return true;
        }

        if (ShouldScanLoot())
        {
            ResetLootScanTimer();
            ScanForLoot();
            if (HasLootTarget())
                return true;
        }

        return false;
    }

    public void NavigateToLootTarget()
    {
        if (_lootTarget == null) return;
        NavigateTo(_lootTarget.position);
    }

    public bool IsAtLootTarget()
    {
        if (_lootTarget == null) return false;
        return Vector3.Distance(botKCC.Position, _lootTarget.position) <= 1.5f;
    }

    public void TryPickupLoot()
    {
        if (_lootTarget == null) return;

        var inv = botData.Inventory;

        if (_lootCrate != null)
        {
            string bestKey = null;
            int bestPrio = -1;
            foreach (var kv in _lootCrate.Weapons)
            {
                int prio = GetLootPriority(kv.Key.ToString(), inv);
                if (prio > bestPrio)
                {
                    bestPrio = prio;
                    bestKey = kv.Key.ToString();
                }
            }
            if (bestKey != null)
            {
                _lootCrate.PickupItemForBot(bestKey, botData);
                if (bestKey == "001" || bestKey == "002")
                {
                    if (inv.WeaponIndex == 1)
                        inv.SwitchToPrimary();
                }
                else if (bestKey == "003" || bestKey == "004")
                {
                    inv.SwitchToSecondary();
                }
            }
        }
        else if (_lootPrimary != null && !_lootPrimary.IsPickedUp)
        {
            _lootPrimary.InitializeItem(botData.Object, true);
            if (inv.WeaponIndex == 1)
                inv.SwitchToPrimary();
        }
        else if (_lootSecondary != null && !_lootSecondary.IsPickedUp)
        {
            _lootSecondary.InitializeItem(botData.Object, _lootSecondary.Supplies, true);
            inv.SwitchToSecondary();
        }

        _lootTarget = null;
        _lootCrate = null;
        _lootPrimary = null;
        _lootSecondary = null;
        _lootNoProgressTimer = 0f;
        _lootLastDistance = float.MaxValue;
    }

    private bool HasLootTargetBecomeUnreachable()
    {
        if (_lootTarget == null || Runner == null)
            return false;

        float dist = Vector3.Distance(botKCC.Position, _lootTarget.position);
        float delta = _lootLastDistance - dist;

        if (dist > 1.6f && delta < 0.03f)
            _lootNoProgressTimer += Runner.DeltaTime;
        else
            _lootNoProgressTimer = Mathf.Max(0f, _lootNoProgressTimer - Runner.DeltaTime * 0.5f);

        _lootLastDistance = dist;
        return _lootNoProgressTimer > 1.4f;
    }

    private void AbandonCurrentLootTarget()
    {
        if (_lootTarget != null)
        {
            _lootBlacklistInstanceId = _lootTarget.GetInstanceID();
            _lootBlacklistTimer = 2.5f;
        }

        _lootTarget = null;
        _lootCrate = null;
        _lootPrimary = null;
        _lootSecondary = null;
        _lootNoProgressTimer = 0f;
        _lootLastDistance = float.MaxValue;
    }

    private int GetLootPriority(string itemKey, BotInventory inv)
    {
        itemKey = NormalizeWeaponId(itemKey);
        string secId = NormalizeWeaponId(inv.GetSecondaryWeaponID());
        switch (itemKey)
        {
            // Allow swap only when bot has no secondary OR current secondary is completely out of ammo.
            case "003": return (inv.SecondaryWeapon == null || !SecondaryHasAmmo(inv)) ? 10 : -1;
            case "004": return (inv.SecondaryWeapon == null || !SecondaryHasAmmo(inv)) ? 9 : -1;
            case "001": return inv.PrimaryWeapon == null ? 6 : -1;
            case "002": return inv.PrimaryWeapon == null ? 5 : -1;
            case "005": return secId == "003" && inv.RifleMagazine < 90 ? 4 : -1;
            case "006": return secId == "004" && inv.BowMagazine < 60 ? 4 : -1;
            case "007": return inv.Armor == null ? 3 : (inv.Armor.Supplies < 50 ? 2 : -1);
            case "008": return inv.HealCount < 4 ? 1 : -1;
            case "009": return inv.RepairCount < 4 ? 1 : -1;
            case "010": return inv.TrapCount < 4 ? 0 : -1;
            default:    return -1;
        }
    }

    private bool SecondaryHasAmmo(BotInventory inv)
    {
        if (inv.SecondaryWeapon == null) return false;
        return inv.SecondaryWeapon.Supplies > 0 || inv.RifleMagazine > 0 || inv.BowMagazine > 0;
    }

    //  ===================== SAFE ZONE =====================

    public bool IsOutsideSafeZone()
    {
        if (SafeZoneServerController.Instance?.SafeZone == null) return false;

        var safeZone = SafeZoneServerController.Instance.SafeZone;
        float radius = safeZone.CurrentShrinkSize.x / 2f;
        Vector3 flatBot  = new Vector3(botKCC.Position.x, 0f, botKCC.Position.z);
        Vector3 flatZone = new Vector3(safeZone.transform.position.x, 0f, safeZone.transform.position.z);
        return Vector3.Distance(flatBot, flatZone) > radius;
    }

    public void NavigateToSafeZone()
    {
        if (SafeZoneServerController.Instance?.SafeZone == null) return;

        Vector3 safeCenter = SafeZoneServerController.Instance.SafeZone.transform.position;

        // If currently routing through an exit, keep going until we clear it.
        if (_exitTarget != null)
        {
            if (Vector3.Distance(botKCC.Position, _exitTarget.Position) < 2f)
                _exitTarget = null; // reached exit — continue straight to safe zone
            else
            {
                NavigateTo(_exitTarget.Position);
                return;
            }
        }

        // Try a direct path first. If the agent can't complete it (bot is inside a
        // disconnected building island), route through the nearest reachable exit.
        if (navAgent.isOnNavMesh)
        {
            NavMeshPath testPath = new NavMeshPath();
            bool directOk = navAgent.CalculatePath(safeCenter, testPath)
                            && testPath.status == NavMeshPathStatus.PathComplete;
            if (!directOk)
            {
                BuildingExit exit = BuildingExit.FindNearest(navAgent, botKCC.Position);
                if (exit != null)
                {
                    _exitTarget = exit;
                    NavigateTo(exit.Position);
                    return;
                }
            }
        }

        NavigateTo(safeCenter);
    }

    public Vector3 GetSafeZoneDirection()
    {
        if (SafeZoneServerController.Instance?.SafeZone == null) return Vector3.zero;
        Vector3 toCenter = SafeZoneServerController.Instance.SafeZone.transform.position - botKCC.Position;
        toCenter.y = 0f;
        return toCenter.normalized;
    }
}
