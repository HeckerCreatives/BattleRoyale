using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BotBasicMovement : NetworkBehaviour
{
    [SerializeField] private SimpleKCC simpleKCC;
    [SerializeField] private BotPlayables botPlayables;
    [SerializeField] private BotMovementController botMovement;

    [Space]
    [SerializeField] private List<string> animationnames;
    [SerializeField] private List<string> mixernames;

    [Space]
    [SerializeField] private AnimationClip idle;
    [SerializeField] private AnimationClip hit;
    [SerializeField] private AnimationClip stagger;
    [SerializeField] private AnimationClip gettingup;
    [SerializeField] private AnimationClip falling;
    [SerializeField] private AnimationClip death;
    [SerializeField] private AnimationClip run;
    [SerializeField] private AnimationClip firstPunch;
    [SerializeField] private AnimationClip secondPunch;
    [SerializeField] private AnimationClip lastPunch;
    [SerializeField] private AnimationClip swordIdle;
    [SerializeField] private AnimationClip swordRun;
    [SerializeField] private AnimationClip swordAttackOne;
    [SerializeField] private AnimationClip swordAttackTwo;
    [SerializeField] private AnimationClip swordAttackThree;
    [SerializeField] private AnimationClip spearIdle;
    [SerializeField] private AnimationClip spearRun;
    [SerializeField] private AnimationClip spearAttackOne;
    [SerializeField] private AnimationClip spearAttackTwo;
    [SerializeField] private AnimationClip healing;
    [SerializeField] private AnimationClip repairing;
    [SerializeField] private AnimationClip trap;
    [SerializeField] private AnimationClip rifleIdle;
    [SerializeField] private AnimationClip rifleRun;
    [SerializeField] private AnimationClip rifleAim;
    [SerializeField] private AnimationClip rifleCocking;
    [SerializeField] private AnimationClip rifleShoot;
    [SerializeField] private AnimationClip rifleReload;
    [SerializeField] private AnimationClip rifleAimIdle;
    [SerializeField] private AnimationClip rifleAimMove;
    [SerializeField] private AnimationClip rifleCockingIdle;
    [SerializeField] private AnimationClip rifleCockingMove;
    [SerializeField] private AnimationClip rifleReloadIdle;
    [SerializeField] private AnimationClip rifleReloadMove;
    [SerializeField] private AnimationClip bowIdle;
    [SerializeField] private AnimationClip bowRun;
    [SerializeField] private AnimationClip bowDrawArrow;
    [SerializeField] private AnimationClip bowCharge;
    [SerializeField] private AnimationClip bowShot;
    [SerializeField] private AnimationClip bowDrawIdle;
    [SerializeField] private AnimationClip bowDrawMove;
    [SerializeField] private AnimationClip bowChargeIdle;
    [SerializeField] private AnimationClip bowChargeMove;
    [SerializeField] private AnimationClip bowShotIdle;
    [SerializeField] private AnimationClip bowShotMove;

    public AnimationClip RifleAimClip => rifleAim;
    public AnimationClip RifleShootClip => rifleShoot;
    public AnimationClip RifleReloadClip => rifleReload;
    public AnimationClip BowDrawArrowClip => bowDrawArrow;
    public AnimationClip BowChargeClip => bowCharge;
    public AnimationClip BowShotClip => bowShot;
    public AnimationClip RifleAimIdleClip => rifleAimIdle != null ? rifleAimIdle : rifleAim;
    // Move-variants fall back to the Idle-variant (whose upper body has the correct pose).
    // The upper-body layer's AvatarMask filters out lower-body bones, so the Idle clip's stance
    // doesn't bleed into locomotion. This avoids defaulting to plain run/walk clips that have
    // no aim/draw/reload pose at all.
    public AnimationClip RifleAimMoveClip => rifleAimMove != null ? rifleAimMove : RifleAimIdleClip;
    public AnimationClip RifleCockingIdleClip => rifleCockingIdle != null ? rifleCockingIdle : (rifleCocking != null ? rifleCocking : rifleReload);
    public AnimationClip RifleCockingMoveClip => rifleCockingMove != null ? rifleCockingMove : RifleCockingIdleClip;
    public AnimationClip RifleReloadIdleClip => rifleReloadIdle != null ? rifleReloadIdle : rifleReload;
    public AnimationClip RifleReloadMoveClip => rifleReloadMove != null ? rifleReloadMove : RifleReloadIdleClip;
    public AnimationClip BowDrawIdleClip => bowDrawIdle != null ? bowDrawIdle : bowDrawArrow;
    public AnimationClip BowDrawMoveClip => bowDrawMove != null ? bowDrawMove : BowDrawIdleClip;
    public AnimationClip BowChargeIdleClip => bowChargeIdle != null ? bowChargeIdle : bowCharge;
    public AnimationClip BowChargeMoveClip => bowChargeMove != null ? bowChargeMove : BowChargeIdleClip;
    public AnimationClip BowShotIdleClip => bowShotIdle != null ? bowShotIdle : bowShot;
    public AnimationClip BowShotMoveClip => bowShotMove != null ? bowShotMove : BowShotIdleClip;
    public BotMovementController MovementController => botMovement;

    //  ================

    public AnimationMixerPlayable mixerPlayable;

    public BotIdlePlayable IdlePlayable;
    public BotHitPlayable HitPlayable;
    public BotStaggerPlayable StaggerPlayable;
    public BotGettingUpPlayable GettingUpPlayable;
    public BotFallingPlayable FallingPlayable;
    public BotDeathPlayable DeathPlayable;
    public BotRunPlayable RunPlayable;
    public BotFistFirstPunch FistFirstPunch;
    public BotFistMiddlePunch FistMiddlePunch;
    public BotFistLastPunch FistLastPunch;
    public BotSwordIdle SwordIdlePlayable;
    public BotSwordRunPlayable SwordRunPlayable;
    public BotSwordAttackOne SwordAttackOnePlayable;
    public BotSwordAttackTwo SwordAttackTwoPlayable;
    public BotSwordAttackThree SwordAttackThreePlayable;
    public BotSpearIdle SpearIdle;
    public BotSpearRun SpearRun;
    public BotSpearAttackOne SpearAttackOne;
    public BotSpearAttackTwo SpearAttackTwo;
    public BotHealingPlayable HealingPlayable;
    public BotRepairArmorPlayable RepairArmorPlayable;
    public BotTrapPlayable TrapPlayable;
    public BotRifleIdlePlayable    RifleIdlePlayable;
    public BotRifleRunPlayable     RifleRunPlayable;
    public BotRifleAimPlayable     RifleAimPlayable;
    public BotRifleCockingPlayable RifleCockingPlayable;
    public BotRifleShootPlayable   RifleShootPlayable;
    public BotRifleReloadPlayable  RifleReloadPlayable;
    public BotBowIdlePlayable      BowIdlePlayable;
    public BotBowRunPlayable       BowRunPlayable;
    public BotBowDrawArrowPlayable BowDrawArrowPlayable;
    public BotBowChargePlayable    BowChargePlayable;
    public BotBowShotPlayable      BowShotPlayable;

    //  =================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(botPlayables.playableGraph, 34);

        var idleClip = AnimationClipPlayable.Create(botPlayables.playableGraph, idle);
        var hitClip = AnimationClipPlayable.Create(botPlayables.playableGraph, hit);
        var staggerClip = AnimationClipPlayable.Create(botPlayables.playableGraph, stagger);
        var gettingUpClip = AnimationClipPlayable.Create(botPlayables.playableGraph, gettingup);
        var fallingClip = AnimationClipPlayable.Create(botPlayables.playableGraph, falling);
        var deathClip = AnimationClipPlayable.Create(botPlayables.playableGraph, death);
        var runClip = AnimationClipPlayable.Create(botPlayables.playableGraph, run);
        var firstPunchClip = AnimationClipPlayable.Create(botPlayables.playableGraph, firstPunch);
        var secondPunchClip = AnimationClipPlayable.Create(botPlayables.playableGraph, secondPunch);
        var lastPunchClip = AnimationClipPlayable.Create(botPlayables.playableGraph, lastPunch);
        var swordIdleClip = AnimationClipPlayable.Create(botPlayables.playableGraph, swordIdle);
        var swordRunClip = AnimationClipPlayable.Create(botPlayables.playableGraph, swordRun);
        var swordAttackOneClip = AnimationClipPlayable.Create(botPlayables.playableGraph, swordAttackOne);
        var swordAttackTwoClip = AnimationClipPlayable.Create(botPlayables.playableGraph, swordAttackTwo);
        var swordAttackThreeClip = AnimationClipPlayable.Create(botPlayables.playableGraph, swordAttackThree);
        var spearIdleClip = AnimationClipPlayable.Create(botPlayables.playableGraph, spearIdle);
        var spearRunClip = AnimationClipPlayable.Create(botPlayables.playableGraph, spearRun);
        var spearAttackOneClip = AnimationClipPlayable.Create(botPlayables.playableGraph, spearAttackOne);
        var spearAttackTwoClip = AnimationClipPlayable.Create(botPlayables.playableGraph, spearAttackTwo);
        var healClip = AnimationClipPlayable.Create(botPlayables.playableGraph, healing);
        var repairClip = AnimationClipPlayable.Create(botPlayables.playableGraph, repairing);
        var trapClip = AnimationClipPlayable.Create(botPlayables.playableGraph, trap);
        var rifleIdleClip    = AnimationClipPlayable.Create(botPlayables.playableGraph, rifleIdle);
        var rifleRunClip     = AnimationClipPlayable.Create(botPlayables.playableGraph, rifleRun);
        var rifleAimClip     = AnimationClipPlayable.Create(botPlayables.playableGraph, rifleAim);
        var rifleCockingClip = AnimationClipPlayable.Create(botPlayables.playableGraph, rifleCocking != null ? rifleCocking : rifleReload);
        var rifleShootClip   = AnimationClipPlayable.Create(botPlayables.playableGraph, rifleShoot);
        var rifleReloadClip  = AnimationClipPlayable.Create(botPlayables.playableGraph, rifleReload);
        var bowIdleClip      = AnimationClipPlayable.Create(botPlayables.playableGraph, bowIdle);
        var bowRunClip       = AnimationClipPlayable.Create(botPlayables.playableGraph, bowRun);
        var bowDrawArrowClip = AnimationClipPlayable.Create(botPlayables.playableGraph, bowDrawArrow);
        var bowChargeClip    = AnimationClipPlayable.Create(botPlayables.playableGraph, bowCharge);
        var bowShotClip      = AnimationClipPlayable.Create(botPlayables.playableGraph, bowShot);

        botPlayables.playableGraph.Connect(idleClip, 0, mixerPlayable, 1);
        botPlayables.playableGraph.Connect(hitClip, 0, mixerPlayable, 2);
        botPlayables.playableGraph.Connect(staggerClip, 0, mixerPlayable, 3);
        botPlayables.playableGraph.Connect(gettingUpClip, 0, mixerPlayable, 4);
        botPlayables.playableGraph.Connect(fallingClip, 0, mixerPlayable, 5);
        botPlayables.playableGraph.Connect(deathClip, 0, mixerPlayable, 6);
        botPlayables.playableGraph.Connect(runClip, 0, mixerPlayable, 7);
        botPlayables.playableGraph.Connect(firstPunchClip, 0, mixerPlayable, 8);
        botPlayables.playableGraph.Connect(secondPunchClip, 0, mixerPlayable, 9);
        botPlayables.playableGraph.Connect(lastPunchClip, 0, mixerPlayable, 10);
        botPlayables.playableGraph.Connect(swordIdleClip, 0, mixerPlayable, 11);
        botPlayables.playableGraph.Connect(swordRunClip, 0, mixerPlayable, 12);
        botPlayables.playableGraph.Connect(swordAttackOneClip, 0, mixerPlayable, 13);
        botPlayables.playableGraph.Connect(swordAttackTwoClip, 0, mixerPlayable, 14);
        botPlayables.playableGraph.Connect(swordAttackThreeClip, 0, mixerPlayable, 15);
        botPlayables.playableGraph.Connect(spearIdleClip, 0, mixerPlayable, 16);
        botPlayables.playableGraph.Connect(spearRunClip, 0, mixerPlayable, 17);
        botPlayables.playableGraph.Connect(spearAttackOneClip, 0, mixerPlayable, 18);
        botPlayables.playableGraph.Connect(spearAttackTwoClip, 0, mixerPlayable, 19);
        botPlayables.playableGraph.Connect(healClip,          0, mixerPlayable, 20);
        botPlayables.playableGraph.Connect(repairClip,        0, mixerPlayable, 21);
        botPlayables.playableGraph.Connect(trapClip,          0, mixerPlayable, 22);
        botPlayables.playableGraph.Connect(rifleIdleClip,    0, mixerPlayable, 23);
        botPlayables.playableGraph.Connect(rifleRunClip,     0, mixerPlayable, 24);
        botPlayables.playableGraph.Connect(rifleAimClip,     0, mixerPlayable, 25);
        botPlayables.playableGraph.Connect(rifleCockingClip, 0, mixerPlayable, 26);
        botPlayables.playableGraph.Connect(rifleShootClip,   0, mixerPlayable, 27);
        botPlayables.playableGraph.Connect(rifleReloadClip,  0, mixerPlayable, 28);
        botPlayables.playableGraph.Connect(bowIdleClip,      0, mixerPlayable, 29);
        botPlayables.playableGraph.Connect(bowRunClip,       0, mixerPlayable, 30);
        botPlayables.playableGraph.Connect(bowDrawArrowClip, 0, mixerPlayable, 31);
        botPlayables.playableGraph.Connect(bowChargeClip,    0, mixerPlayable, 32);
        botPlayables.playableGraph.Connect(bowShotClip,      0, mixerPlayable, 33);

        IdlePlayable = new BotIdlePlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false);
        HitPlayable = new BotHitPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "hit", "basic", hit.length, hitClip, true);
        StaggerPlayable = new BotStaggerPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "stagger", "basic", stagger.length, staggerClip, true);
        GettingUpPlayable = new BotGettingUpPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "gettingup", "basic", gettingup.length, gettingUpClip, true);
        FallingPlayable = new BotFallingPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "falling", "basic", falling.length, fallingClip, true);
        DeathPlayable = new BotDeathPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "death", "basic", death.length, deathClip, true);
        RunPlayable = new BotRunPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false);
        FistFirstPunch = new BotFistFirstPunch(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "firstpunch", "basic", firstPunch.length, firstPunchClip, true);
        FistMiddlePunch = new BotFistMiddlePunch(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "middlepunch", "basic", secondPunch.length, secondPunchClip, true);
        FistLastPunch = new BotFistLastPunch(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "lastpunch", "basic", lastPunch.length, lastPunchClip, true);
        SwordIdlePlayable = new BotSwordIdle(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "swordidle", "basic", swordIdle.length, swordIdleClip, false);
        SwordRunPlayable = new BotSwordRunPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "swordrun", "basic", swordRun.length, swordRunClip, false);
        SwordAttackOnePlayable = new BotSwordAttackOne(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "swordAttackOne", "basic", swordAttackOne.length, swordAttackOneClip, true);
        SwordAttackTwoPlayable = new BotSwordAttackTwo(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "swordAttackTwo", "basic", swordAttackTwo.length, swordAttackTwoClip, true);
        SwordAttackThreePlayable = new BotSwordAttackThree(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "swordAttackThree", "basic", swordAttackThree.length, swordAttackThreeClip, true);
        SpearIdle = new BotSpearIdle(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "spearidle", "basic", spearIdle.length, spearIdleClip, false);
        SpearRun = new BotSpearRun(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "spearrun", "basic", spearRun.length, spearRunClip, false);
        SpearAttackOne = new BotSpearAttackOne(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "spearAttackOne", "basic", spearAttackOne.length, spearAttackOneClip, true);
        SpearAttackTwo = new BotSpearAttackTwo(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "spearAttackTwo", "basic", spearAttackTwo.length, spearAttackTwoClip, true);
        HealingPlayable = new BotHealingPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "healing", "basic", healing.length, healClip, true);
        RepairArmorPlayable = new BotRepairArmorPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "repairing", "basic", repairing.length, repairClip, true);
        TrapPlayable = new BotTrapPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "trap", "basic", trap.length, trapClip, true);
        RifleIdlePlayable    = new BotRifleIdlePlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "rifleidle",    "basic", rifleIdle.length,    rifleIdleClip,    false);
        RifleRunPlayable     = new BotRifleRunPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "riflerun",     "basic", rifleRun.length,     rifleRunClip,     false);
        RifleAimPlayable     = new BotRifleAimPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "rifleaim",     "basic", rifleAim.length,     rifleAimClip,     false);
        RifleCockingPlayable = new BotRifleCockingPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "riflecocking", "basic", (rifleCocking != null ? rifleCocking.length : rifleReload.length), rifleCockingClip, true);
        RifleShootPlayable   = new BotRifleShootPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "rifleshoot",    "basic", rifleShoot.length,   rifleShootClip,   true);
        RifleReloadPlayable  = new BotRifleReloadPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "riflereload",   "basic", rifleReload.length,  rifleReloadClip,  true);
        BowIdlePlayable      = new BotBowIdlePlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "bowidle",        "basic", bowIdle.length,      bowIdleClip,      false);
        BowRunPlayable       = new BotBowRunPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "bowrun",         "basic", bowRun.length,       bowRunClip,       false);
        BowDrawArrowPlayable = new BotBowDrawArrowPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "bowdrawarrow",  "basic", bowDrawArrow.length, bowDrawArrowClip, true);
        BowChargePlayable    = new BotBowChargePlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "bowcharge",      "basic", bowCharge.length,    bowChargeClip,    false);
        BowShotPlayable      = new BotBowShotPlayable(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "bowshot",        "basic", bowShot.length,      bowShotClip,      true);

        return mixerPlayable;
    }


    public BotAnimationPlayable GetPlayableAnimation(int index)
    {
        switch (index)
        {
            case 1:
                return IdlePlayable;
            case 2:
                return HitPlayable;
            case 3:
                return StaggerPlayable;
            case 4:
                return GettingUpPlayable;
            case 5:
                return FallingPlayable;
            case 6:
                return DeathPlayable;
            case 7: 
                return RunPlayable;
            case 8:
                return FistFirstPunch;
            case 9:
                return FistMiddlePunch;
            case 10:
                return FistLastPunch;
            case 11:
                return SwordIdlePlayable;
            case 12:
                return SwordRunPlayable;
            case 13:
                return SwordAttackOnePlayable;
            case 14:
                return SwordAttackTwoPlayable;
            case 15:
                return SwordAttackThreePlayable;
            case 16:
                return SpearIdle;
            case 17:
                return SpearRun;
            case 18:
                return SpearAttackOne;
            case 19:
                return SpearAttackTwo;
            case 20: return HealingPlayable;
            case 21: return RepairArmorPlayable;
            case 22: return TrapPlayable;
            case 23: return RifleIdlePlayable;
            case 24: return RifleRunPlayable;
            case 25: return RifleAimPlayable;
            case 26: return RifleCockingPlayable;
            case 27: return RifleShootPlayable;
            case 28: return RifleReloadPlayable;
            case 29: return BowIdlePlayable;
            case 30: return BowRunPlayable;
            case 31: return BowDrawArrowPlayable;
            case 32: return BowChargePlayable;
            case 33: return BowShotPlayable;
            default: return null;
        }
    }
}
