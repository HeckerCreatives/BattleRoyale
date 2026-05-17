# DOTS Migration Dev Tickets

---

## Phase 0 — Baseline & Guardrails

- [ ] Add `UnityEngine.Profiling.Profiler.BeginSample/EndSample` markers to `BotMovementController.DetectTarget()`, `BotAIStrategicBrain.TickReplan()`, `EvaluateCombatLoadout()`, and `NavigateTo()`
- [ ] Record baseline server tick time, AI CPU ms, and GC alloc/frame at 20/40/60 bot counts using Unity Profiler and log to a spreadsheet
- [ ] Write a runtime assertion script that validates: kill feed fires on bot death, game-over triggers when only bots remain, bot ammo state matches networked inventory
- [ ] Document current aim-assist cone radius, slowdown strength, and magnet strength values as the acceptance baseline for regression checks

---

## Phase 1 — ECS Read-Only Mirror

- [ ] Install `com.unity.entities` and `com.unity.collections` packages; confirm no Fusion compile conflicts; gate all ECS code behind `UNITY_DOTS` define symbol
- [ ] Define unmanaged ECS components: `BotTag`, `AliveComponent`, `HealthComponent`, `PositionComponent`, `VelocityComponent`, `TeamComponent`
- [ ] Define unmanaged ECS components: `WeaponStateComponent` (weaponIndex, weaponId), `AmmoStateComponent` (rifleMag, bowMag, supplies)
- [ ] Define blob-asset configs: `PerceptionConfigBlob` (detectionRadius, detectionAngle, peripheralChance), `StrategicConfigBlob` (wander delays, loot scan radius, melee/ranged ranges)
- [ ] Create `BotEntityBridge : MonoBehaviour` that holds a stable `BotRuntimeId` (int) matching `Botdata.BotIndex` and an `Entity` handle; attach to bot prefab root
- [ ] Implement `BotEcsBootstrap : ISystem` that creates one entity per bot on `Spawned()` and destroys it on `Despawned()`
- [ ] Implement `BotStateWriteSystem` that runs each simulation step and copies position, velocity, hp, alive, ammo, weaponIndex from GO/Fusion into ECS components using `EntityManager.SetComponentData`
- [ ] Add debug overlay (Scene view Gizmos) that draws ECS position vs GO position diff to confirm parity; disable via scripting define before shipping

---

## Phase 2 — DOTS Perception & Scoring

- [ ] Implement `BotNearbyCollectionJob : IJobParallelFor` that reads all bot `PositionComponent` pairs and writes candidate distances into a `NativeArray<PerceptionCandidate>` (id, distance, position)
- [ ] Implement `FovConeFilterJob : IJobParallelFor` that filters `PerceptionCandidate` list by simulated-camera forward dot product and half-angle; outputs surviving candidates
- [ ] Implement LOS batch raycast using `Physics.RaycastCommand` + `JobHandle` that discards candidates blocked by obstruction layer; keep results in `NativeArray<byte>`
- [ ] Implement `UtilityScoringJob : IJobParallelFor` that assigns a float score to each surviving candidate: distance weight, health delta weight, last-damage-source bonus, zone urgency modifier
- [ ] Implement `LootScoringJob : IJobParallelFor` that scores nearby loot items by priority using the same logic as `GetLootPriority()`; writes top-N results per bot
- [ ] Write `BotPerceptionResultReader` method on `BotEntityBridge` that reads the top-scored candidate from ECS output `NativeArray` and feeds it into `BotMovementController.SetTarget()` each simulation tick
- [ ] Add `UseEcsPerception` bool flag in `BotMovementController`; when false, fall back to existing `DetectTarget()` path; validate parity at 20-bot count before enabling by default

---

## Phase 3 — DOTS Strategic Brain Output

- [ ] Define output ECS components: `DesiredGoalComponent` (enum: Loot/Hunt/Rotate/Camp/Recover), `DesiredTargetComponent` (BotRuntimeId), `DesiredMoveDirectionComponent` (float3, urgency float), `DesiredCombatModeComponent` (enum: Melee/Ranged/Evade)
- [ ] Implement `StrategicGoalSystem : ISystem` that reads perception outputs + zone state + ammo state and writes `DesiredGoalComponent` per bot entity
- [ ] Implement `StrategicTargetSystem : ISystem` that selects the highest-utility target from perception candidates and writes `DesiredTargetComponent`
- [ ] Implement `StrategicMoveSystem : ISystem` that computes desired move vector and urgency from goal + target + zone center; writes `DesiredMoveDirectionComponent`
- [ ] Implement `StrategicCombatModeSystem : ISystem` that selects melee/ranged/evade from weapon state, distance, ammo, and threat; writes `DesiredCombatModeComponent`
- [ ] Update `BotEntityBridge` to read all four output components and push them into `BotMovementController` and `BotPlayables` each simulation tick on state authority only
- [ ] Refactor `BotAIStrategicBrain.TickReplan()` to consume ECS outputs when `UseEcsStrategicBrain` flag is true; keep legacy path active as fallback
- [ ] Add `UseEcsStrategicBrain` feature flag; default off; validate bot behavior parity (chase, loot, retreat, safe-zone routing) before enabling

---

## Phase 4 — Player-Adjacent DOTS (Selective)

- [ ] Profile player-side `FindEnemyInCone()` in `PlayerCameraRotation` at max bot count; gate Phase 4 work on confirmed >1ms cost
- [ ] Implement `PlayerVisibilityJob` that batch-raycasts from each player origin to all bot positions per tick; write results into `NativeArray<VisibilityResult>` consumed by aim-assist
- [ ] Implement `CrowdAwarenessJob` that aggregates nearby entity density around each player and exposes a float3 crowd-push hint consumed by `PlayerMovementV2` for anti-overlap
- [ ] Add `UseEcsPlayerPerception` flag; keep `FindEnemyInCone()` GO path active as fallback; verify aim-assist cone, crosshair color, and slowdown feel are unchanged

---

## Phase 5 — Full DOTS Evaluation Gate

- [ ] Profile all four phases under target bot count; document CPU ms per system before deciding to proceed
- [ ] Evaluate Fusion-compatible DOTS animation path (Animation Rigging + Entities); prototype one bot state transition before committing to rewrite
- [ ] Estimate QA scope for full player+animation rewrite; if >3 weeks QA or any authority-safety ambiguity, document stop-at-hybrid decision and close roadmap
