# Bot System — Full Rewrite Documentation

## Overview

Complete overhaul of the bot AI system. Bots now use NavMesh pathfinding, full rifle/bow projectile combat, item pickup from ground and crates, 360° target detection, and a significantly smarter priority-based AI.

---

## Files Modified

### `Botdata.cs`
- **Fixed `ApplyDamage` bug** — `remainingDamage` was computed but `CurrentHealth` was never decremented. Now correctly applies armor absorption then subtracts from health.
- **Centralized death logic** into `HandleDeath(killer, killerObject, killerStats)` — all 3 death paths (CircleDamage, FallDamage, ApplyDamage) call this single method.
- **Added secondary weapon drop on death** — previously only primary + armor were dropped.
- **Added 6 networked projectile replication properties:**
  - `BulletFiredTick`, `BulletStart`, `BulletTarget`
  - `ArrowFiredTick`, `ArrowStart`, `ArrowTarget`
- **Added `FireBullet(Transform muzzlePoint, Vector3 targetPos)`** — lag-compensated raycast, hits Player and Bot tags. Damage: Head=60 / Body=45 / Thigh=35 / Shin=30 / Foot=25 / Arm=40 / Forearm=30.
- **Added `FireArrow(Transform muzzlePoint, Vector3 targetPos)`** — same pattern. Damage: Head=75 / Body=55 / Thigh=45 / Shin=40 / Foot=35 / Arm=50 / Forearm=40.
- **`Render()`** detects `BulletFiredTick` / `ArrowFiredTick` changes to play VFX on non-authority clients.

---

### `BotInventory.cs`
- Added `[SerializeField] private Transform rifleHand` and `bowHand` with public getters.
- Added `[Networked] public int RifleMagazine` (max 30) and `BowMagazine` (max 20).
- Added `GetSecondaryWeaponID()` — returns `SecondaryWeapon.WeaponID` or `""`.
- Added `SwitchToHands()`, `SwitchToPrimary()`, `SwitchToSecondary()` — sets `WeaponIndex` to 1/2/3.

---

### `BotBasicMovement.cs`
- Mixer expanded from 24 → **33 slots** (spearAttackThree removed, rifle/bow added).
- Added 10 new `[SerializeField] AnimationClip` fields: `rifleIdle`, `rifleRun`, `rifleAim`, `rifleShoot`, `rifleReload`, `bowIdle`, `bowRun`, `bowDrawArrow`, `bowCharge`, `bowShot`.
- Added 10 new public state properties for rifle/bow.
- `GetPlayableAnimation()` updated with all new cases.
- **Removed `spearAttackThree`** — spear only has 2 attack states.

#### animationnames list (33 entries, index = mixer slot):
| Index | Value |
|---|---|
| 0 | *(empty)* |
| 1 | `idle` |
| 2 | `hit` |
| 3 | `stagger` |
| 4 | `gettingup` |
| 5 | `falling` |
| 6 | `death` |
| 7 | `run` |
| 8 | `firstpunch` |
| 9 | `middlepunch` |
| 10 | `lastpunch` |
| 11 | `swordidle` |
| 12 | `swordrun` |
| 13 | `swordAttackOne` |
| 14 | `swordAttackTwo` |
| 15 | `swordAttackThree` |
| 16 | `spearidle` |
| 17 | `spearrun` |
| 18 | `spearAttackOne` |
| 19 | `spearAttackTwo` |
| 20 | `healing` |
| 21 | `repairing` |
| 22 | `trap` |
| 23 | `rifleidle` |
| 24 | `riflerun` |
| 25 | `rifleaim` |
| 26 | `rifleshoot` |
| 27 | `riflereload` |
| 28 | `bowidle` |
| 29 | `bowrun` |
| 30 | `bowdrawarrow` |
| 31 | `bowcharge` |
| 32 | `bowshot` |

`mixernames` list: 1 entry → `basic`

---

### `BotMovementController.cs` — Full Rewrite

