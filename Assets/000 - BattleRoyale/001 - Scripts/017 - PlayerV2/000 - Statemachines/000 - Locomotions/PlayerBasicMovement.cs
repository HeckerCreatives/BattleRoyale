using Fusion;
using Fusion.Addons.SimpleKCC;
using NUnit.Framework.Constraints;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerBasicMovement : NetworkBehaviour
{
    [SerializeField] private SimpleKCC simpleKCC;
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private PlayerMovementV2 playerMovementV2;
    [SerializeField] private PlayerOwnObjectEnabler playerOwnObjectEnabler;

    [Space]
    public bool isLowerBody;

    [Space]
    [SerializeField] private List<string> animationnames;
    [SerializeField] private List<string> mixernames;

    [Space]
    [SerializeField] private AnimationClip idle;
    [SerializeField] private AnimationClip run;
    [SerializeField] private AnimationClip sprint;
    [SerializeField] private AnimationClip roll;
    [SerializeField] private AnimationClip punch1;
    [SerializeField] private AnimationClip punch2;
    [SerializeField] private AnimationClip punch3;
    [SerializeField] private AnimationClip startJump;
    [SerializeField] private AnimationClip jumpidle;
    [SerializeField] private AnimationClip falling;
    [SerializeField] private AnimationClip jumpPunch;
    [SerializeField] private AnimationClip block;
    [SerializeField] private AnimationClip hit;
    [SerializeField] private AnimationClip staggerHit;
    [SerializeField] private AnimationClip gettingUp;
    [SerializeField] private AnimationClip death;
    [SerializeField] private AnimationClip heal;
    [SerializeField] private AnimationClip trapping;
    [SerializeField] private AnimationClip swordIdle;
    [SerializeField] private AnimationClip swordRun;
    [SerializeField] private AnimationClip swordFirstAttack;
    [SerializeField] private AnimationClip swordSecondAttack;
    [SerializeField] private AnimationClip swordFinalAttack;
    [SerializeField] private AnimationClip swordSprint;
    [SerializeField] private AnimationClip swordBlock;
    [SerializeField] private AnimationClip swordJumpSlash;
    [SerializeField] private AnimationClip spearIdle;
    [SerializeField] private AnimationClip spearFirstAttack;
    [SerializeField] private AnimationClip spearFinalAttack;
    [SerializeField] private AnimationClip spearJumpAttack;
    [SerializeField] private AnimationClip rifleIdle;
    [SerializeField] private AnimationClip rifleRun;
    [SerializeField] private AnimationClip rifleSprint;
    [SerializeField] private AnimationClip rifleCocking;
    [SerializeField] private AnimationClip rifleShoot;
    [SerializeField] private AnimationClip rifleReload;
    [SerializeField] private AnimationClip rifleJumpFalling;
    [SerializeField] private AnimationClip bowIdle;
    [SerializeField] private AnimationClip bowRun;
    [SerializeField] private AnimationClip bowSprint;
    [SerializeField] private AnimationClip bowDrawArrow;
    [SerializeField] private AnimationClip bowCharge;
    [SerializeField] private AnimationClip bowShot;

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float attackRadius;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    //  ======================

    public AnimationMixerPlayable mixerPlayable;
    //private List<AnimationClipPlayable> clipPlayables;

    public IdleState IdlePlayable { get; private set; }
    public RunState RunPlayable { get; private set; }
    public SprintState SprintPlayable { get; private set; }
    public JumpState JumpPlayable { get; private set; }
    public FallingState FallingPlayable { get; private set; }
    public RollState RollPlayable { get; private set; }
    public PunchState Punch1Playable { get; private set; }
    public MiddlePunchState Punch2Playable { get; private set; }
    public FinalPunchState Punch3Playable { get; private set; }
    public JumpPunchState JumpPunchPlayable { get; private set; }
    public BlockState BlockPlayable { get; private set; }
    public HitState HitPlayable { get; private set; }
    public StaggerHit StaggerHitPlayable { get; private set; }
    public GettingUp GettingUpPlayable { get; private set; }
    public DeathState DeathPlayable { get; private set; }
    public HealState HealPlayable { get; private set; }
    public RepairState RepairPlayable { get; private set; }
    public TrappingState TrappingPlayable { get; private set; }
    public MiddleHitState MiddleHitPlayable { get; private set; }
    public SwordIdleState SwordIdlePlayable { get; private set; }
    public SwordRunState SwordRunPlayable { get; private set; }
    public SwordFirstAttackState SwordAttackFirstPlayable { get; private set; }
    public SwordSecondAttackState SwordAttackSecondPlayable { get; private set; }
    public SwordFinalAttackState SwordFinalAttackPlayable { get; private set; }
    public SwordSprintState SwordSprintPlayable { get; private set; }
    public BlockState SwordBlockPlayable { get; private set; }
    public SwordJumpAttack SwordJumpAttackPlayable { get; private set; }
    public SpearIdleState SpearIdlePlayable { get; private set; }
    public SpearRunState SpearRunPlayable { get; private set; }
    public SpearSprintState SpearSprintPlayable { get; private set; }
    public SpearFirstAttackState SpearFirstAttackPlayable { get; private set; }
    public SpearFinalAttackState SpearFinalAttackPlayable { get; private set; }
    public BlockState SpearBlockPlayable { get; private set; }
    public SpearJumpAttack SpearJumpAttackPlayable { get; private set; }
    public RifleIdleState RifleIdlePlayable { get ; private set; }
    public RifleRunState RifleRunPlayable { get; private set; }
    public RifleSprintState RifleSprintPlayable { get; private set; }
    public RifleSShootState RifleShootPlayable { get; private set; }
    public RifleCockingState RifleCockingPlayable { get; private set; }
    public RifleReloadState RifleReloadPlayable { get; private set; }
    public FallingRangeState RifleFallingPlayable { get; private set; }
    public JumpRangeState RifleJumpPlayable { get; private set; }
    public BowIdle BowIdlePlayable { get; private set; }
    public BowRun BowRunPlayable { get; private set; }
    public BowSprint BowSprintPlayable { get; private set; }
    public BowDrawArrow BowDrawArrowPlayable { get; private set; }
    public BowCharge BowChargePlayable { get; private set; }
    public BowShot BowShotPlayable { get; private set; }

    //  ======================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 50);

        var idleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, idle);
        var runClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, run);
        var sprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, sprint);
        var startJumpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, startJump);
        var idleJumpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, jumpidle);
        var fallingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, falling);
        var rollClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, roll);
        var punch1Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch1);
        var punch2Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch2);
        var punch3Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch3);
        var jumpPunchClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, jumpPunch);
        var blockClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, block);
        var hitClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, hit);
        var staggerHitClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, staggerHit);
        var gettingUpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, gettingUp);
        var deathClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, death);
        var healClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, heal);
        var repairClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, heal);
        var trappingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, trapping);
        var middleHitClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, hit);
        var swordIdleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordIdle);
        var swordRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordRun);
        var swordFirstAttackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordFirstAttack);
        var swordSecondAttackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordSecondAttack);
        var swordFinalAttackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordFinalAttack);
        var swordSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordSprint);
        var swordBlockClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordBlock);
        var swordJumpAttackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordJumpSlash);
        var spearIdleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearIdle);
        var spearRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordRun);
        var spearSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordSprint);
        var spearFirstAattackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearFirstAttack);
        var spearFinalAattackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearFinalAttack);
        var spearBlockClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordBlock);
        var spearJumpAttackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearJumpAttack);
        var rifleIdleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleIdle);
        var rifleRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleRun);
        var rifleSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleSprint);
        var rifleShootClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleShoot);
        var rifleCockingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleCocking);
        var rifleReloadClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleReload);
        var rifleFallingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleJumpFalling);
        var rifleJumpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleJumpFalling);
        var bowIdleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowIdle);
        var bowRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowRun);
        var bowSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowSprint);
        var bowDrawArrowClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowDrawArrow);
        var bowChargeClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowCharge);
        var bowShotClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowShot);

        playerPlayables.playableGraph.Connect(idleClip, 0, mixerPlayable, 1);
        playerPlayables.playableGraph.Connect(runClip, 0, mixerPlayable, 2);
        playerPlayables.playableGraph.Connect(sprintClip, 0, mixerPlayable, 3);
        playerPlayables.playableGraph.Connect(rollClip, 0, mixerPlayable, 4);
        playerPlayables.playableGraph.Connect(punch1Clip, 0, mixerPlayable, 5);
        playerPlayables.playableGraph.Connect(punch2Clip, 0, mixerPlayable, 6);
        playerPlayables.playableGraph.Connect(punch3Clip, 0, mixerPlayable, 7);
        playerPlayables.playableGraph.Connect(startJumpClip, 0, mixerPlayable, 8);
        playerPlayables.playableGraph.Connect(idleJumpClip, 0, mixerPlayable, 9);
        playerPlayables.playableGraph.Connect(fallingClip, 0, mixerPlayable, 10);
        playerPlayables.playableGraph.Connect(jumpPunchClip, 0, mixerPlayable, 11);
        playerPlayables.playableGraph.Connect(blockClip, 0, mixerPlayable, 12);
        playerPlayables.playableGraph.Connect(hitClip, 0, mixerPlayable, 13);
        playerPlayables.playableGraph.Connect(staggerHitClip, 0, mixerPlayable, 14);
        playerPlayables.playableGraph.Connect(gettingUpClip, 0, mixerPlayable, 15);
        playerPlayables.playableGraph.Connect(deathClip, 0, mixerPlayable, 16);
        playerPlayables.playableGraph.Connect(healClip, 0, mixerPlayable, 17);
        playerPlayables.playableGraph.Connect(repairClip, 0, mixerPlayable, 18);
        playerPlayables.playableGraph.Connect(trappingClip, 0, mixerPlayable, 19);
        playerPlayables.playableGraph.Connect(middleHitClip, 0, mixerPlayable, 20);
        playerPlayables.playableGraph.Connect(swordIdleClip, 0, mixerPlayable, 21);
        playerPlayables.playableGraph.Connect(swordRunClip, 0, mixerPlayable, 22);
        playerPlayables.playableGraph.Connect(swordFirstAttackClip, 0, mixerPlayable, 23);
        playerPlayables.playableGraph.Connect(swordSecondAttackClip, 0, mixerPlayable, 24);
        playerPlayables.playableGraph.Connect(swordFinalAttackClip, 0, mixerPlayable, 25);
        playerPlayables.playableGraph.Connect(swordSprintClip, 0, mixerPlayable, 26);
        playerPlayables.playableGraph.Connect(swordBlockClip, 0, mixerPlayable, 27);
        playerPlayables.playableGraph.Connect(swordJumpAttackClip, 0, mixerPlayable, 28);
        playerPlayables.playableGraph.Connect(spearIdleClip, 0, mixerPlayable, 29);
        playerPlayables.playableGraph.Connect(spearRunClip, 0, mixerPlayable, 30);
        playerPlayables.playableGraph.Connect(spearSprintClip, 0, mixerPlayable, 31);
        playerPlayables.playableGraph.Connect(spearFirstAattackClip, 0, mixerPlayable, 32);
        playerPlayables.playableGraph.Connect(spearFinalAattackClip, 0, mixerPlayable, 33);
        playerPlayables.playableGraph.Connect(spearBlockClip, 0, mixerPlayable, 34);
        playerPlayables.playableGraph.Connect(spearJumpAttackClip, 0, mixerPlayable, 35);
        playerPlayables.playableGraph.Connect(rifleIdleClip, 0, mixerPlayable, 36);
        playerPlayables.playableGraph.Connect(rifleRunClip, 0, mixerPlayable, 37);
        playerPlayables.playableGraph.Connect(rifleSprintClip, 0, mixerPlayable, 38);
        playerPlayables.playableGraph.Connect(rifleShootClip, 0, mixerPlayable, 39);
        playerPlayables.playableGraph.Connect(rifleCockingClip, 0, mixerPlayable, 40);
        playerPlayables.playableGraph.Connect(rifleReloadClip, 0, mixerPlayable, 41);
        playerPlayables.playableGraph.Connect(rifleFallingClip, 0, mixerPlayable, 42);
        playerPlayables.playableGraph.Connect(rifleJumpClip, 0, mixerPlayable, 43);
        playerPlayables.playableGraph.Connect(bowIdleClip, 0, mixerPlayable, 44);
        playerPlayables.playableGraph.Connect(bowRunClip, 0, mixerPlayable, 45);
        playerPlayables.playableGraph.Connect(bowSprintClip, 0, mixerPlayable, 46);
        playerPlayables.playableGraph.Connect(bowDrawArrowClip, 0, mixerPlayable, 47);
        playerPlayables.playableGraph.Connect(bowChargeClip, 0, mixerPlayable, 48);
        playerPlayables.playableGraph.Connect(bowShotClip, 0, mixerPlayable, 49);

        PlayablesChanger changer = playerPlayables.lowerBodyChanger;

        #region GLOBAL

        FallingPlayable = new FallingState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "falling", "basic", falling.length, fallingClip, false, isLowerBody);
        RollPlayable = new RollState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic", roll.length, rollClip, true, isLowerBody);
        HitPlayable = new HitState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "hit", "basic", hit.length, hitClip, true, isLowerBody  );
        StaggerHitPlayable = new StaggerHit(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "staggerhit", "basic", staggerHit.length, staggerHitClip, true, isLowerBody);
        GettingUpPlayable = new GettingUp(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "gettingup", "basic", gettingUp.length, gettingUpClip, true, isLowerBody);
        DeathPlayable = new DeathState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "death", "basic", death.length, deathClip, true, isLowerBody);
        HealPlayable = new HealState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "heal", "basic", heal.length, healClip, true, isLowerBody);
        RepairPlayable = new RepairState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "repair", "basic", heal.length, repairClip, true, isLowerBody);
        TrappingPlayable = new TrappingState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "trapping", "basic", trapping.length, trappingClip, true, isLowerBody);
        MiddleHitPlayable = new MiddleHitState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "middlehit", "basic", hit.length, hitClip, true, isLowerBody);

        #endregion

        #region BASIC

        IdlePlayable = new IdleState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false, isLowerBody);
        RunPlayable = new RunState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false, isLowerBody);
        SprintPlayable = new SprintState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic", sprint.length, sprintClip, false, isLowerBody);
        JumpPlayable = new JumpState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumpidle", "basic", jumpidle.length, idleJumpClip, false, isLowerBody);
        BlockPlayable = new BlockState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "block", "basic", block.length, blockClip, true, isLowerBody);
        Punch1Playable = new PunchState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch1", "basic", punch1.length, punch1Clip, true, isLowerBody);
        Punch2Playable = new MiddlePunchState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch2", "basic", punch2.length, punch2Clip, true, isLowerBody);
        Punch3Playable = new FinalPunchState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch3", "basic", punch3.length, punch3Clip, true, isLowerBody);
        JumpPunchPlayable = new JumpPunchState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumppunch", "basic", jumpPunch.length, jumpPunchClip, true, isLowerBody);

        #endregion

        #region SWORD

        SwordIdlePlayable = new SwordIdleState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordidle", "basic", swordIdle.length, swordIdleClip, false, isLowerBody);
        SwordRunPlayable = new SwordRunState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordrun", "basic", swordRun.length, swordRunClip, false, isLowerBody);
        SwordAttackFirstPlayable = new SwordFirstAttackState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfirstattack", "basic", swordFirstAttack.length, swordFirstAttackClip, true, isLowerBody);
        SwordAttackSecondPlayable = new SwordSecondAttackState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsecondattack", "basic", swordSecondAttack.length, swordSecondAttackClip, true, isLowerBody);
        SwordFinalAttackPlayable = new SwordFinalAttackState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfinalattack", "basic", swordFinalAttack.length, swordFinalAttackClip, true, isLowerBody);
        SwordSprintPlayable = new SwordSprintState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsprint", "basic", swordSprint.length, swordSprintClip, false, isLowerBody);
        SwordBlockPlayable = new BlockState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordblock", "basic", swordBlock.length, swordBlockClip, true, isLowerBody);
        SwordJumpAttackPlayable = new SwordJumpAttack(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordjumpattack", "basic", swordJumpSlash.length, swordJumpAttackClip, true, isLowerBody);

        #endregion

        #region SPEAR

        SpearIdlePlayable = new SpearIdleState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearidle", "basic", spearIdle.length, spearIdleClip, false, isLowerBody);
        SpearRunPlayable = new SpearRunState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearrun", "basic", swordRun.length, spearRunClip, false, isLowerBody);
        SpearSprintPlayable = new SpearSprintState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearsprint", "basic", swordSprint.length, spearSprintClip, false, isLowerBody);
        SpearFirstAttackPlayable = new SpearFirstAttackState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfirstattack", "basic", spearFirstAttack.length, spearFirstAattackClip, true, isLowerBody);
        SpearFinalAttackPlayable = new SpearFinalAttackState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfinalattack", "basic", spearFinalAttack.length, spearFinalAattackClip, true, isLowerBody);
        SpearBlockPlayable = new BlockState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearblock", "basic", swordBlock.length, spearBlockClip, true, isLowerBody);
        SpearJumpAttackPlayable = new SpearJumpAttack(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearjumpattack", "basic", spearJumpAttack.length, spearJumpAttackClip, true, isLowerBody);

        #endregion

        #region RIFLE

        RifleIdlePlayable = new RifleIdleState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleidle", "basic", rifleIdle.length, rifleIdleClip, false, isLowerBody);
        RifleRunPlayable = new RifleRunState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflerun", "basic", rifleRun.length, rifleRunClip, false, isLowerBody);
        RifleSprintPlayable = new RifleSprintState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflesprint", "basic", rifleSprint.length, rifleSprintClip, false, isLowerBody);
        RifleShootPlayable = new RifleSShootState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleshoot", "basic", rifleShoot.length, rifleShootClip, true, isLowerBody);
        RifleCockingPlayable = new RifleCockingState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflecock", "basic", rifleCocking.length, rifleCockingClip, true, isLowerBody);
        RifleReloadPlayable = new RifleReloadState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflereload", "basic", rifleReload.length, rifleReloadClip, true, isLowerBody);
        RifleFallingPlayable = new FallingRangeState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflefalling", "basic", falling.length, rifleFallingClip, false, isLowerBody);
        RifleJumpPlayable = new JumpRangeState(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflejump", "basic", jumpidle.length, rifleJumpClip, false, isLowerBody);

        #endregion

        #region BOW

        BowIdlePlayable = new BowIdle(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowidle", "basic", bowIdle.length, bowIdleClip, false, isLowerBody);
        BowRunPlayable = new BowRun(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowrun", "basic", bowRun.length, bowRunClip, false, isLowerBody);
        BowSprintPlayable = new BowSprint(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowsprint", "basic", bowSprint.length, bowSprintClip, false, isLowerBody);
        BowDrawArrowPlayable = new BowDrawArrow(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowdrawarrow", "basic", bowDrawArrow.length, bowDrawArrowClip, true, isLowerBody);
        BowChargePlayable = new BowCharge(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowcharge", "basic", bowCharge.length, bowChargeClip, true, isLowerBody);
        BowShotPlayable = new BowShot(this, simpleKCC, changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowshot", "basic", bowShot.length, bowShotClip, true, isLowerBody);

        #endregion

        return mixerPlayable;
    }

    public AnimationPlayable GetPlayableAnimation(int index)
    {
        switch (index)
        {
            case 1:
                return IdlePlayable;
            case 2:
                return RunPlayable;
            case 3:
                return SprintPlayable;
            case 4:
                return RollPlayable;
            case 5:
                return Punch1Playable;
            case 6:
                return Punch2Playable;
            case 7:
                return Punch3Playable;
            case 9:
                return JumpPlayable;
            case 10:
                return FallingPlayable;
            case 11:
                return JumpPunchPlayable;
            case 12:
                return BlockPlayable;
            case 13:
                return HitPlayable;
            case 14:
                return StaggerHitPlayable;
            case 15:
                return GettingUpPlayable;
            case 16:
                return DeathPlayable;
            case 17:
                return HealPlayable;
            case 18:
                return RepairPlayable;
            case 19:
                return TrappingPlayable;
            case 20:
                return MiddleHitPlayable;
            case 21:
                return SwordIdlePlayable;
            case 22:
                return SwordRunPlayable;
            case 23:
                return SwordAttackFirstPlayable;
            case 24:
                return SwordAttackSecondPlayable;
            case 25:
                return SwordFinalAttackPlayable;
            case 26:
                return SwordSprintPlayable;
            case 27:
                return SwordBlockPlayable;
            case 28:
                return SwordJumpAttackPlayable;
            case 29:
                return SpearIdlePlayable;
            case 30:
                return SpearRunPlayable;
            case 31:
                return SpearSprintPlayable;
            case 32:
                return SpearFirstAttackPlayable;
            case 33:
                return SpearFinalAttackPlayable;
            case 34:
                return SpearBlockPlayable;
            case 35:
                return SpearJumpAttackPlayable;
            case 36:
                return RifleIdlePlayable;
            case 37:
                return RifleRunPlayable;
            case 38:
                return RifleSprintPlayable;
            case 39:
                return RifleShootPlayable;
            case 40:
                return RifleReloadPlayable;
            case 41:
                return RifleReloadPlayable;
            case 42:
                return RifleFallingPlayable;
            case 43:
                return RifleJumpPlayable;
            case 44:
                return BowIdlePlayable;
            case 45:
                return BowRunPlayable;
            case 46:
                return BowSprintPlayable;
            case 47:
                return BowDrawArrowPlayable;
            case 48:
                return BowChargePlayable;
            case 49:
                return BowShotPlayable;
            default: return null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(impactFirstFistPoint.position, attackRadius);


        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(impactSecondFistPoint.position, attackRadius);
    }
}
