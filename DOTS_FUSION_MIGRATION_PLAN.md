# DOTS + Fusion Migration Roadmap (AI and Player)

## Goal
Create a realistic migration path from the current GameObject + Photon Fusion setup to a DOTS-assisted architecture, without breaking multiplayer authority, combat, and animation pipelines.

This plan is intentionally phased. It starts with a **hybrid model** (best risk/perf ratio), then expands only if profiling proves it is needed.

---

## Executive Summary

- **Is it possible?** Yes.
- **Should it be done in one pass?** No.
- **Recommended strategy:** keep Fusion networking and authority on `NetworkObject`/`NetworkBehaviour`, migrate high-cost AI computation to DOTS first, then evaluate deeper migration.
- **Why hybrid first:** your current systems (Fusion authority, playables, weapon parenting, UI/camera logic) are tightly coupled to GameObjects and would be expensive to rewrite all at once.

---

## Current Constraints (Why Full Immediate Conversion Is Risky)

1. **Fusion model is GameObject-centric**
   - Snapshot replication, authority ownership, and many gameplay scripts are currently built around `NetworkBehaviour`.

2. **Animation stack complexity**
   - Player and bot state machines rely on Playables and weapon-specific upper/lower body behavior.
   - Full DOTS animation migration is non-trivial and can regress combat feel.

3. **Combat and inventory coupling**
   - Weapon pickup, equip flags, projectile/melee hit flows, and kill notifications currently depend on existing object references.

4. **Camera and input authority**
   - Player camera, aim assist, and local authority checks are tied to player-side MonoBehaviours.

---

## Recommended Architecture (Hybrid)

### Keep in Fusion / GameObject side

- `NetworkObject`, `NetworkBehaviour`, authority checks
- RPC/networked state that already works
- Player camera and input
- Final movement execution and animation state transitions
- Item/weapon world objects and parenting

### Move to DOTS side (first)

- AI perception batch jobs (distance/FOV/LOS prefiltering)
- Utility scoring / strategic decision jobs
- Threat map, loot scoring, zone desirability grids
- Expensive bot query loops and aggregation

### Bridge layer

- `BotEntityBridge` (MonoBehaviour): links each bot GO to an ECS entity
- Write GO runtime state -> ECS components each tick (position, hp, ammo, goal context)
- Run ECS systems/jobs
- Read ECS outputs -> GO commands (desired goal, desired move vector, target id)

---

## Phased Plan

## Phase 0 - Baseline and Guardrails (1-2 days)

### Objectives
- Capture current performance and behavior before migration.

### Tasks
- Add profiler markers around:
  - bot perception
  - strategic replan
  - target selection
  - movement decision loops
- Record baseline metrics:
  - server tick time
  - max bot count at target tick rate
  - CPU time for AI scripts
  - GC allocations/frame
- Freeze gameplay acceptance checks:
  - combat parity
  - aim assist behavior
  - kill feed correctness
  - game state transitions (including bot-only remainder done condition)

### Exit criteria
- You can compare before/after with measurable numbers.

---

## Phase 1 - ECS Read-Only Mirror (2-4 days)

### Objectives
- Build ECS world safely without driving gameplay yet.

### Tasks
- Create ECS components for read-only snapshots:
  - `BotTag`, `Alive`, `Health`, `Position`, `Velocity`, `Team`
  - `WeaponState`, `AmmoState`
  - `PerceptionConfig`, `StrategicConfig`
- Build bridge that copies GO/Fusion data into ECS each simulation step.
- Validate entity lifecycle:
  - spawn, despawn, reconnect-safe cleanup

### Exit criteria
- ECS data matches live bot GO state (debug parity checks pass).

---

## Phase 2 - DOTS Perception + Scoring (3-6 days)

### Objectives
- Offload the heaviest AI loops while keeping final decisions in existing bot controllers.

### Tasks
- Implement jobs for:
  - nearby candidate collection
  - FOV cone filtering
  - distance/risk/loot utility scoring