Replaced direct-movement + raycast obstacle avoidance with **NavMesh + SimpleKCC hybrid**:
- `NavMeshAgent.updatePosition = false`, `updateRotation = false`
- Each tick: `navAgent.nextPosition = botKCC.Position` (keeps NavMesh in sync)
- `NavigateTo(Vector3)` uses `navAgent.desiredVelocity` as direction fed to `botKCC.Move()`; falls back to direct movement if not on NavMesh

**New/changed public API (called by animation substates):**

| Method | Description |
|---|---|
| `DetectTarget()` | 360° sphere, no angle cone, picks closest visible target. Damage-awareness override (pursues attacker up to 60m regardless of angle) |
| `MoveToTarget()` | Calls `NavigateTo(target.position)` |
| `MoveInDirection()` | Direct KCC wander movement with safe zone correction |
| `PickNewWanderDirection()` | Random normalized direction |
| `NavigateTo(Vector3)` | NavMesh-pathed movement |
| `NavigateToSafeZone()` | NavMesh path to safe zone center |
| `FaceTarget()` | Slerp look rotation toward target |
| `StopMovement()` | `botKCC.Move(Vector3.zero)` |
| `ApplyStrafe()` | Random direction flip every 1–2s, strafes at 35% speed |
| `CanPunch()` | Distance ≤ 1.2m |
| `CanSwordAttack()` | Distance ≤ 1.5m |
| `CanSpearAttack()` | Distance ≤ 2.2m |
| `CanRifleShoot()` | Distance ≥ 15m AND has ammo |
| `CanBowShoot()` | Distance ≥ 15m AND has ammo |
| `IsInRangedRange()` | Distance ≥ 15m |
| `GetTargetAimPosition()` | Target position + Vector3.up * 1.2f |
| `ScanForLoot()` | OverlapSphere on itemLayer, priority-sorts all crates + dropped weapons |
| `HasLootTarget()` | Whether a loot target was found |
| `NavigateToLootTarget()` | Navigate to current loot target |
| `IsAtLootTarget()` | Within 1.5m of loot target |
| `TryPickupLoot()` | Calls `crate.PickupItemForBot()` or `item.InitializeItem(botObject, true)`. Auto-equips picked-up weapons |
| `IsOutsideSafeZone()` | Uses `SafeZone.CurrentShrinkSize.x / 2f` as radius |

**Loot priority order:**
1. Rifle (003) if no secondary
2. Bow (004) if no secondary
3. Rifle if different secondary held
4. Bow if different secondary held
5. Sword (001) if no primary
6. Spear (002) if no primary
7. Rifle ammo (005) if holding rifle and low ammo
8. Bow ammo (006) if holding bow and low ammo
9. Armor (007) if none or low
10. Heal (008), Repair (009), Trap (010)

**Networked timers:** `WanderTimer`, `IdleBeforeWanderTimer`, `LootScanTimer` (resets to 3s after each scan)

---

### `CrateController.cs`
- Added `PickupItemForBot(string itemkey, Botdata bot)` — handles all 10 item types (001–010) for bots. Spawns weapons with `PlayerRef.None` (server authority). Handles ammo, armor, heal, repair, trap directly on `BotInventory`.

---

### New Substates: Rifle (5 files)

**`BotRifleIdlePlayable.cs`** — Entry idle for rifle. Checks heal/repair priority, safe zone, then detect + transition to run or reload.

**`BotRifleRunPlayable.cs`** — Pursues or wanders with rifle. If target in ranged range → `RifleAimPlayable`. If out of ammo → `RifleReloadPlayable`. Includes loot scanning during wander.

**`BotRifleAimPlayable.cs`** — Holds aim pose (looping). Faces target, stops movement, strafes. At 60% of animation length → calls `botData.FireBullet(SecondaryWeapon.ImpactPoint, targetAimPos)`, decrements `RifleMagazine`, transitions to `RifleShootPlayable`.

**`BotRifleShootPlayable.cs`** — One-shot recoil animation. On finish → returns to aim or reload based on ammo/target.

**`BotRifleReloadPlayable.cs`** — One-shot reload. At 70% of animation → `RifleMagazine = 30`. On finish → `RifleIdlePlayable`.

---

### New Substates: Bow (5 files)

