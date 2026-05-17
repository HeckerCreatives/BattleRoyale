using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Mood
{
    JOMARIE, // DUMB BOT
    ALEX,    // SCARED BOT
    BIEN     // AGGRESSIVE AND SMART BOT
}

public class Botdata : NetworkBehaviour
{
    public static readonly List<Botdata> All = new();

    public BotInventory Inventory => inventory;

    //  ===================

    [SerializeField] private BotInventory inventory;
    [SerializeField] private NetworkObject trapObj;

    [Space]
    [SerializeField] private List<ParticleSystem> bloodParticles;
    [SerializeField] private ParticleSystem healParticles;
    [SerializeField] private ParticleSystem repairParticles;
    [SerializeField] private ArrowController[] localArrowPool;
    [SerializeField] private BulletController[] localBulletPool;

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private LayerMask projectileRaycastMask;
    [SerializeField] private float attackRadius;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    [Header("DEBUGGER")]
    [SerializeField] private int bloodIndex;

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField][Networked] public int BotIndex { get; set; }
    [field: SerializeField][Networked] public string BotName { get; set; }
    [field: SerializeField][Networked] public float CurrentHealth { get; set; }
    [field: SerializeField][Networked] public bool IsDead { get; set; }
    [field: SerializeField][Networked] public int Hitted { get; set; }
    [field: SerializeField][Networked] public bool IsHit { get; set; }
    [field: SerializeField][Networked] public bool IsStagger { get; set; }
    [field: SerializeField][Networked] public bool IsGettingUp { get; set; }
    [field: SerializeField][Networked] public int Healed { get; set; }
    [field: SerializeField][Networked] public int Repaired { get; set; }
    [field: SerializeField][Networked] public TickTimer DeadTimer { get; set; }
    [field: SerializeField][Networked] public TickTimer HealTimer { get; set; }
    [field: SerializeField][Networked] public TickTimer ArmorTimer { get; set; }
    [field: SerializeField][Networked] public TickTimer DamageAwareness { get; set; }
    [field: SerializeField][Networked] public NetworkObject DamageBy { get; set; }

    // Projectile replication (visual state for non-authority clients)
    [Networked] public int BulletFiredTick { get; set; }
    [Networked] public Vector3 BulletStart { get; set; }
    [Networked] public Vector3 BulletTarget { get; set; }
    [Networked] public int ArrowFiredTick { get; set; }
    [Networked] public Vector3 ArrowStart { get; set; }
    [Networked] public Vector3 ArrowTarget { get; set; }
    [Networked] public Vector3 HitKnockbackDir { get; set; }

    //  ======================

    private ChangeDetector _changeDetector;
    private BotPlayables _botPlayables;

    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> _projectileHits = new List<LagCompensatedHit>();

    private readonly HashSet<NetworkObject> hitEnemiesFirstFist = new();
    private readonly HashSet<NetworkObject> hitEnemiesSecondFist = new();

    private int _localArrowIndex;
    private int _lastArrowSpawnTick = -1;
    private int _localBulletIndex;
    private int _lastBulletSpawnTick = -1;

    //  ======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        _botPlayables = GetComponent<BotPlayables>();

        if (HasStateAuthority)
            CurrentHealth = 100f;

        All.Add(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    public override void FixedUpdateNetwork()
    {
        CircleDamage();

        if (IsDead && DeadTimer.Expired(Runner))
            Runner.Despawn(Object);
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Hitted):
                    if (!HasStateAuthority) DamageIndicator();
                    break;

                case nameof(Healed):
                    if (!HasStateAuthority) healParticles.Play();
                    break;

                case nameof(Repaired):
                    if (!HasStateAuthority) repairParticles.Play();
                    break;

                case nameof(BulletFiredTick):
                    if (!HasStateAuthority)
                        SpawnBulletTrail(BulletStart, BulletTarget);
                    break;

                case nameof(ArrowFiredTick):
                    if (!HasStateAuthority)
                        SpawnArrowTrail(ArrowStart, ArrowTarget);
                    break;
            }
        }
    }

    #region DAMAGE RECEIVED

    private void DamageIndicator()
    {
        if (bloodIndex >= bloodParticles.Count - 1)
            bloodIndex = 0;
        else
            bloodIndex++;

        bloodParticles[bloodIndex].Play();
    }

    private void CircleDamage()
    {
        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;
        if (!MultiplayerServerManager.Instance.DonePlayerBattlePositions) return;
        if (IsDead) return;

        float distanceFromCenter = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(SafeZoneServerController.Instance.SafeZone.transform.position.x, 0, SafeZoneServerController.Instance.SafeZone.transform.position.z));
        float radius = SafeZoneServerController.Instance.SafeZone.CurrentShrinkSize.x / 2f;

        if (distanceFromCenter > radius)
            CurrentHealth -= Runner.DeltaTime * ((SafeZoneServerController.Instance.SafeZone.ShrinkSizeIndex + 1) / 2f);

        if (CurrentHealth <= 0)
            HandleDeath("outside safe area", null, null);
    }

    public void FallDamage(float damage)
    {
        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;
        if (!MultiplayerServerManager.Instance.DonePlayerBattlePositions) return;
        if (IsDead) return;

        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
            HandleDeath("themselves", null, null);
    }

    public void ApplyDamage(float damage, string killer, NetworkObject nobject)
    {
        if (IsDead) return;

        Hitted++;
        DamageBy = nobject;
        DamageAwareness = TickTimer.CreateFromSeconds(Runner, 10f);

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;

        float remainingDamage = damage;

        if (inventory.Armor != null && inventory.Armor.Supplies > 0)
        {
            if (inventory.Armor.Supplies >= (int)remainingDamage)
            {
                inventory.Armor.Supplies -= (int)remainingDamage;
                remainingDamage = 0f;
            }
            else
            {
                remainingDamage -= inventory.Armor.Supplies;
                inventory.Armor.Supplies = 0;
            }
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - remainingDamage);

        if (CurrentHealth <= 0)
            HandleDeath(killer, nobject, (nobject != null && nobject.CompareTag("Player")) ? nobject.GetComponent<PlayerGameStats>() : null);
    }

    private void HandleDeath(string killer, NetworkObject killerObject, PlayerGameStats killerStats)
    {
        if (HasStateAuthority)
            IsDead = true;

        if (!IsDead) return;

        if (killerStats != null)
            killerStats.KillCount++;

        PlayerJoinedController.Instance?.RemoveBot(BotIndex);

        string notifMsg = killerObject == null
            ? $"{BotName} was killed {killer}"
            : $"{killer} KILLED {BotName}";

        KillNotifServerController.Instance.KillNotifController.RPC_ReceiveKillNotification(notifMsg);

        if (Inventory.PrimaryWeapon != null)  Inventory.PrimaryWeapon.DropWeapon();
        if (Inventory.SecondaryWeapon != null) Inventory.SecondaryWeapon.DropWeapon();
        if (Inventory.Armor != null)           Inventory.Armor.DropArmor();

        DeadTimer = TickTimer.CreateFromSeconds(Runner, 5f);
    }

    public void HealHealth()
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + 35f, 0f, 100f);
        inventory.HealCount -= 1;
        Healed++;
    }

    public void RepairArmor()
    {
        inventory.Armor.Supplies = (int)Mathf.Clamp(inventory.Armor.Supplies + 40f, 0f, 100f);
        inventory.RepairCount -= 1;
        Repaired++;
    }

    #endregion

    #region RANGED COMBAT

    public void FireBullet(Transform muzzlePoint, Vector3 targetPos)
    {
        if (!HasStateAuthority) return;
        if (BulletFiredTick == Runner.Tick) return;

        Vector3 muzzlePos = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 dir = (targetPos - muzzlePos).normalized;

        BulletStart = muzzlePos;
        BulletTarget = targetPos;

        LagCompensatedHit hit = new LagCompensatedHit();
        Vector3 rayStart = muzzlePos;
        int safetyLimit = 10;

        while (safetyLimit-- > 0)
        {
            if (!Runner.LagCompensation.Raycast(rayStart, dir, 999f, Object.StateAuthority, out hit, projectileRaycastMask, HitOptions.IncludePhysX))
                break;

            NetworkObject hitObj = hit.Hitbox?.Root.Object;
            if (hitObj == Object)
            {
                rayStart = hit.Point + dir * 0.5f;
                continue;
            }

            BulletTarget = hit.Point;

            if (hit.Hitbox != null && hitObj != null)
            {
                string tag = hit.Hitbox.tag;
                float damage = tag switch
                {
                    "Head"    => 60f,
                    "Body"    => 45f,
                    "Thigh"   => 35f,
                    "Shin"    => 30f,
                    "Foot"    => 25f,
                    "Arm"     => 40f,
                    "Forearm" => 30f,
                    _         => 0f
                };

                if (hitObj.CompareTag("Player"))
                    hitObj.GetComponent<PlayerHealthV2>()?.ApplyDamage(damage, BotName, Object);
                else if (hitObj.CompareTag("Bot"))
                    hitObj.GetComponent<Botdata>()?.ApplyDamage(damage, BotName, Object);
            }
            break;
        }

        if (inventory.SecondaryWeapon != null)
            inventory.SecondaryWeapon.Supplies = Mathf.Max(0, inventory.SecondaryWeapon.Supplies - 1);

        BulletFiredTick = Runner.Tick;
        SpawnBulletTrail(BulletStart, BulletTarget);
    }

    public void FireArrow(Transform muzzlePoint, Vector3 targetPos)
    {
        if (!HasStateAuthority) return;
        if (ArrowFiredTick == Runner.Tick) return;

        Vector3 arrowOrigin = muzzlePoint != null ? muzzlePoint.position : transform.position + Vector3.up * 1.5f;
        Vector3 dir = (targetPos - arrowOrigin).normalized;

        ArrowStart = arrowOrigin;
        ArrowTarget = targetPos;

        LagCompensatedHit hit = new LagCompensatedHit();
        Vector3 rayStart = arrowOrigin;
        int safetyLimit = 10;

        while (safetyLimit-- > 0)
        {
            if (!Runner.LagCompensation.Raycast(rayStart, dir, 999f, Object.StateAuthority, out hit, projectileRaycastMask, HitOptions.IncludePhysX))
                break;

            NetworkObject hitObj = hit.Hitbox?.Root.Object;
            if (hitObj == Object)
            {
                rayStart = hit.Point + dir * 0.5f;
                continue;
            }

            ArrowTarget = hit.Point;

            if (hit.Hitbox != null && hitObj != null)
            {
                string tag = hit.Hitbox.tag;
                float damage = tag switch
                {
                    "Head"    => 75f,
                    "Body"    => 55f,
                    "Thigh"   => 45f,
                    "Shin"    => 40f,
                    "Foot"    => 35f,
                    "Arm"     => 50f,
                    "Forearm" => 40f,
                    _         => 0f
                };

                if (hitObj.CompareTag("Player"))
                    hitObj.GetComponent<PlayerHealthV2>()?.ApplyDamage(damage, BotName, Object);
                else if (hitObj.CompareTag("Bot"))
                    hitObj.GetComponent<Botdata>()?.ApplyDamage(damage, BotName, Object);
            }
            break;
        }

        if (inventory.BowMagazine > 0)
            inventory.BowMagazine--;

        ArrowFiredTick = Runner.Tick;
        SpawnArrowTrail(ArrowStart, ArrowTarget);
    }

    private void SpawnBulletTrail(Vector3 start, Vector3 end)
    {
        int tick = Runner != null ? Runner.Tick : -1;
        if (tick == _lastBulletSpawnTick)
            return;
        _lastBulletSpawnTick = tick;

        if (localBulletPool == null || localBulletPool.Length == 0)
            return;

        var bullet = localBulletPool[_localBulletIndex];
        _localBulletIndex = (_localBulletIndex + 1) % localBulletPool.Length;
        bullet.FireFromPosition(start, end, false, true);
    }

    private void SpawnArrowTrail(Vector3 start, Vector3 end)
    {
        int tick = Runner != null ? Runner.Tick : -1;
        if (tick == _lastArrowSpawnTick)
            return;
        _lastArrowSpawnTick = tick;

        if (localArrowPool == null || localArrowPool.Length == 0)
            return;

        var arrow = localArrowPool[_localArrowIndex];
        _localArrowIndex = (_localArrowIndex + 1) % localArrowPool.Length;
        arrow.FireFromPosition(start, end, true);
    }

    #endregion

    #region SPAWN OBJECTS

    public void SpawnTrap()
    {
        Inventory.TrapCount -= 1;

        if (!HasStateAuthority) return;

        Runner.Spawn(trapObj, transform.position, Quaternion.identity, Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            obj.GetComponent<TrapWeaponController>().Initialize(BotName, transform.position, Vector3.zero);
        });
    }

    #endregion

    #region DAMAGE GIVEN

    public void PerformFirstAttack(bool isFinal = false)
    {
        int hitCount = Runner.LagCompensation.OverlapSphere(
            impactFirstFistPoint.position,
            attackRadius,
            Object.InputAuthority,
            hitsFirstFist,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsFirstFist[i].Hitbox;
            if (hitbox == null) continue;

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();
            if (hitObject == null || hitObject == Object) continue;

            if (hitObject.CompareTag("Bot"))
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();
                if (tempdata.IsStagger || tempdata.IsGettingUp || tempdata.IsDead) return;

                if (!hitEnemiesFirstFist.Contains(hitObject))
                {
                    hitEnemiesFirstFist.Add(hitObject);
                    float tempdamage = GetFistDamage(hitbox.tag);
                    if (isFinal) tempdata.IsStagger = true;
                    else tempdata.IsHit = true;
                    tempdata.ApplyDamage(tempdamage, BotName, Object);
                    _botPlayables?.RPC_PlayPunchHit();
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();
                if (tempplayables.healthV2.IsStagger || tempplayables.healthV2.IsGettingUp) return;

                if (!hitEnemiesFirstFist.Contains(hitObject))
                {
                    hitEnemiesFirstFist.Add(hitObject);
                    float tempdamage = GetFistDamage(hitbox.tag);
                    PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();
                    if (isFinal) healthV2.IsStagger = true;
                    healthV2.ApplyDamage(tempdamage, BotName, Object);
                    _botPlayables?.RPC_PlayPunchHit();
                }
            }
        }
    }

    public void PerformSecondAttack()
    {
        int hitCount = Runner.LagCompensation.OverlapSphere(
            impactSecondFistPoint.position,
            attackRadius,
            Object.InputAuthority,
            hitsSecondFist,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsSecondFist[i].Hitbox;
            if (hitbox == null) continue;

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();
            if (hitObject == null || hitObject == Object) continue;

            if (hitObject.CompareTag("Bot"))
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();
                if (tempdata.IsStagger || tempdata.IsGettingUp || tempdata.IsDead) return;

                if (!hitEnemiesSecondFist.Contains(hitObject))
                {
                    hitEnemiesSecondFist.Add(hitObject);
                    tempdata.IsHit = true;
                    tempdata.ApplyDamage(GetFistDamage(hitbox.tag), BotName, Object);
                    _botPlayables?.RPC_PlayPunchHit();
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();
                if (tempplayables.healthV2.IsStagger || tempplayables.healthV2.IsGettingUp) return;

                if (!hitEnemiesSecondFist.Contains(hitObject))
                {
                    hitEnemiesSecondFist.Add(hitObject);
                    hitObject.GetComponent<PlayerHealthV2>()?.ApplyDamage(GetFistDamage(hitbox.tag), BotName, Object);
                    _botPlayables?.RPC_PlayPunchHit();
                }
            }
        }
    }

    private float GetFistDamage(string bodyPartTag) => bodyPartTag switch
    {
        "Head"    => 30f,
        "Body"    => 25f,
        "Thigh"   => 20f,
        "Shin"    => 15f,
        "Foot"    => 10f,
        "Arm"     => 20f,
        "Forearm" => 15f,
        _         => 0f
    };

    public void ResetFirstAttack()  => hitEnemiesFirstFist.Clear();
    public void ResetSecondAttack() => hitEnemiesSecondFist.Clear();

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(impactFirstFistPoint.position, attackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(impactSecondFistPoint.position, attackRadius);
    }
}