- Return ranked target/goal candidates back to `BotMovementController`/strategic layer.
- Keep final action selection in current C# GO logic for safety.

### Exit criteria
- Same gameplay decisions (or intentionally tuned differences), lower AI CPU cost.

---

## Phase 3 - DOTS Strategic Brain Output (4-8 days)

### Objectives
- Move strategic goal selection fully to ECS, keep locomotion/animation in existing scripts.

### Tasks
- ECS strategic systems produce:
  - `DesiredGoal` (loot/hunt/rotate/camp/recover)
  - `DesiredTarget`
  - `DesiredMoveDirection`
  - `DesiredCombatMode`
- GO side consumes those outputs and applies:
  - nav/kcc movement
  - playable state changes
  - weapon switching

### Exit criteria
- Bot behavior parity maintained, strategic CPU now mostly ECS-side.

---

## Phase 4 - Optional Player-Adjacent DOTS (Selective) (5-10 days)

### Objectives
- Only migrate proven hotspots around players, not full player control stack.

### Tasks
- Consider DOTS for:
  - crowd/nearby awareness
  - visibility/threat aggregation
  - non-authoritative helper computations
- Keep in GO/Fusion:
  - input handling
  - camera
  - weapon fire authority
  - animation state machine authority

### Exit criteria
- Measurable gain without player feel regressions.

---

## Phase 5 - Full DOTS Evaluation Gate (Decision, not automatic)

### Decision questions
- Are target scale goals still unmet after hybrid optimization?
- Is rewrite budget acceptable for animation/combat/network glue?
- Can you absorb QA cost for parity + edge cases?

If any answer is no, stop at hybrid; it is often the optimal long-term solution for Fusion projects.

---

## Data Contract Example (Bridge)

Use stable IDs so ECS and GO can reference the same actor safely:

- `BotRuntimeId` (int) -> matches bot index/network identity
- Input to ECS each tick:
  - position, hp, alive, ammo, zone context, last damage source
- Output from ECS each tick:
  - desired target id
  - desired strategic goal
  - desired move vector/urgency

Never let ECS directly mutate Fusion network state in early phases; GO/Fusion side remains the authority writer.

---

## Networking and Authority Rules

1. Only state authority writes replicated gameplay state.
2. ECS jobs run as compute, not as direct network state owners.
3. Bridge applies ECS outputs only on authoritative simulation side.
4. Client-side prediction/visuals should remain in existing flow until parity is proven.

---

## Testing Plan Per Phase

- **Determinism sanity:** same input seed gives stable strategic outputs in repeated runs.
- **Combat parity:** kill times, hit registration, retreat/chase behavior remain within tolerance.
- **Animation parity:** no wrong weapon pose/state bleed, no underground/root glitches.
- **Authority safety:** no invalid networked-property access before simulation validity.
- **Performance:** compare against Phase 0 baseline under same bot counts.

---

## Rollback Strategy

- Each phase should be behind a feature flag:
  - `UseEcsPerception`
  - `UseEcsStrategicBrain`
- Keep legacy GO logic available during rollout.
- If regression is found, disable flag and ship with previous stable path.

---

## Suggested Initial Scope for Your Project

Given your current codebase and recent fixes, start with:

1. ECS mirror + perception scoring for bots only.
2. Keep `BotMovementController` and playables as action executors.
3. Keep all player systems GO/Fusion for now.
4. Re-profile.
5. Expand only where profiler proves a bottleneck.

This gives you most of DOTS performance upside with much lower integration risk.

---

## Rough Effort Estimate

- Hybrid bot compute migration (Phases 0-3): **2-5 weeks** depending on QA depth and bot complexity.
- Player-adjacent selective migration (Phase 4): **1-3 weeks**.
- Full player + AI deep rewrite: **multi-month** effort with high regression risk.

---

## Final Recommendation

Do **not** convert everything to DOTS immediately.

Adopt a **Fusion-authoritative hybrid architecture** first, move AI compute hotspots to ECS/jobs, and use profiling gates to decide whether deeper migration is worth it.
