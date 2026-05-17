using UnityEngine;

/// <summary>
/// Designer-tunable knobs for strategic bot behaviour. Assign optional reference on <see cref="BotMovementController"/>.
/// When unassigned, <see cref="BotAIStrategicTuning.EmbeddedDefaults"/> matches the original hard-coded values.
/// </summary>
[CreateAssetMenu(fileName = "BotAI Strategic Policy", menuName = "BattleRoyale/Bot AI/Strategic Policy")]
public sealed class BotAIPolicy : ScriptableObject
{
    [Header("Replan cadence (runner ticks)")]
    [Tooltip("How often goals are reconsidered outside arena phase.")]
    [SerializeField] private int nonArenaPlanIntervalTicks = 120;
    [Tooltip("While holding camp and hold timer not expired, replan at this cadence.")]
    [SerializeField] private int campHoldSkipReplanTicks = 20;
    [Tooltip("Base interval between full replans in arena.")]
    [SerializeField] private int basePlanIntervalTicks = 36;
    [Tooltip("Extra jitter: abs(BotIndex * 5) % this value is added to base interval.")]
    [SerializeField] private int planIntervalIndexJitterMod = 40;

    [Header("Recover")]
    [Tooltip("Below this HP and with heals in inventory, bot disengages toward recover goal.")]
    [SerializeField] private float lowHpRecoverThreshold = 38f;
    [Tooltip("How far (m) to move away from fight centroid when recovering.")]
    [SerializeField] private float recoverDisengageDistance = 14f;
    [Tooltip("If away vector is shorter than sqrt(this), pick a random direction.")]
    [SerializeField] private float recoverDisengageMinAwaySqrMag = 2f;

    [Header("Loot roam")]
    [SerializeField] private float lootRoamCentroidBlend = 0.22f;
    [SerializeField] private float lootRoamJitterHalfExtent = 26f;

    [Header("Camp")]
    [Tooltip("Minimum living actors (players + other bots) near centroid to consider camping.")]
    [SerializeField] private int minActorsToConsiderCamp = 2;
    [SerializeField] private float campChanceBase = 0.18f;
    [SerializeField] private float campChancePersonalityScale = 0.38f;
    [Tooltip("Multiplies camp roll chance while zone is shrinking.")]
    [SerializeField] private float zoneClosingCampChanceMultiplier = 0.4f;
    [Tooltip("Fallback blend toward centroid when safe zone unavailable.")]
    [SerializeField] private float campFallbackCentroidBlend = 0.35f;
    [Tooltip("Ring position as fraction of current shrink radius (inner placement).")]
    [SerializeField] private float campRingRadiusScale = 0.55f;
    [SerializeField] private int campHoldDurationBaseTicks = 72;
    [SerializeField] private int campHoldDurationIndexJitterMod = 100;
    [SerializeField] private int campTangentialWobbleIndexMod = 9;
    [SerializeField] private float campTangentialWobbleStep = 1.4f;
    [Tooltip("Navigate until within this distance of camp anchor, then strafe/watch.")]
    [SerializeField] private float campArrivalDistance = 5f;

    [Header("Hunt / roam")]
    [SerializeField] private float huntBlendBase = 0.32f;
    [SerializeField] private float huntBlendPersonalityScale = 0.42f;
    [Tooltip("Adds to centroid lerp blend while zone shrinking.")]
    [SerializeField] private float zoneClosingHuntBlendAdd = 0.14f;
    [SerializeField] private float huntWaypointNoiseHalfExtent = 18f;
    [Tooltip("After centroid blend goal, probability to rotate instead of pure hunt.")]
    [SerializeField] private float rotateAfterHuntChance = 0.22f;

    [Header("Safe zone clamp")]
    [SerializeField] private float focusClampSafeRadiusScale = 0.9f;

    internal BotAIStrategicTuning ToRuntime()
    {
        int planJitter = Mathf.Max(1, planIntervalIndexJitterMod);
        int campHoldJitter = Mathf.Max(1, campHoldDurationIndexJitterMod);
        int wobbleMod = Mathf.Max(1, campTangentialWobbleIndexMod);
        return new BotAIStrategicTuning(
            nonArenaPlanIntervalTicks,
            campHoldSkipReplanTicks,
            basePlanIntervalTicks,
            planJitter,
            lowHpRecoverThreshold,
            recoverDisengageDistance,
            recoverDisengageMinAwaySqrMag,
            lootRoamCentroidBlend,
            lootRoamJitterHalfExtent,
            Mathf.Max(1, minActorsToConsiderCamp),
            campChanceBase,
            campChancePersonalityScale,
            zoneClosingCampChanceMultiplier,
            campFallbackCentroidBlend,
            campRingRadiusScale,
            campHoldDurationBaseTicks,
            campHoldJitter,
            wobbleMod,
            campTangentialWobbleStep,
            campArrivalDistance,
            huntBlendBase,
            huntBlendPersonalityScale,
            zoneClosingHuntBlendAdd,
            huntWaypointNoiseHalfExtent,
            rotateAfterHuntChance,
            focusClampSafeRadiusScale);
    }
}

