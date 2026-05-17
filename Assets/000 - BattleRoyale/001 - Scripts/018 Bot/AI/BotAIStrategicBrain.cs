using Fusion;

using UnityEngine;



/// <summary>

/// Operational / strategic planner for bots (BR-style goals).

/// Owned by <see cref="BotMovementController"/> — does not tick animations or combat combos.

/// </summary>

public sealed class BotAIStrategicBrain

{

    private readonly BotMovementController _locomotion;



    private BotStrategicGoal _goal = BotStrategicGoal.HuntPlayers;

    private Vector3 _focusXZ;

    private Vector3 _watchXZ;

    private int _nextPlanTick;

    private int _campHoldUntilTick;

    private float _personalityCamp;

    private float _personalityHuntAggro;



    public BotStrategicGoal CurrentGoal => _goal;



    public BotAIStrategicBrain(BotMovementController locomotion)

    {

        _locomotion = locomotion;

    }



    /// <summary>Called from <see cref="BotMovementController.Spawned"/> for everyone; timer init only meaningful on authority.</summary>

    public void Bootstrap(int spawnTickHint)

    {

        int idx = Mathf.Max(0, _locomotion.AIBotdata != null ? _locomotion.AIBotdata.BotIndex : 0);

        _personalityCamp = (Mathf.Abs(idx * 17 + 23) % 100) / 100f;

        _personalityHuntAggro = (Mathf.Abs(idx * 31 + 11) % 100) / 100f;



        _nextPlanTick = spawnTickHint;

        _campHoldUntilTick = 0;

        _focusXZ = FlatXZ(_locomotion.AIMotorPosition);

        _watchXZ = _focusXZ;

    }



    public void TickReplan()

    {

        if (!_locomotion.HasStateAuthority || _locomotion.Runner == null || _locomotion.AIBotdata == null)

            return;



        BotAIStrategicTuning t = BotAIStrategicTuning.Resolve(_locomotion.StrategicAiPolicy);

        NetworkRunner runner = _locomotion.Runner;



        if (runner.Tick < _nextPlanTick)

            return;



        Botdata botData = _locomotion.AIBotdata;



        if (MultiplayerServerManager.Instance != null && MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA)

        {

            _nextPlanTick = runner.Tick + t.NonArenaPlanIntervalTicks;

            return;

        }



        if (_goal == BotStrategicGoal.HoldCamp && runner.Tick < _campHoldUntilTick)

        {

            _nextPlanTick = runner.Tick + t.CampHoldSkipReplanTicks;

            return;

        }



        _nextPlanTick = runner.Tick + t.BasePlanIntervalTicks + Mathf.Abs(botData.BotIndex * 5) % t.PlanIntervalIndexJitterMod;



        Vector3 myFlat = FlatXZ(_locomotion.AIMotorPosition);

        TryGetFightCentroid(botData.gameObject, myFlat, out Vector3 centroid, out int popCount);



        BotInventory inv = botData.Inventory;



        bool lowHpWantsHeal = botData.CurrentHealth < t.LowHpRecoverThreshold && inv != null && inv.HealCount > 0;

        if (lowHpWantsHeal)

        {

            _goal = BotStrategicGoal.Recover;

            PickDisengageFocus(botData.BotIndex, myFlat, centroid, ref _focusXZ, t);

            _watchXZ = centroid;

            ClampFocusIntoSafeZone(ref _focusXZ, t);

            return;

        }



        if (NeedsLootPressure(inv))

        {

            _goal = BotStrategicGoal.AcquireLoot;

            PickLootRoamFocus(myFlat, centroid, ref _focusXZ, out _watchXZ, t);

            ClampFocusIntoSafeZone(ref _focusXZ, t);

            return;

        }



        if (botData.DamageBy != null && !botData.DamageAwareness.Expired(runner))

        {

            _goal = BotStrategicGoal.HuntPlayers;

            Vector3 atk = botData.DamageBy.transform.position;

            _focusXZ = new Vector3(atk.x, 0f, atk.z);

            _watchXZ = _focusXZ;

            ClampFocusIntoSafeZone(ref _focusXZ, t);

            return;

        }



        bool zoneClosing = SafeZoneServerController.Instance != null

            && SafeZoneServerController.Instance.CurrentSafeZoneState == SafeZoneState.SHRINK;



        float campRoll = Random.value;

        float campChance = t.CampChanceBase + _personalityCamp * t.CampChancePersonalityScale;

        if (zoneClosing)

            campChance *= t.ZoneClosingCampChanceMultiplier;



        if (popCount >= t.MinActorsToConsiderCamp && campRoll < campChance)

        {

            _goal = BotStrategicGoal.HoldCamp;

            PickRingCampPoint(botData.BotIndex, myFlat, centroid, ref _focusXZ, t);

            _watchXZ = centroid;

            _campHoldUntilTick = runner.Tick + t.CampHoldDurationBaseTicks

                + Mathf.Abs(botData.BotIndex * 7) % t.CampHoldDurationIndexJitterMod;

            ClampFocusIntoSafeZone(ref _focusXZ, t);

            return;

        }



        _goal = BotStrategicGoal.HuntPlayers;

        float huntBlend = t.HuntBlendBase + _personalityHuntAggro * t.HuntBlendPersonalityScale;

        if (zoneClosing)

            huntBlend += t.ZoneClosingHuntBlendAdd;



        Vector3 centroidFlat = FlatXZ(centroid);

        Vector3 toward = Vector3.Lerp(myFlat, centroidFlat, Mathf.Clamp01(huntBlend));

        float n = Mathf.Max(0f, t.HuntWaypointNoiseHalfExtent);

        Vector3 noise = new Vector3(Random.Range(-n, n), 0f, Random.Range(-n, n));

        _focusXZ = toward + noise;

        _watchXZ = centroidFlat;

        ClampFocusIntoSafeZone(ref _focusXZ, t);



        if (Random.value < t.RotateAfterHuntChance)

            _goal = BotStrategicGoal.RotatePosition;

    }



