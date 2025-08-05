using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperMovement : NetworkBehaviour
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

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float attackRadius;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

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

    //  ======================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 35);

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
        //var fallingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, falling);
        //var rollClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, roll);
        //var punch2Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch2);
        //var punch3Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch3);
        //var jumpPunchClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, jumpPunch);
        //var blockClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, block);
        //var trappingClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, trapping);
        //var middleHitClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, hit);
        //var swordRunClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, swordRun);

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

        #region GLOBAL

        FallingPlayables = new PlayerUpperFalling(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "falling", "basic", falling.length, fallingClip, false);
        RollPlayables = new PlayerUpperRoll(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic", roll.length, rollClip, true);
        DeathPlayable = new PlayerUpperDeath(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "death", "basic", death.length, deathClip, true);
        HitPlayable = new PlayerUpperHit(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "hit", "basic", hit.length, hitClip, true);
        StaggerHitPlayable = new PlayerUpperStagger(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "staggerhit", "basic", staggerHit.length, staggerHitClip, true);
        GettingUpPlayable = new PlayerUpperGettingUp(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "gettingup", "basic", gettingUp.length, gettingUpClip, true);
        HealPlayable = new PlayerHealPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "heal", "basic", heal.length, healClip, true);
        RepairPlayable = new PlayerUpperRepair(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "repair", "basic", heal.length, repairClip, true);
        TrapPlayable = new PlayerUpperTrap(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "trapping", "basic", trapping.length, trappingClip, true);
        MiddleHitPlayable = new PlayerMiddleHit(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "middlehit", "basic", hit.length, hitClip, true);

        #endregion

        #region BASIC

        IdlePlayables = new PlayerUpperIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false);
        RunPlayables = new PlayerUpperRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false);
        FirstPunch = new PlayerFistFirstPunch(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch1", "basic", punch1.length, punch1Clip, true);
        SecondPunch = new PlayerFistSecondPunch(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch2", "basic", punch2.length, punch2Clip, true);
        FinalPunch = new PlayerFistFinalPunch(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch3", "basic", punch3.length, punch3Clip, true);
        SprintPlayables = new PlayerUpperSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic", sprint.length, sprintClip, false);
        JumpPlayable = new PlayerUpperJump(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumpidle", "basic", jumpidle.length, jumpClip, false);
        BlockPlayable = new PlayerBlockPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "block", "basic", block.length, blockClip, true);
        JumpPunchPlayable = new PlayerJumpPunchPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumppunch", "basic", jumpPunch.length, jumpPunchClip, true);

        #endregion

        #region SWORD

        SwordIdlePlayable = new PlayerUpperSwordIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordidle", "basic", swordIdle.length, swordIdleClip, false);
        SwordRunPlayable = new PlayerUpperSwordRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordrun", "basic", swordRun.length, swordRunClip, false);
        SwordAttackFirstPlayable = new PlayerUpperSwordFirstAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfirstattack", "basic", swordFirstAttack.length, swordFirstAttackClip, true);
        SwordAttackSecondPlayable = new PlayerUpperSwordMiddleAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsecondattack", "basic", swordSecondAttack.length, swordSecondAttackClip, true);
        SwordFinalAttackPlayable = new PlayerUpperSwordFinalAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfinalattack", "basic", swordFinalAttack.length, swordFinalAttackClip, true);
        SwordSprint = new PlayerUpperSwordSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsprint", "basic", swordSprint.length, swordSprintClip, false);
        SwordBlockPlayable = new PlayerBlockPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordblock", "basic", swordBlock.length, swordBlockClip, true);
        SwordJumpAttackPlayable = new PlayerUpperSwordJumpAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordjumpattack", "basic", swordJumpSlash.length, swordJumpAttackClip, true);

        #endregion

        #region SPEAR

        SpearIdle = new PlayerUpperSpearIdle(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearidle", "basic", spearIdle.length, spearIdleClip, false);
        SpearRunPlayable = new PlayerUpperSpearRun(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearrun", "basic", swordRun.length, spearRunClip, false);
        SpearSprintPlayable = new PlayerUpperSpearSprint(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearsprint", "basic", swordSprint.length, spearSprintClip, false);
        SpearFirstAttackPlayable = new PlayerUpperSpearFirstAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfirstattack", "basic", spearFirstAttack.length, spearFirstAattackClip, true);
        SpearFinalAttackPlayable = new PlayerUpperSpearFinalAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfinalattack", "basic", spearFinalAttack.length, spearFinalAattackClip, true);
        SpearBlockPlayable = new PlayerBlockPlayable(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearblock", "basic", swordBlock.length, spearBlockClip, true);
        SpearJumpAttackPlayable = new PlayerUpperSpearJumpAttack(simpleKCC, playerPlayables.upperBodyChanger, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearjumpattack", "basic", spearJumpAttack.length, spearJumpAttackClip, true);

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

                    tempdata.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username, Object);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();

                if (tempplayables.healthV2.IsStagger) return;
                if (tempplayables.healthV2.IsGettingUp) return;

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

                    if (isFinal) healthV2.IsStagger = true;
                    else
                    {
                        healthV2.IsHitUpper = true;
                        healthV2.IsHit = true;
                    }

                    healthV2.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username, Object);
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

                    tempdata.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username, Object);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();

                if (tempplayables.healthV2.IsStagger) return;
                if (tempplayables.healthV2.IsGettingUp) return;

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

                    healthV2.IsHitUpper = true;
                    healthV2.IsHit = true;

                    healthV2.ApplyDamage(tempdamage, playerOwnObjectEnabler.Username, Object);
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
            default: return null;
        }
    }

}