/// <summary>
/// Resolved snapshot used by <see cref="BotAIStrategicBrain"/> — no Unity object references at runtime planning.
/// </summary>
public readonly struct BotAIStrategicTuning
{
    public int NonArenaPlanIntervalTicks { get; }
    public int CampHoldSkipReplanTicks { get; }
    public int BasePlanIntervalTicks { get; }
    public int PlanIntervalIndexJitterMod { get; }
    public float LowHpRecoverThreshold { get; }
    public float RecoverDisengageDistance { get; }
    public float RecoverDisengageMinAwaySqrMag { get; }
    public float LootRoamCentroidBlend { get; }
    public float LootRoamJitterHalfExtent { get; }
    public int MinActorsToConsiderCamp { get; }
    public float CampChanceBase { get; }
    public float CampChancePersonalityScale { get; }
    public float ZoneClosingCampChanceMultiplier { get; }
    public float CampFallbackCentroidBlend { get; }
    public float CampRingRadiusScale { get; }
    public int CampHoldDurationBaseTicks { get; }
    public int CampHoldDurationIndexJitterMod { get; }
    public int CampTangentialWobbleIndexMod { get; }
    public float CampTangentialWobbleStep { get; }
    public float CampArrivalDistance { get; }
    public float HuntBlendBase { get; }
    public float HuntBlendPersonalityScale { get; }
    public float ZoneClosingHuntBlendAdd { get; }
    public float HuntWaypointNoiseHalfExtent { get; }
    public float RotateAfterHuntChance { get; }
    public float FocusClampSafeRadiusScale { get; }

    public BotAIStrategicTuning(
        int nonArenaPlanIntervalTicks,
        int campHoldSkipReplanTicks,
        int basePlanIntervalTicks,
        int planIntervalIndexJitterMod,
        float lowHpRecoverThreshold,
        float recoverDisengageDistance,
        float recoverDisengageMinAwaySqrMag,
        float lootRoamCentroidBlend,
        float lootRoamJitterHalfExtent,
        int minActorsToConsiderCamp,
        float campChanceBase,
        float campChancePersonalityScale,
        float zoneClosingCampChanceMultiplier,
        float campFallbackCentroidBlend,
        float campRingRadiusScale,
        int campHoldDurationBaseTicks,
        int campHoldDurationIndexJitterMod,
        int campTangentialWobbleIndexMod,
        float campTangentialWobbleStep,
        float campArrivalDistance,
        float huntBlendBase,
        float huntBlendPersonalityScale,
        float zoneClosingHuntBlendAdd,
        float huntWaypointNoiseHalfExtent,
        float rotateAfterHuntChance,
        float focusClampSafeRadiusScale)
    {
        NonArenaPlanIntervalTicks = nonArenaPlanIntervalTicks;
        CampHoldSkipReplanTicks = campHoldSkipReplanTicks;
        BasePlanIntervalTicks = basePlanIntervalTicks;
        PlanIntervalIndexJitterMod = planIntervalIndexJitterMod;
        LowHpRecoverThreshold = lowHpRecoverThreshold;
        RecoverDisengageDistance = recoverDisengageDistance;
        RecoverDisengageMinAwaySqrMag = recoverDisengageMinAwaySqrMag;
        LootRoamCentroidBlend = lootRoamCentroidBlend;
        LootRoamJitterHalfExtent = lootRoamJitterHalfExtent;
        MinActorsToConsiderCamp = minActorsToConsiderCamp;
        CampChanceBase = campChanceBase;
        CampChancePersonalityScale = campChancePersonalityScale;
        ZoneClosingCampChanceMultiplier = zoneClosingCampChanceMultiplier;
        CampFallbackCentroidBlend = campFallbackCentroidBlend;
        CampRingRadiusScale = campRingRadiusScale;
        CampHoldDurationBaseTicks = campHoldDurationBaseTicks;
        CampHoldDurationIndexJitterMod = campHoldDurationIndexJitterMod;
        CampTangentialWobbleIndexMod = campTangentialWobbleIndexMod;
        CampTangentialWobbleStep = campTangentialWobbleStep;
        CampArrivalDistance = campArrivalDistance;
        HuntBlendBase = huntBlendBase;
        HuntBlendPersonalityScale = huntBlendPersonalityScale;
        ZoneClosingHuntBlendAdd = zoneClosingHuntBlendAdd;
        HuntWaypointNoiseHalfExtent = huntWaypointNoiseHalfExtent;
        RotateAfterHuntChance = rotateAfterHuntChance;
        FocusClampSafeRadiusScale = focusClampSafeRadiusScale;
    }

    /// <summary>Original hard-coded behaviour when no <see cref="BotAIPolicy"/> is assigned.</summary>
    public static BotAIStrategicTuning EmbeddedDefaults { get; } = new BotAIStrategicTuning(
        nonArenaPlanIntervalTicks: 120,
        campHoldSkipReplanTicks: 20,
        basePlanIntervalTicks: 36,
        planIntervalIndexJitterMod: 40,
        lowHpRecoverThreshold: 38f,
        recoverDisengageDistance: 14f,
        recoverDisengageMinAwaySqrMag: 2f,
        lootRoamCentroidBlend: 0.22f,
        lootRoamJitterHalfExtent: 26f,
        minActorsToConsiderCamp: 2,
        campChanceBase: 0.18f,
        campChancePersonalityScale: 0.38f,
        zoneClosingCampChanceMultiplier: 0.4f,
        campFallbackCentroidBlend: 0.35f,
        campRingRadiusScale: 0.55f,
        campHoldDurationBaseTicks: 72,
        campHoldDurationIndexJitterMod: 100,
        campTangentialWobbleIndexMod: 9,
        campTangentialWobbleStep: 1.4f,
        campArrivalDistance: 5f,
        huntBlendBase: 0.32f,
        huntBlendPersonalityScale: 0.42f,
        zoneClosingHuntBlendAdd: 0.14f,
        huntWaypointNoiseHalfExtent: 18f,
        rotateAfterHuntChance: 0.22f,
        focusClampSafeRadiusScale: 0.9f);

    public static BotAIStrategicTuning Resolve(BotAIPolicy asset) =>
        asset != null ? asset.ToRuntime() : EmbeddedDefaults;
}