**`BotBowIdlePlayable.cs`** — Mirror of RifleIdle for bow.

**`BotBowRunPlayable.cs`** — Pursues or wanders with bow. If target in ranged range → `BowDrawArrowPlayable`. If no ammo, keeps moving (melee fallback). Includes loot scanning.

**`BotBowDrawArrowPlayable.cs`** — One-shot draw animation. Faces target, stops movement. On finish → `BowChargePlayable`.

**`BotBowChargePlayable.cs`** — Looping charge pose. After 0.5s → calls `botData.FireArrow(SecondaryWeapon.ImpactPoint, targetAimPos)`, decrements `BowMagazine`, transitions to `BowShotPlayable`.

**`BotBowShotPlayable.cs`** — One-shot release animation. On finish → draw again or run.

---

### Modified Existing Substates

| File | Change |
|---|---|
| `BotIdlePlayable.cs` | Added `WeaponIndex == 3` → rifle/bow run transitions |
| `BotRunPlayable.cs` | Added `WeaponIndex == 3` branches in has-target and wander paths |
| `BotSwordIdle.cs` | Added early-out: if `WeaponIndex == 3` → rifle/bow idle |
| `BotSpearIdle.cs` | Same as BotSwordIdle |
| `BotSpearRun.cs` | Fixed `CanSwordAttack()` → `CanSpearAttack()` (correct longer range) |
| `BotHealingPlayable.cs` | Added `WeaponIndex == 3` case in MovePlayer() |
| `BotRepairArmorPlayable.cs` | Added `WeaponIndex == 3` case in MovePlayer() |

---

## Inspector Setup Checklist

- [ ] Add `NavMeshAgent` component to bot prefab root
- [ ] Set NavMeshAgent `updatePosition = false`, `updateRotation = false` in Inspector
- [ ] Assign `NavMeshAgent` reference in `BotMovementController`
- [ ] Assign `itemLayer` mask in `BotMovementController` (crate + weapon collider layers)
- [ ] Assign 10 new animation clips in `BotBasicMovement` (rifleIdle → bowShot)
- [ ] Assign `rifleHand` and `bowHand` transforms in `BotInventory`
- [ ] Bake NavMesh on game map (Window → AI → Navigation → Bake)
- [ ] Mark static geometry as Navigation Static before baking

---

## Known Bugs Fixed

| Bug | Fix |
|---|---|
| `ApplyDamage` never applied damage | Uncommented and corrected armor absorption + health subtraction block |
| Bot death only dropped primary + armor | Added `SecondaryWeapon.DropWeapon()` in `HandleDeath()` |
| `CanSwordAttack()` used in spear run | Changed to `CanSpearAttack()` (2.2m range vs 1.5m) |
| Bot detection had 90° angle cone | Replaced with 360° sphere; cone only existed because `detectionAngle` divided in half |
| Safe zone used `localScale.x * 0.5f` | Changed to `SafeZone.CurrentShrinkSize.x / 2f` matching all other game code |

---

---

## Next Fixes Needed

### 1. Player Upper/Lower Body Split — Rifle & Bow Reference

Before implementing bot movement during ranged animations, read and understand:
- `PlayerUpperMovement.cs` — handles upper body states: aim, reload, cock, shoot
- `PlayerBasicMovement.cs` — handles lower body: idle, run, strafe

The player uses **two separate mixers** with **avatar masks** so upper and lower body animations blend independently. Bots currently use a single mixer with no masking, which means the rifle aim animation locks the whole body and the bot cannot move legs while aiming.

---

### 2. Bot Movement During Rifle/Bow Aim & Shoot

Currently in `BotRifleAimPlayable` and `BotBowChargePlayable` the bot calls `StopMovement()` — it stands completely still while aiming. This looks unnatural and makes bots sitting ducks.

**Goal:** Bots should strafe/move while their upper body plays aim/shoot/reload animations, exactly like the player does via upper/lower split.

**Approach options to evaluate after reading player scripts:**
- Option A: Add a second mixer to bots (upper + lower) with avatar masks, mirroring the player architecture.
- Option B: Keep single mixer but allow leg movement by blending run animation weight partially while aim weight is also active.

