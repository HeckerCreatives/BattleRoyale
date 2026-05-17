---
name: Bot AI Changes — Full Session Log
description: All bot AI fixes and features implemented across recent sessions. Read this before touching any bot script.
type: project
---

# Bot AI Changes — Full Session Log

## Architecture Overview

- **Movement**: `BotMovementController.cs` — NavMesh agent (direction only) + SimpleKCC (physical movement)
- **Animation/State**: `BotPlayables.cs` + `BotPlayableChanger.cs` — Playables API state machine
- **States**: `000 - BattleRoyale/001 - Scripts/018 Bot/000 - Locomotions/001 - SubState/`
- **NavMesh pattern**: `navAgent.updatePosition = false`, `navAgent.updateRotation = false`. KCC moves physically; `navAgent.nextPosition = botKCC.Position` syncs the agent each tick so `desiredVelocity` / `velocity` stay valid for pathfinding direction.
- **Timing**: All animation timers use `botPlayables.TickRateAnimation` = `Runner.Tick * Runner.DeltaTime`

---

## Session 1 Changes (Upper Body / IK / Ranged Combat)

### BotPlayables.cs
- **Removed** entire upper body layer (AvatarMask, layered mixer), IK job chain (`LookAtJobBoneIK`), lower-body override system
- **Graph is now**: `basicMovement.mixerPlayable → finalMixer → AnimationPlayableOutput`
- **Removed from FixedUpdateNetwork/Render**: `UpdateUpperBodyLayer()`, `UpdateLowerBodyLocomotionOverride()`, `UpdateLookAtIK()`

### Ranged substates (all 7 files)
Removed `skipLowerBodyBlend = true` from: `BotRifleAimPlayable`, `BotRifleCockingPlayable`, `BotRifleShootPlayable`, `BotRifleReloadPlayable`, `BotBowDrawArrowPlayable`, `BotBowChargePlayable`, `BotBowShotPlayable`

### Ranged movement — all ranged action states now call `StopMovement()`
- `BotRifleAimPlayable`: removed `MaintainRangedSpacing()` + `ApplyStrafe()` → `StopMovement()`; fire timer = `Random.Range(0.15f, 0.5f)`
- `BotRifleCockingPlayable`: movement → `StopMovement()`; added distance check → `RifleRunPlayable` if target too close
- `BotRifleShootPlayable`: added `StopMovement()`
- `BotRifleReloadPlayable`: added `StopMovement()` + distance check → `RifleRunPlayable`
- `BotBowDrawArrowPlayable`: added distance check → `BowRunPlayable`
- `BotBowChargePlayable`: movement → `StopMovement()`; charge timer = `Random.Range(0.15f, 0.5f)`
- `BotBowShotPlayable`: no movement change needed

### BotMovementController.cs — Ranged ranges
- `minRifleRange = 10f` (was effectively ~2m)
- `minBowRange = 7f`
- `IsTargetTooClose(float threshold = 5f)` — new helper, used in cocking/reload/bow draw distance checks

---

## Session 2 Changes (NavMesh, Melee Combo, Loot, LOS)

### NavMesh — BotMovementController.cs
- **Spawned()**: `navAgent.Warp()` at spawn to ensure `isOnNavMesh = true` from tick 0
- **FixedUpdateNetwork()**: sync `navAgent.nextPosition = botKCC.Position` when on NavMesh; warp recovery radius increased 3f → 20f
- **NavigateTo()**: throttled `SetDestination` — only calls when destination moves > 0.5m (`_activeNavDestination` field); `pathPending` hold (max 3 frames via `_pathPendingFrames`); uses `navAgent.velocity` (post-RVO) with `desiredVelocity` fallback; off-NavMesh fallback moves toward nearest NavMesh point via `SamplePosition(20f)` instead of straight-line toward destination
- **MoveInDirection()**: when outside safe zone, now calls `NavigateToSafeZone()` instead of `GetSafeZoneDirection()` + direct move (direct move ignored NavMesh/rocks)
- **PickNewWanderDirection()**: tries 4 random 6–18m NavMesh points; on failure tries 4 short-range 1–3m fallbacks; on total failure routes to nearest `BuildingExit`

### Melee combo fixes
All attack states had `nextPuncDelay = timer + Xs` gate that caused the chain to bail to run state before the delay expired. Fix: return `null` (wait) while in the gap window rather than returning run state.