    /// <summary>Directed roam when no melee chase is active.</summary>

    public bool TryDriveExplorationMovement()

    {

        if (!_locomotion.HasStateAuthority || _locomotion.Runner == null || _locomotion.AIBotdata == null)

            return false;



        if (MultiplayerServerManager.Instance != null && MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA)

            return false;



        BotAIStrategicTuning t = BotAIStrategicTuning.Resolve(_locomotion.StrategicAiPolicy);

        Vector3 myFlat = FlatXZ(_locomotion.AIMotorPosition);



        switch (_goal)

        {

            case BotStrategicGoal.AcquireLoot:

            case BotStrategicGoal.Recover:

                if (_locomotion.TryRunLootBehaviour())

                    return true;

                _locomotion.NavigateFlatXZ(_focusXZ);

                return true;



            case BotStrategicGoal.HuntPlayers:

            case BotStrategicGoal.RotatePosition:

                _locomotion.NavigateFlatXZ(_focusXZ);

                return true;



            case BotStrategicGoal.HoldCamp:

            {

                float d = Vector2.Distance(new Vector2(myFlat.x, myFlat.z), new Vector2(_focusXZ.x, _focusXZ.z));

                if (d > t.CampArrivalDistance)

                    _locomotion.NavigateFlatXZ(_focusXZ);

                else

                {

                    _locomotion.FacePositionOnPlane(_watchXZ);

                    _locomotion.ApplyStrafePublic();

                }

                return true;

            }



            default:

                return false;

        }

    }



    /// <summary>Idle animation branch: rotate view toward contested area while camping.</summary>

    public bool TryIdleHoldCampFace()

    {

        if (!_locomotion.HasStateAuthority || _locomotion.Runner == null)

            return false;

        if (_goal != BotStrategicGoal.HoldCamp)

            return false;

        if (_locomotion.detectedTarget != null)

            return false;



        _locomotion.FacePositionOnPlane(_watchXZ);

        return true;

    }



    // --------------------------------------------------------------------



    private static Vector3 FlatXZ(Vector3 world)

    {

        return new Vector3(world.x, 0f, world.z);

    }



    private static bool NeedsLootPressure(BotInventory inv)

    {

        if (inv == null) return false;



        bool noPrimary = inv.PrimaryWeapon == null;

        bool noSecondary = inv.SecondaryWeapon == null;

        if (noPrimary && noSecondary) return true;



        if (noPrimary && inv.SecondaryWeapon != null)

        {

            string sid = inv.GetSecondaryWeaponID();
            if (!string.IsNullOrEmpty(sid))
            {
                sid = sid.Trim();
                if (int.TryParse(sid, out int n))
                    sid = n.ToString("000");
            }

            if (sid == "003" && inv.RifleMagazine <= 0) return true;

            if (sid == "004" && inv.BowMagazine <= 0) return true;

        }



        return false;

    }