Read `PlayerUpperMovement.cs` and `PlayerBasicMovement.cs` first to decide which approach fits best.

---

### 3. State Transition Architecture — Return-Based Pattern

Currently bot substates call `botPlayablesChanger.ChangeState(...)` directly inside `NetworkUpdate()`. The player states use a **different pattern**: each state returns the desired next state (or null to stay), and the changer decides whether to transition.

**What to do:**
- Read the player state scripts to understand the return-based pattern.
- Rewrite `BotAnimationPlayable.NetworkUpdate()` return type from `void` → the correct pattern.
- Update `BotPlayableChanger` to call the new pattern.
- Update all existing and new bot substates to return the next state instead of directly calling `ChangeState`.

This change makes state transitions safer (no mid-frame double-transitions), easier to debug, and consistent with the player codebase.

---

## Post-Rewrite Updates (Current Session)

This section documents all follow-up fixes and structural changes applied after the initial rewrite.

### 1) Strategic AI modularization

#### New files
- `Assets/000 - BattleRoyale/001 - Scripts/018 Bot/AI/BotAIDefinitions.cs`
  - Introduced shared enum `BotStrategicGoal`:
    - `AcquireLoot`, `HuntPlayers`, `RotatePosition`, `HoldCamp`, `Recover`
- `Assets/000 - BattleRoyale/001 - Scripts/018 Bot/AI/BotAIStrategicBrain.cs`
  - Extracted high-level strategic planning from `BotMovementController`
  - Owns strategic state (goal, focus/watch points, plan timers, camp hold window, personality factors)
  - Handles:
    - periodic replanning
    - recover/disengage decisions
    - loot pressure decisions
    - hunt/camp/rotate goal selection
    - exploration movement execution for non-combat roaming

#### `BotMovementController.cs` integration
- Brain is created in `Awake()`
- `Spawned()` bootstraps the brain on state authority
- `FixedUpdateNetwork()` calls strategic replanning
- Idle facing and roam driving now delegate to the brain
- Added public hooks used by the brain:
  - `AIBotdata`
  - `AIMotorPosition`
  - `NavigateFlatXZ(Vector3)`
  - `ApplyStrafePublic()`

---

### 2) ScriptableObject policy-based AI tuning

#### New file
- `Assets/000 - BattleRoyale/001 - Scripts/018 Bot/AI/BotAIPolicy.cs`
  - Added `BotAIPolicy : ScriptableObject` with `CreateAssetMenu`
  - Added resolved runtime struct `BotAIStrategicTuning`
  - Added `EmbeddedDefaults` so unassigned policy preserves legacy behavior

#### What became tunable
- Replan cadence and jitter
- Recover thresholds and disengage distance
- Loot roam blend/jitter
- Camp chance, camp hold duration, camp arrival distance, ring placement
- Hunt blend and noise
- Safe-zone focus clamp scaling

#### Wiring
- `BotMovementController` now has optional serialized field:
  - `strategicAiPolicy`
- `BotAIStrategicBrain` resolves tuning from:
  - assigned `BotAIPolicy`, or
  - `BotAIStrategicTuning.EmbeddedDefaults`

---

### 3) Secondary weapon bot-handling and null safety

#### `SecondaryWeaponItem.cs`
- Fixed bot-specific null reference path where code assumed `PlayerCore` exists for all holders
- Added carry-parent resolver that supports both:
  - player holders via `PlayerCore.Inventory`
  - bot holders via `CurrentPlayer.GetComponent<BotInventory>()`
- `Render()` and `FixedUpdateNetwork()` now use the same safe parent resolution path

#### `BotInventory.cs`
- Added secondary carry transforms and getters:
  - `RifleBack`, `BowBack` (in addition to `RifleHand`, `BowHand`)

---

### 4) Fusion networked-property access safety

#### Issue fixed
- `InvalidOperationException` when reading `Botdata.IsDead` before behavior simulation state was ready.