- **BotFistFirstPunch / BotFistMiddlePunch**: wait-in-gap fix; chain to next punch correctly
- **BotSwordAttackOne / BotSwordAttackTwo**: wait-in-gap fix; chain to next attack correctly
- **BotSpearAttackOne**: fixed self-reference bug (was returning `SpearAttackOne` instead of `SpearAttackTwo`); changed `CanSwordAttack()` → `CanSpearAttack()`
- **BotSpearAttackTwo**: removed `nextPuncDelay` gate entirely; after finisher goes to `SpearRun` (there is NO SpearAttackThree — do not add it)

### Loot pickup — BotMovementController.cs
- `ScanForLoot()`: added `if (prio < 0) continue` to all item blocks so priority −1 items are skipped
- Secondary weapon skip: `sec.Supplies > 0` guard prevents picking up depleted secondaries
- `GetLootPriority()`: secondary weapons return −1 if bot already has a secondary with ammo
- `SecondaryHasAmmo()`: new private helper checks `Supplies`, `RifleMagazine`, `BowMagazine`

### LOS fix — BotMovementController.cs
All 4 LOS raycasts changed from `obstructionLayer.value == 0 ? Physics.AllLayers : obstructionLayer.value` to `obstructionLayer.value != 0 ? obstructionLayer.value : ~playerLayer.value`. With `Physics.AllLayers` as fallback, bots could see through solid geometry when `obstructionLayer` was unset.

---

## Session 3 Changes (Overlap, Dead Target, Stuck States, Peripheral Detection, Building Exit)

### MoveToTarget() — BotMovementController.cs
- Added `if (!IsTargetValid()) { SetTarget(null); return; }` at top — prevents chasing dead enemies
- Sweet-spot hold case now calls `ComputeSeparationOffset()` to push away bystander bots

### ComputeSeparationOffset() — BotMovementController.cs (new private method)
Scans `playerLayer` within 0.75m, excludes self and `detectedTarget`, computes weighted push vector for each overlapping entity. Used by `TryLungeForward()` and `MoveToTarget()` sweet-spot case.

### TryLungeForward() — BotMovementController.cs
Runs `ComputeSeparationOffset()` first. If any entity is within 0.75m, applies separation push instead of lunging. Prevents bots from lunging through each other during melee attacks.

**Why**: KCC collision between bots/players requires the SimpleKCC collision layer mask to include the other entity's layer. Configure this on the bot prefab's `SimpleKCC` component. The code separation is a fallback for when layers aren't configured.

### MaintainRangedSpacing() — BotMovementController.cs
Rewritten to cover all four cases cleanly:
- `dist < 3m`: direct back-away move
- `dist > 15m`: `MoveToTarget()` to close in
- `3–15m, no LOS`: `NavigateTo(target)` to reposition for LOS
- `3–10m (< minRifleRange), has LOS`: `NavigateTo(target + awayDir * (minRifleRange + 1))` to back into firing range
- `10–15m, has LOS`: `FaceTarget()` only — `CanRifleShoot()` passes next tick

**Why**: The previous dead zone (3–15m with no specific condition) did nothing → bots froze in rifle-run animation.

### PickNewWanderDirection() — BotMovementController.cs
Added short-range fallback after all long-range attempts fail: tries 4 compass directions at 1–3m. If still nothing, routes to nearest `BuildingExit`.

**Why**: Bots in confined NavMesh areas (small rooms, building corners) had all 6–18m samples fail → `_hasWanderDestination = false` → held position in run animation indefinitely.

### Peripheral Awareness — BotMovementController.cs (new)
**New serialized fields** (tunable in Inspector):
- `peripheralDetectionChance = 0.3f` — probability per scan of noticing an out-of-FOV entity
- `peripheralFleeChance = 0.35f` — probability of fleeing vs attacking when peripherally detected

**`CheckPeripheralAwareness(Collider[] hits)`** — called from `DetectTarget()` when FOV scan finds nothing. Runs every ~0.5s with jitter. Re-uses the same `OverlapSphere` collider array. Considers only entities OUTSIDE the FOV cone. Requires clear LOS (walls block peripheral awareness). Detection chance scales with distance (closer = higher chance). On detection: random flee or attack.