    private static void TryGetFightCentroid(GameObject self, Vector3 myFlat, out Vector3 centroid, out int count)

    {

        centroid = Vector3.zero;

        count = 0;



        PlayerJoinedController pj = PlayerJoinedController.Instance;

        if (pj == null)

        {

            centroid = myFlat;

            return;

        }



        foreach (var kv in pj.RemainingPlayers)

        {

            NetworkObject no = kv.Value;

            if (no == null) continue;

            PlayerHealthV2 ph = no.GetComponent<PlayerHealthV2>();

            if (ph != null && ph.IsDead) continue;



            centroid += FlatXZ(no.transform.position);

            count++;

        }



        foreach (var kv in pj.Bots)

        {

            NetworkObject no = kv.Value;

            if (no == null || no.gameObject == self) continue;

            Botdata bd = no.GetComponent<Botdata>();

            if (bd != null && bd.IsDead) continue;



            centroid += FlatXZ(no.transform.position);

            count++;

        }



        if (count == 0)

        {

            centroid = myFlat;

            return;

        }



        centroid /= count;

    }



    private static void PickLootRoamFocus(Vector3 myFlat, Vector3 centroid, ref Vector3 focusXZ, out Vector3 watchXZ, BotAIStrategicTuning t)

    {

        float j = Mathf.Max(0f, t.LootRoamJitterHalfExtent);

        Vector3 jitter = new Vector3(Random.Range(-j, j), 0f, Random.Range(-j, j));

        focusXZ = Vector3.Lerp(myFlat, FlatXZ(centroid), Mathf.Clamp01(t.LootRoamCentroidBlend)) + jitter;

        watchXZ = FlatXZ(centroid);

    }



    private static void PickDisengageFocus(int botIndex, Vector3 myFlat, Vector3 centroid, ref Vector3 focusXZ, BotAIStrategicTuning t)

    {

        Vector3 away = myFlat - FlatXZ(centroid);

        if (away.sqrMagnitude < t.RecoverDisengageMinAwaySqrMag)

            away = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));



        away.Normalize();

        focusXZ = myFlat + away * t.RecoverDisengageDistance;

        _ = botIndex;

    }



    private static void PickRingCampPoint(int botIndex, Vector3 myFlat, Vector3 centroid, ref Vector3 focusXZ, BotAIStrategicTuning t)

    {

        if (SafeZoneServerController.Instance?.SafeZone == null)

        {

            focusXZ = Vector3.Lerp(myFlat, FlatXZ(centroid), Mathf.Clamp01(t.CampFallbackCentroidBlend));

            return;

        }



        SafeZoneController sz = SafeZoneServerController.Instance.SafeZone;

        Vector3 c = FlatXZ(sz.transform.position);

        float ring = sz.CurrentShrinkSize.x * 0.5f * Mathf.Clamp(t.CampRingRadiusScale, 0f, 1f);



        Vector3 radial = myFlat - c;

        if (radial.sqrMagnitude < 0.5f)

            radial = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));

        radial.Normalize();



        Vector3 tang = Vector3.Cross(Vector3.up, radial);

        int woMod = Mathf.Max(2, t.CampTangentialWobbleIndexMod);

        int wobbleIdx = Mathf.Abs(botIndex * 13) % woMod;

        float wobble = (wobbleIdx - woMod / 2) * t.CampTangentialWobbleStep;

        focusXZ = c + radial * ring + tang * wobble;

    }



    private static void ClampFocusIntoSafeZone(ref Vector3 flatXZ, BotAIStrategicTuning t)

    {

        if (SafeZoneServerController.Instance?.SafeZone == null)

            return;



        SafeZoneController sz = SafeZoneServerController.Instance.SafeZone;

        Vector3 c = FlatXZ(sz.transform.position);

        float rad = sz.CurrentShrinkSize.x * 0.5f * Mathf.Clamp(t.FocusClampSafeRadiusScale, 0f, 1f);



        Vector3 d = flatXZ - c;

        d.y = 0f;

        float mag = d.magnitude;

        if (mag > rad && mag > 0.01f)

            flatXZ = c + d.normalized * rad;

    }

}

