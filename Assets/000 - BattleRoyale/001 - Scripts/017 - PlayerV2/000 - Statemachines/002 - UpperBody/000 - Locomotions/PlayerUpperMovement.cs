using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Experimental.AI;
using UnityEngine.Playables;
using static UnityEngine.Rendering.PostProcessing.HistogramMonitor;
using static UnityEngine.Rendering.PostProcessing.PostProcessResources;

public class PlayerUpperMovement : NetworkBehaviour
{
    [SerializeField] private SimpleKCC simpleKCC;
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private PlayerMovementV2 playerMovementV2;
    [SerializeField] private PlayerOwnObjectEnabler playerOwnObjectEnabler;

    [Space]
    public bool isLowerBody;
    [SerializeField] private float shaker;

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
    [SerializeField] private AnimationClip rifleAim;
    [SerializeField] private AnimationClip bowIdle;
    [SerializeField] private AnimationClip bowRun;
    [SerializeField] private AnimationClip bowSprint;
    [SerializeField] private AnimationClip bowDrawArrow;
    [SerializeField] private AnimationClip bowCharge;
    [SerializeField] private AnimationClip bowShot;
    [SerializeField] private AnimationClip stopSprint;

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float attackRadius;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    [field: Space]
    [Networked][field: SerializeField] public int Hitted { get; set; }
    [Networked] public Vector3 HittedPosition { get; set; }

    //  =====================

    private readonly HashSet<NetworkObject> hitEnemiesFirstFist = new();
    private readonly HashSet<NetworkObject> hitEnemiesSecondFist = new();

    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();

    //  ======================

    public AnimationMixerPlayable mixerPlayable;

    public PlayerUpperIdle IdlePlayables;
    public PlayerUpperRun RunPlayables;
    public PlayerFistFirstPunch FirstPunch;
    public PlayerFistSecondPunch SecondPunch;
    public PlayerFistFinalPunch FinalPunch;
    public PlayerUpperSprint SprintPlayables;
    public PlayerUpperRoll RollPlayables;
    public PlayerUpperFalling FallingPlayables;
    public PlayerUpperDeath DeathPlayable;
    public PlayerUpperHit HitPlayable;
    public PlayerUpperJump JumpPlayable;
    public PlayerUpperStagger StaggerHitPlayable;
    public PlayerUpperGettingUp GettingUpPlayable;
    public PlayerJumpPunchPlayable JumpPunchPlayable;
    public PlayerBlockPlayable BlockPlayable;
    public PlayerHealPlayable HealPlayable;
    public PlayerUpperRepair RepairPlayable;
    public PlayerUpperTrap TrapPlayable;
    public PlayerMiddleHit MiddleHitPlayable;
    public PlayerUpperSwordIdle SwordIdlePlayable;
    public PlayerUpperSwordRun SwordRunPlayable;
    public PlayerUpperSwordFirstAttack SwordAttackFirstPlayable;
    public PlayerUpperSwordMiddleAttack SwordAttackSecondPlayable;
    public PlayerUpperSwordFinalAttack SwordFinalAttackPlayable;
    public PlayerUpperSwordSprint SwordSprint;
    public PlayerBlockPlayable SwordBlockPlayable;
    public PlayerUpperSwordJumpAttack SwordJumpAttackPlayable;
    public PlayerUpperSpearIdle SpearIdle;
    public PlayerUpperSpearRun SpearRunPlayable;
    public PlayerUpperSpearSprint SpearSprintPlayable;
    public PlayerUpperSpearFirstAttack SpearFirstAttackPlayable;
    public PlayerUpperSpearFinalAttack SpearFinalAttackPlayable;
    public PlayerUpperSpearJumpAttack SpearJumpAttackPlayable;
    public PlayerBlockPlayable SpearBlockPlayable;
    public PlayerUpperRifleIdle RifleIdle;
    public PlayerUpperRifleRun RifleRunPlayable;
    public PlayerUpperRifleSprint RifleSprintPlayable;
    public PlayerUpperRifleShoot RifleShootPlayable;
    public PlayerUpperRifleCocking RifleCockingPlayable;
    public PlayerUpperRifleReload RifleReloadPlayable;
    public PlayerUpperRifleAim RifleAimPlayable;
    public PlayerUpperJumpRange RifleJumpPlayable;
    public PlayerUpperFallingRange RifleFallingPlayable;
    public PlayerUpperBowIdle BowIdlePlayable;
    public PlayerUpperBowRun BowRunPlayable;
    public PlayerUpperBowSprint BowSprintPlayable;
    public PlayerUpperBowDraw BowDrawArrowPlayable;
    public PlayerUpperBowCharge BowChargePlayable;
    public PlayerUpperBowShot BowShotPlayable;
    public PlayerUpperJumpRange BowJumpPlayable;
    public PlayerUpperFallingRange BowFallingPlayable;
    public PlayerUpperStopSprint StopSprintPlayable;

