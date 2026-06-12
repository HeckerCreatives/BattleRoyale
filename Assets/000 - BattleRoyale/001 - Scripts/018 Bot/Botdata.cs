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

    [Header("AUDIO")]
    [Tooltip("Spatial AudioSource on the bot rig — pain grunt + knockdown grunt play here. Heard by every peer except the StateAuthority (mirrors the player pattern).")]
    [SerializeField] private AudioSource damageSource;
    [Tooltip("Random pain grunt picked on each hit (avoids repeating the previous clip).")]
    [SerializeField] private AudioClip[] gruntClips;
    [Tooltip("Knockdown / stagger grunt — plays when IsStagger flips true.")]
    [SerializeField] private AudioClip thumpGruntClip;

    [Header("WEAPON IMPACT SOUNDS (heard when this bot is hit)")]
    [Tooltip("Fist hits — bots have no fistSoundController, so the punch impact plays here.")]
    [SerializeField] private AudioClip punchHitClip;
    [Tooltip("Shared by sword + spear hits.")]
    [SerializeField] private AudioClip primaryHitClip;
    [Tooltip("Shared by rifle + bow hits.")]
    [SerializeField] private AudioClip secondaryHitClip;
    [Tooltip("Optional — trap damage impact. Silent if unassigned.")]
    [SerializeField] private AudioClip trapHitClip;

    [Header("FOOTSTEP AUDIO")]
    [Tooltip("Spatial AudioSource the per-surface clips play on. Mirrors PlayerPlayables.footstepSource.")]
    [SerializeField] private AudioSource footstepSource;
    [Tooltip("Foot transform — origin of the ground-check raycast.")]
    [SerializeField] private Transform groundDetector;
    [Tooltip("Layers the ground raycast collides against. Match PlayerMovementV2/PlayerPlayables groundMask.")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private AudioClip[] grassClip;
    [SerializeField] private AudioClip[] dirtClip;
    [SerializeField] private AudioClip[] stoneClip;
    [SerializeField] private AudioClip[] woodClip;

    [Header("DEBUGGER")]
    [SerializeField] private int bloodIndex;
    private AudioClip _previousGruntClip;
    private AudioClip _previousFootstepClip;

    [field: Header("NETWORK DEBUGGER")]
    [field: SerializeField][Networked] public int BotIndex { get; set; }
    [field: SerializeField][Networked] public string BotName { get; set; }
    [field: SerializeField][Networked] public float CurrentHealth { get; set; }
    [field: SerializeField][Networked] public bool IsDead { get; set; }
    [field: SerializeField][Networked] public int Hitted { get; set; }
    // Written in the same tick as Hitted++ — snapshot carries weapon category.
    [field: SerializeField][Networked] public int LastHitWeaponType { get; set; }
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
    // Server-authoritative ground material under the bot, computed each tick
    // by CheckGround(). Drives PlayFootstepSound's per-surface clip selection
    // on every peer (so spatial footstep audio matches the surface).
    [field: SerializeField][Networked] public Ground CurrentGround { get; set; }

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
        // Compute the surface under the bot once per tick on the authority,
        // then sync via the networked CurrentGround. Every peer reads the
        // synced value when its animation event fires PlayFootstepSound.
        if (HasStateAuthority) CheckGround();

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
                    if (!HasStateAuthority)
                    {
                        DamageIndicator();
                        HitSoundEffects();
                    }
                    break;

                case nameof(IsStagger):
                    // Knockdown grunt only on the leading edge (entering stagger),
                    // not when recovering.
                    if (!HasStateAuthority && IsStagger)
                        FallSoundEffects();
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

    // Pain grunt — mirrors PlayerHealthV2.HitSoundEffects. Spatial via the
    // bot's damageSource AudioSource, heard by every peer except the
    // StateAuthority (who guards the audio call out of the case above).
    private void HitSoundEffects()
    {
        if (damageSource == null) return;

        AudioClip clip = PickRandomClipNoRepeat(gruntClips, ref _previousGruntClip);
        if (clip != null) damageSource.PlayOneShot(clip);

        // Weapon-category impact sound — mirrors PlayerHealthV2.HitSoundEffects.
        AudioClip impact = LastHitWeaponType switch
        {
            HitWeaponType.Primary   => primaryHitClip,
            HitWeaponType.Secondary => secondaryHitClip,
            HitWeaponType.Trap      => trapHitClip,
            _                       => punchHitClip, // HitWeaponType.Punch
        };
        if (impact != null) damageSource.PlayOneShot(impact);
    }

    // Knockdown grunt — mirrors PlayerHealthV2.FallSoundEffects (single thump
    // clip). Called when IsStagger flips true.
    private void FallSoundEffects()
    {
        if (damageSource == null || thumpGruntClip == null) return;

        damageSource.PlayOneShot(thumpGruntClip);
    }

    // Server-only: raycast down from groundDetector and classify the surface
    // by tag, writing the [Networked] CurrentGround. Every peer reads the
    // synced value when its animation event calls PlayFootstepSound.
    // Mirrors PlayerPlayables.CheckGround except it uses Object.StateAuthority
    // for lag compensation (bots are server-driven, no input authority).
    public void CheckGround()
    {
        if (groundDetector == null) return;

        if (Runner.LagCompensation.Raycast(groundDetector.position, Vector3.down, 10f,
                                           Object.StateAuthority, out LagCompensatedHit hit,
                                           groundMask, HitOptions.IncludePhysX))
        {
            if (hit.GameObject == null) return;

            GameObject g = hit.GameObject;
            if (g.CompareTag("BattleAreaStage") || g.CompareTag("WaitingAreaStage")) CurrentGround = Ground.TERRAIN;
            else if (g.CompareTag("Stone")) CurrentGround = Ground.STONE;
            else if (g.CompareTag("Dirt"))  CurrentGround = Ground.DIRT;
            else if (g.CompareTag("Wood"))  CurrentGround = Ground.WOOD;
            else if (g.CompareTag("Grass")) CurrentGround = Ground.GRASS;
        }
    }

    // Per-surface footstep — invoked from Animation Events on the bot's
    // run/walk clips. Mirrors PlayerPlayables.PlayFootstepSound: the
    // StateAuthority skips audio (server runs no audio), every other peer
    // plays the clip spatially via footstepSource.
    public void PlayFootstepSound()
    {
        if (HasStateAuthority) return;
        if (footstepSource == null) return;

        AudioClip[] clips = null;
        switch (CurrentGround)
        {
            case Ground.DIRT:  clips = dirtClip;  break;
            case Ground.STONE: clips = stoneClip; break;
            case Ground.WOOD:  clips = woodClip;  break;
            case Ground.GRASS: clips = grassClip; break;
            // Ground.TERRAIN is intentionally silent — matches the player
            // behavior in PlayerPlayables.PlayFootstepSound.
        }

        AudioClip clip = PickRandomClipNoRepeat(clips, ref _previousFootstepClip);
        if (clip == null) return;

        footstepSource.PlayOneShot(clip);
    }

    // Shared random-pick-with-no-immediate-repeat. Tries up to 3 times to
    // avoid the previous clip in the same array (no-op if the array has only
    // one entry). Tracks per-category via the ref parameter so grunts and
    // footsteps each have their own "previous" memory.
    private AudioClip PickRandomClipNoRepeat(AudioClip[] clips, ref AudioClip previous)
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        AudioClip pick = null;
        for (int i = 0; i < 3; i++)
        {
            pick = clips[UnityEngine.Random.Range(0, clips.Length)];
            if (pick != previous) break;
        }
        previous = pick;
        return pick;
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

    public void ApplyDamage(float damage, string killer, NetworkObject nobject, int weaponType = HitWeaponType.Punch)
    {
        if (IsDead) return;

        LastHitWeaponType = weaponType;
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
                    hitObj.GetComponent<PlayerHealthV2>()?.ApplyDamage(damage, BotName, Object, HitWeaponType.Secondary);
                else if (hitObj.CompareTag("Bot"))
                    hitObj.GetComponent<Botdata>()?.ApplyDamage(damage, BotName, Object, HitWeaponType.Secondary);
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
                    hitObj.GetComponent<PlayerHealthV2>()?.ApplyDamage(damage, BotName, Object, HitWeaponType.Secondary);
                else if (hitObj.CompareTag("Bot"))
                    hitObj.GetComponent<Botdata>()?.ApplyDamage(damage, BotName, Object, HitWeaponType.Secondary);
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