#### `BotMovementController.cs` fix
- Added `CanReadNetworkedCombatState(NetworkBehaviour nb)` guard:
  - checks `nb != null`
  - checks `nb.Object != null`
  - checks `nb.Object.IsValid`
  - checks `nb.Object.IsInSimulation`
- Applied this guard before reading networked combat flags from cached targets.

---

### 5) Wall/collision stuck mitigation (no layer separation required)

#### `BotMovementController.cs`
- Upgraded `NavigateTo(Vector3)` with anti-stuck steering:
  - tracks movement progress toward destination
  - detects low/no progress while target remains roughly the same
  - injects temporary side-step/back-off steering
  - ray-probes escape direction and chooses less-blocked side

This is designed for maps where world/collision objects share broad layers (e.g. same "Ground" layer) and traditional layer-based avoidance is insufficient.

---

### 6) Primary melee hit robustness for bot attackers

#### `PrimaryWeaponItem.cs`
- Fixed null reference in `DamagePlayer(...)` when attacker is a bot:
  - added null checks for victim components (`PlayerPlayables`, `PlayerHealthV2`, movement references)
  - removed hard dependency on `PlayerMovementV2` for knockback direction
  - knockback now derives from attacker-to-victim world direction with fallback
  - attacker display name passed to damage now uses safe fallback values when references are missing

---

### 7) Weapon parenting/equip consistency fixes

#### `PrimaryWeaponItem.cs`
- Fixed bot spear back-parent typo:
  - unequipped bot spear was incorrectly parented to `SpearHand`
  - corrected to `SpearBack` in both authority and client positioning paths

#### `BotInventory.cs`
- `SwitchToHands()`, `SwitchToPrimary()`, `SwitchToSecondary()` now also update weapon `IsEquipped` booleans
- Added `SyncEquipFlagsToWeaponIndex()` safety method

#### `BotPlayables.cs`
- `FixedUpdateNetwork()` now calls `inventory.SyncEquipFlagsToWeaponIndex()` on state authority
  - keeps visual equip flags and `WeaponIndex` synchronized every tick
  - prevents stale dual-weapon hand states

---

### 8) Secondary-weapon state machine activation fixes

#### `BotInventory.cs`
- `GetSecondaryWeaponID()` now normalizes IDs:
  - trims whitespace
  - parses numeric values
  - returns zero-padded canonical form (`"003"`, `"004"`)

#### `CrateController.cs`
- `PickupItemForBot()` now forces `inv.SwitchToSecondary()` immediately after bot rifle/bow pickup (`003`/`004`)
  - ensures `WeaponIndex == 3` and proper bow/rifle locomotion/state transitions

---

### 9) Loot thrash and unreachable-loot recovery

#### Rifle/Bow swap loop prevention
- `BotMovementController.GetLootPriority(...)` changed so bots do not swap between rifle and bow repeatedly:
  - if bot already has a secondary, direct rifle/bow pickup priorities return `-1`
  - bots prioritize ammo/support loot instead

#### Unreachable target bailout
- Added unreachable-loot handling in `BotMovementController`:
  - tracks progress toward active loot target
  - if distance progress stalls for a short duration, bot abandons target
  - abandoned target is briefly blacklisted to avoid immediate re-target loops

This addresses cases where loot appears near obstacle edges (rocks/crates) and bots repeatedly fail pathing into the same point.

---

## Updated Inspector / Content Requirements

- `BotInventory` now requires these secondary transforms assigned on bot prefab:
  - `RifleHand`, `RifleBack`, `BowHand`, `BowBack`
- Optional strategic tuning asset:
  - create and assign `BotAIPolicy` to `BotMovementController.strategicAiPolicy`
  - if not assigned, runtime defaults are preserved

---

## Current Behavioral Intent (after fixes)

- Strategic planning is modular (`BotAIStrategicBrain`) and policy-tunable (`BotAIPolicy`)
- Weapon visuals are always synchronized to active `WeaponIndex`
- Bots can safely hold either player or bot weapon ownership paths without null-reference crashes
- Bots no longer repeatedly swap rifle/bow when both are nearby
- Bots can abandon temporarily unreachable loot and continue decision flow
- Navigation includes geometry-based unstuck behavior independent of strict layer setup