**`PickFleeDirection(Vector3 threatPosition)`** — public method. Computes away vector from threat, tries 4 NavMesh samples at 8–15m in that direction with angle variation. On success: sets `_wanderDestination`, `_hasWanderDestination = true`, resets `WanderTimer` to 3–5s. Bot will wander away naturally via existing run states.

### Building Exit System

**`BuildingExit.cs`** (new script — `018 Bot/BuildingExit.cs`):
- `MonoBehaviour` with static `List<BuildingExit> _all` maintained by `OnEnable`/`OnDisable`
- `BuildingExit.FindNearest(NavMeshAgent, Vector3)`: returns nearest exit reachable via complete NavMesh path
- Shows cyan gizmo sphere + "Exit" label in Scene view
- **Setup**: Place empty GameObjects with this component at each building door threshold (ground level) and stair tops/bottoms

**`NavigateToSafeZone()`** updated:
1. If `_exitTarget` is set (currently routing to an exit), keep navigating to it; clear when within 2m
2. Otherwise: test direct path to safe zone center
3. If `PathPartial`/`PathInvalid` (bot on disconnected NavMesh island inside building): find nearest `BuildingExit`, set as `_exitTarget`, navigate there first
4. Fall through to direct safe zone nav if no exits found

**`PickNewWanderDirection()`** updated:
After all short-range fallbacks fail: calls `BuildingExit.FindNearest()`. If found, sets that exit as the wander destination so the bot walks out.

---

## Known Issues / Not Yet Fixed

- **KCC overlap**: The code-based separation (`ComputeSeparationOffset`) reduces overlap but the definitive fix is configuring the `SimpleKCC` collision layer mask on the bot prefab to include the player/bot layer
- **Building NavMesh connectivity**: `BuildingExit` routing only works if the interior NavMesh at each exit point is connected to the exterior NavMesh via a NavMesh Link (Unity Editor). Without this link, `FindNearest()` will return null even with exits placed
- **Stair navigation**: Stairs need either walkable NavMesh baked over them (slope < agent max slope) or NavMesh Links at each floor landing

---

## File Reference

| File | What changed |
|------|-------------|
| `018 Bot/BotMovementController.cs` | NavMesh sync, NavigateTo, MoveToTarget, TryLungeForward, MaintainRangedSpacing, PickNewWanderDirection, NavigateToSafeZone, DetectTarget (peripheral), LOS mask, loot, separation |
| `018 Bot/BotPlayables.cs` | Removed upper body layer, IK, override system |
| `018 Bot/BuildingExit.cs` | New — building exit point marker |
| `018 Bot/000-Locomotions/001-SubState/BotRifleAimPlayable.cs` | skipLowerBodyBlend removed, StopMovement, timer randomized |
| `018 Bot/000-Locomotions/001-SubState/BotRifleCockingPlayable.cs` | skipLowerBodyBlend removed, StopMovement, distance check |
| `018 Bot/000-Locomotions/001-SubState/BotRifleShootPlayable.cs` | skipLowerBodyBlend removed, StopMovement |
| `018 Bot/000-Locomotions/001-SubState/BotRifleReloadPlayable.cs` | skipLowerBodyBlend removed, StopMovement, distance check |
| `018 Bot/000-Locomotions/001-SubState/BotBowDrawArrowPlayable.cs` | skipLowerBodyBlend removed, distance check |
| `018 Bot/000-Locomotions/001-SubState/BotBowChargePlayable.cs` | skipLowerBodyBlend removed, StopMovement, timer randomized |
| `018 Bot/000-Locomotions/001-SubState/BotBowShotPlayable.cs` | skipLowerBodyBlend removed |
| `018 Bot/000-Locomotions/001-SubState/BotFistFirstPunch.cs` | Wait-in-gap combo fix |
| `018 Bot/000-Locomotions/001-SubState/BotFistMiddlePunch.cs` | Wait-in-gap combo fix |
| `018 Bot/000-Locomotions/001-SubState/BotSwordAttackOne.cs` | Wait-in-gap combo fix |
| `018 Bot/000-Locomotions/001-SubState/BotSwordAttackTwo.cs` | Wait-in-gap combo fix |
| `018 Bot/000-Locomotions/001-SubState/BotSpearAttackOne.cs` | Self-reference bug fix, CanSpearAttack |
| `018 Bot/000-Locomotions/001-SubState/BotSpearAttackTwo.cs` | Goes to SpearRun (no SpearAttackThree exists) |