    //  ======================

    private ChangeDetector _changeDetector;

    Coroutine ShakerCoroutine;

    //  ======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
    public override void Render()
    {
        if (HasStateAuthority) return;

        ChangeDetectorUpdate();
    }

    private void ChangeDetectorUpdate()
    {
        if (!HasInputAuthority) return;


        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Hitted):

                    if (ShakerCoroutine != null) StopCoroutine(ShakerCoroutine);

                    ShakerCoroutine = StartCoroutine(Shaker());
                    playerPlayables.RegisterComboHit(HittedPosition);

                    break;
            }
        }
    }

    IEnumerator Shaker()
    {
        playerPlayables.CameraShaker(shaker);
        yield return new WaitForSecondsRealtime(0.15f);
        playerPlayables.CameraShaker(0f);
    }

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 53);

        var idleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, idle);
        var runClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, run);
        var punch1Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch1);
        var punch2Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch2);
        var punch3Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch3);
        var sprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, sprint);
        var rollClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, roll);
        var fallingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, falling);
        var deathClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, death);
        var hitClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, hit);
        var jumpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, startJump);
        var staggerHitClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, staggerHit);
        var gettingUpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, gettingUp);
        var jumpPunchClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, jumpPunch);
        var blockClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, block);
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
        var spearSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordSprint);
        var spearRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordRun);
        var spearBlockClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordBlock);
        var spearFirstAattackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearFirstAttack);
        var spearFinalAattackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearFinalAttack);
        var spearJumpAttackClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, spearJumpAttack);
        var rifleIdleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleIdle);
        var rifleRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleRun);
        var rifleSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleSprint);
        var rifleShootClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleShoot);
        var rifleCockingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleCocking);
        var rifleReloadClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleReload);
        var rifleJumpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleRun);
        var rifleFallingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleRun);
        var bowIdleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowIdle);
        var bowRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowRun);
        var bowSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowSprint);
        var bowDrawArrowClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowDrawArrow);
        var bowChargeClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowCharge);
        var bowShotClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, bowShot);
        var bowJumpClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, jumpidle);
        var bowFallingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, falling);
        var stopSprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, stopSprint);
        var rifleAimClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, rifleAim);

        playerPlayables.playableGraph.Connect(idleClip, 0, mixerPlayable, 1);
        playerPlayables.playableGraph.Connect(runClip, 0, mixerPlayable, 2);
        playerPlayables.playableGraph.Connect(punch1Clip, 0, mixerPlayable, 3);
        playerPlayables.playableGraph.Connect(punch2Clip, 0, mixerPlayable, 4);
        playerPlayables.playableGraph.Connect(punch3Clip, 0, mixerPlayable, 5);
        playerPlayables.playableGraph.Connect(sprintClip, 0, mixerPlayable, 6);
        playerPlayables.playableGraph.Connect(rollClip, 0, mixerPlayable, 7);
        playerPlayables.playableGraph.Connect(fallingClip, 0, mixerPlayable, 8);
        playerPlayables.playableGraph.Connect(deathClip, 0, mixerPlayable, 9);
        playerPlayables.playableGraph.Connect(hitClip, 0, mixerPlayable, 10);
        playerPlayables.playableGraph.Connect(jumpClip, 0, mixerPlayable, 11);
        playerPlayables.playableGraph.Connect(staggerHitClip, 0, mixerPlayable, 12);
        playerPlayables.playableGraph.Connect(gettingUpClip, 0, mixerPlayable, 13);
        playerPlayables.playableGraph.Connect(jumpPunchClip, 0, mixerPlayable, 14);
        playerPlayables.playableGraph.Connect(blockClip, 0, mixerPlayable, 15);
        playerPlayables.playableGraph.Connect(healClip, 0, mixerPlayable, 16);
        playerPlayables.playableGraph.Connect(repairClip, 0, mixerPlayable, 17);
        playerPlayables.playableGraph.Connect(trappingClip, 0, mixerPlayable, 18);
        playerPlayables.playableGraph.Connect(middleHitClip, 0, mixerPlayable, 19);
        playerPlayables.playableGraph.Connect(swordIdleClip, 0, mixerPlayable, 20);
        playerPlayables.playableGraph.Connect(swordRunClip, 0, mixerPlayable, 21);
        playerPlayables.playableGraph.Connect(swordFirstAttackClip, 0, mixerPlayable, 22);
        playerPlayables.playableGraph.Connect(swordSecondAttackClip, 0, mixerPlayable, 23);
        playerPlayables.playableGraph.Connect(swordFinalAttackClip, 0, mixerPlayable, 24);
        playerPlayables.playableGraph.Connect(swordSprintClip, 0, mixerPlayable, 25);
        playerPlayables.playableGraph.Connect(swordBlockClip, 0, mixerPlayable, 26);
        playerPlayables.playableGraph.Connect(swordJumpAttackClip, 0, mixerPlayable, 27);
        playerPlayables.playableGraph.Connect(spearIdleClip, 0, mixerPlayable, 28);
        playerPlayables.playableGraph.Connect(spearSprintClip, 0, mixerPlayable, 29);
        playerPlayables.playableGraph.Connect(spearRunClip, 0, mixerPlayable, 30);
        playerPlayables.playableGraph.Connect(spearBlockClip, 0, mixerPlayable, 31);
        playerPlayables.playableGraph.Connect(spearFirstAattackClip, 0, mixerPlayable, 32);
        playerPlayables.playableGraph.Connect(spearFinalAattackClip, 0, mixerPlayable, 33);
        playerPlayables.playableGraph.Connect(spearJumpAttackClip, 0, mixerPlayable, 34);
        playerPlayables.playableGraph.Connect(rifleIdleClip, 0, mixerPlayable, 35);
        playerPlayables.playableGraph.Connect(rifleRunClip, 0, mixerPlayable, 36);
        playerPlayables.playableGraph.Connect(rifleSprintClip, 0, mixerPlayable, 37);
        playerPlayables.playableGraph.Connect(rifleShootClip, 0, mixerPlayable, 38);
        playerPlayables.playableGraph.Connect(rifleCockingClip, 0, mixerPlayable, 39);
        playerPlayables.playableGraph.Connect(rifleReloadClip, 0, mixerPlayable, 40);
        playerPlayables.playableGraph.Connect(rifleJumpClip, 0, mixerPlayable, 41);
        playerPlayables.playableGraph.Connect(rifleFallingClip, 0, mixerPlayable, 42);
        playerPlayables.playableGraph.Connect(bowIdleClip, 0, mixerPlayable, 43);
        playerPlayables.playableGraph.Connect(bowRunClip, 0, mixerPlayable, 44);
        playerPlayables.playableGraph.Connect(bowSprintClip, 0, mixerPlayable, 45);
        playerPlayables.playableGraph.Connect(bowDrawArrowClip, 0, mixerPlayable, 46);
        playerPlayables.playableGraph.Connect(bowChargeClip, 0, mixerPlayable, 47);
        playerPlayables.playableGraph.Connect(bowShotClip, 0, mixerPlayable, 48);
        playerPlayables.playableGraph.Connect(bowJumpClip, 0, mixerPlayable, 49);
        playerPlayables.playableGraph.Connect(bowFallingClip, 0, mixerPlayable, 50);
        playerPlayables.playableGraph.Connect(stopSprintClip, 0, mixerPlayable, 51);
        playerPlayables.playableGraph.Connect(rifleAimClip, 0, mixerPlayable, 52);

        #region GLOBAL

        FallingPlayables = new PlayerUpperFalling(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "falling", "basic", falling.length, fallingClip, true, false);
        RollPlayables = new PlayerUpperRoll(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic", roll.length, rollClip, true, false);
        DeathPlayable = new PlayerUpperDeath(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "death", "basic", death.length, deathClip, true, false);
        HitPlayable = new PlayerUpperHit(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "hit", "basic", hit.length, hitClip, true, false);
        StaggerHitPlayable = new PlayerUpperStagger(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "staggerhit", "basic", staggerHit.length, staggerHitClip, true, false);
        GettingUpPlayable = new PlayerUpperGettingUp(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "gettingup", "basic", gettingUp.length, gettingUpClip, true, false);
        HealPlayable = new PlayerHealPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "heal", "basic", heal.length, healClip, true, false);
        RepairPlayable = new PlayerUpperRepair(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "repair", "basic", heal.length, repairClip, true, false);
        TrapPlayable = new PlayerUpperTrap(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "trapping", "basic", trapping.length, trappingClip, true, false);
        MiddleHitPlayable = new PlayerMiddleHit(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "middlehit", "basic", hit.length, hitClip, true, false);
        StopSprintPlayable = new PlayerUpperStopSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "stopsprint", "basic", hit.length, stopSprintClip, true, false);

        #endregion

        #region BASIC

        IdlePlayables = new PlayerUpperIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false, false);
        RunPlayables = new PlayerUpperRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false, false);
        FirstPunch = new PlayerFistFirstPunch(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch1", "basic", punch1.length, punch1Clip, true, false);
        SecondPunch = new PlayerFistSecondPunch(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch2", "basic", punch2.length, punch2Clip, true, false);
        FinalPunch = new PlayerFistFinalPunch(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch3", "basic", punch3.length, punch3Clip, true, false);
        SprintPlayables = new PlayerUpperSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic", sprint.length, sprintClip, false, false);
        JumpPlayable = new PlayerUpperJump(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumpidle", "basic", startJump.length, jumpClip, true, false);
        BlockPlayable = new PlayerBlockPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "block", "basic", block.length, blockClip, true, false);
        JumpPunchPlayable = new PlayerJumpPunchPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumppunch", "basic", jumpPunch.length, jumpPunchClip, true, false);

        #endregion

        #region SWORD

        SwordIdlePlayable = new PlayerUpperSwordIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordidle", "basic", swordIdle.length, swordIdleClip, false, false);
        SwordRunPlayable = new PlayerUpperSwordRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordrun", "basic", swordRun.length, swordRunClip, false, false);
        SwordAttackFirstPlayable = new PlayerUpperSwordFirstAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfirstattack", "basic", swordFirstAttack.length, swordFirstAttackClip, true, false);
        SwordAttackSecondPlayable = new PlayerUpperSwordMiddleAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsecondattack", "basic", swordSecondAttack.length, swordSecondAttackClip, true, false);
        SwordFinalAttackPlayable = new PlayerUpperSwordFinalAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfinalattack", "basic", swordFinalAttack.length, swordFinalAttackClip, true, false);
        SwordSprint = new PlayerUpperSwordSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsprint", "basic", swordSprint.length, swordSprintClip, false, false);
        SwordBlockPlayable = new PlayerBlockPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordblock", "basic", swordBlock.length, swordBlockClip, true, false);
        SwordJumpAttackPlayable = new PlayerUpperSwordJumpAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordjumpattack", "basic", swordJumpSlash.length, swordJumpAttackClip, true, false);

        #endregion

        #region SPEAR

        SpearIdle = new PlayerUpperSpearIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearidle", "basic", spearIdle.length, spearIdleClip, false, false);
        SpearRunPlayable = new PlayerUpperSpearRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearrun", "basic", swordRun.length, spearRunClip, false, false);
        SpearSprintPlayable = new PlayerUpperSpearSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearsprint", "basic", swordSprint.length, spearSprintClip, false, false);
        SpearFirstAttackPlayable = new PlayerUpperSpearFirstAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfirstattack", "basic", spearFirstAttack.length, spearFirstAattackClip, true, false);
        SpearFinalAttackPlayable = new PlayerUpperSpearFinalAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfinalattack", "basic", spearFinalAttack.length, spearFinalAattackClip, true, false);
        SpearBlockPlayable = new PlayerBlockPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearblock", "basic", swordBlock.length, spearBlockClip, true, false);
        SpearJumpAttackPlayable = new PlayerUpperSpearJumpAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearjumpattack", "basic", spearJumpAttack.length, spearJumpAttackClip, true, false);

        #endregion

        #region RIFLE

        RifleIdle = new PlayerUpperRifleIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleidle", "basic", rifleIdle.length, rifleIdleClip, false, false);
        RifleRunPlayable = new PlayerUpperRifleRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleRun", "basic", rifleRun.length, rifleRunClip, false, false);
        RifleSprintPlayable = new PlayerUpperRifleSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleSprint", "basic", rifleSprint.length, rifleSprintClip, false, false);
        RifleShootPlayable = new PlayerUpperRifleShoot(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleshoot", "basic", rifleShoot.length, rifleShootClip, true, true);
        RifleCockingPlayable = new PlayerUpperRifleCocking(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflecocking", "basic", rifleCocking.length, rifleCockingClip, true, true);
        RifleReloadPlayable = new PlayerUpperRifleReload(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflereload", "basic", rifleReload.length, rifleReloadClip, true, true);
        RifleJumpPlayable = new PlayerUpperJumpRange(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflejump", "basic", rifleRun.length, rifleJumpClip, false, false);
        RifleFallingPlayable = new PlayerUpperFallingRange(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "riflefalling", "basic", rifleRun.length, rifleFallingClip, false, false);
        RifleAimPlayable = new PlayerUpperRifleAim(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "rifleaim", "basic", rifleAim.length, rifleAimClip, false, true);

        #endregion

        #region BOW

        BowIdlePlayable = new PlayerUpperBowIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowidle", "basic", bowIdle.length, bowIdleClip, false  , false);
        BowRunPlayable = new PlayerUpperBowRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowrun", "basic", bowRun.length, bowRunClip, false, false);
        BowSprintPlayable = new PlayerUpperBowSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowsprint", "basic", bowSprint.length, bowSprintClip, false, false);
        BowDrawArrowPlayable = new PlayerUpperBowDraw(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowdrawarrow", "basic", bowDrawArrow.length, bowDrawArrowClip, true, true);
        BowChargePlayable = new PlayerUpperBowCharge(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowcharge", "basic", bowCharge.length, bowChargeClip, true, true);
        BowShotPlayable = new PlayerUpperBowShot(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowshot", "basic", bowShot.length, bowShotClip, true, true);
        BowJumpPlayable = new PlayerUpperJumpRange(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowjump", "basic", jumpidle.length, jumpClip, false, false);
        BowFallingPlayable = new PlayerUpperFallingRange(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "bowfalling", "basic", falling.length, fallingClip, false, false);

        #endregion

        return mixerPlayable;
    }

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
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            if (hitObject.tag == "Bot")
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();

                if (tempdata.IsStagger) return;
                if (tempdata.IsGettingUp) return;
                if (tempdata.IsDead) return;

                if (!hitEnemiesFirstFist.Contains(hitObject))
                {
                    hitEnemiesFirstFist.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    if (isFinal) tempdata.IsStagger = true;
                    else tempdata.IsHit = true;

                    tempdata.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username.ToString(), Object);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();


                // Avoid duplicate hits
                if (!hitEnemiesFirstFist.Contains(hitObject))
                {
                    // Mark as hit
                    hitEnemiesFirstFist.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();

                    if (tempplayables.healthV2.IsStagger) return;
                    if (tempplayables.healthV2.IsGettingUp) return;
                    if (playerMovementV2.Rolling) return;

                    if (isFinal)
                    {
                        tempplayables.ChangeCamera(false);
                        tempplayables.StaggerAnimation();
                    }
                    else
                    {
                        tempplayables.ChangeCamera(false);
                        tempplayables.HitAnimation();
                        hitObject.GetComponent<SimpleKCC>().Move(playerMovementV2.MainCharObj.forward * 100f);
                    }

                    tempplayables.RPC_PlayPunchHit();

                    healthV2.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username.ToString(), Object);

                    Hitted = Runner.Tick;
                    HittedPosition = hitbox.transform.root.position + Vector3.up * 1.2f;
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
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            if (hitObject.tag == "Bot")
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();

                if (tempdata.IsStagger) return;
                if (tempdata.IsGettingUp) return;
                if (tempdata.IsDead) return;

                if (!hitEnemiesSecondFist.Contains(hitObject))
                {
                    hitEnemiesSecondFist.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    tempdata.IsHit = true;

                    tempdata.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username.ToString(), Object);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();


                // Avoid duplicate hits
                if (!hitEnemiesSecondFist.Contains(hitObject))
                {
                    // Mark as hit
                    hitEnemiesSecondFist.Add(hitObject);

                    //bareHandsMovement.CanDamage = false;

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();

                    if (tempplayables.healthV2.IsStagger) return;
                    if (tempplayables.healthV2.IsGettingUp) return;
                    if (playerMovementV2.Rolling) return;

                    hitObject.GetComponent<PlayerPlayables>().HitAnimation();
                    hitObject.GetComponent<SimpleKCC>().Move(playerMovementV2.MainCharObj.forward * 100f);

                    tempplayables.RPC_PlayPunchHit();

                    healthV2.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username.ToString(), Object);

                    Hitted = Runner.Tick;
                    HittedPosition = hitbox.transform.root.position + Vector3.up * 1.2f;
                }
            }
        }
    }

    public void ResetFirstAttack()
    {
        hitEnemiesFirstFist.Clear();
    }

    public void ResetSecondAttack()
    {
        hitEnemiesSecondFist.Clear();
    }

    public UpperBodyAnimations GetPlayableAnimation(int index)
    {
        switch (index)
        {
            case 1:
                return IdlePlayables;
            case 2:
                return RunPlayables;
            case 3:
                return FirstPunch;
            case 4:
                return SecondPunch;
            case 5:
                return FinalPunch;
            case 6:
                return SprintPlayables;
            case 7:
                return RollPlayables;
            case 8:
                return FallingPlayables;
            case 9:
                return DeathPlayable;
            case 10:
                return HitPlayable;
            case 11:
                return JumpPlayable;
            case 12:
                return StaggerHitPlayable;
            case 13:
                return GettingUpPlayable;
            case 14:
                return JumpPunchPlayable;
            case 15:
                return BlockPlayable;
            case 16:
                return HealPlayable;
            case 17:
                return RepairPlayable;
            case 18:
                return TrapPlayable;
            case 19:
                return MiddleHitPlayable;
            case 20:
                return SwordIdlePlayable;
            case 21:
                return SwordRunPlayable;
            case 22:
                return SwordAttackFirstPlayable;
            case 23:
                return SwordAttackSecondPlayable;
            case 24:
                return SwordFinalAttackPlayable;
            case 25:
                return SwordSprint;
            case 26:
                return SwordBlockPlayable;
            case 27:
                return SwordJumpAttackPlayable;
            case 28:
                return SpearIdle;
            case 29:
                return SpearSprintPlayable;
            case 30:
                return SpearRunPlayable;
            case 31:
                return SpearBlockPlayable;
            case 32:
                return SpearFirstAttackPlayable;
            case 33:
                return SpearFinalAttackPlayable;
            case 34:
                return SpearJumpAttackPlayable;
            case 35:
                return RifleIdle;
            case 36:
                return RifleRunPlayable;
            case 37:
                return RifleSprintPlayable;
            case 38:
                return RifleShootPlayable;
            case 39:
                return RifleCockingPlayable;
            case 40:
                return RifleReloadPlayable;
            case 41:
                return RifleJumpPlayable;
            case 42:
                return RifleFallingPlayable;
            case 43:
                return BowIdlePlayable;
            case 44:
                return BowRunPlayable;
            case 45:
                return BowSprintPlayable;
            case 46:
                return BowDrawArrowPlayable;
            case 47:
                return BowChargePlayable;
            case 48:
                return BowShotPlayable;
            case 49:
                return BowJumpPlayable;
            case 50:
                return BowFallingPlayable;
            case 51:
                return StopSprintPlayable;
            case 52:
                return RifleAimPlayable;
            default: return null;
        }
    }

}
