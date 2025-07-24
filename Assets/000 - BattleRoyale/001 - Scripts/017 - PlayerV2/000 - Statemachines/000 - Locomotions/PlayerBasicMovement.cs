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

    private readonly HashSet<NetworkObject> hitEnemiesFirstFist = new();
    private readonly HashSet<NetworkObject> hitEnemiesSecondFist = new();

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

    //  ======================

    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();

    //  ======================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 36);

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

        #region GLOBAL

        FallingPlayable = new FallingState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "falling", "basic", falling.length, fallingClip, false);
        RollPlayable = new RollState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic", roll.length, rollClip, true);
        HitPlayable = new HitState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "hit", "basic", hit.length, hitClip, true);
        StaggerHitPlayable = new StaggerHit(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "staggerhit", "basic", staggerHit.length, staggerHitClip, true);
        GettingUpPlayable = new GettingUp(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "gettingup", "basic", gettingUp.length, gettingUpClip, true);
        DeathPlayable = new DeathState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "death", "basic", death.length, deathClip, true);
        HealPlayable = new HealState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "heal", "basic", heal.length, healClip, true);
        RepairPlayable = new RepairState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "repair", "basic", heal.length, repairClip, true);
        TrappingPlayable = new TrappingState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "trapping", "basic", trapping.length, trappingClip, true);
        MiddleHitPlayable = new MiddleHitState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "middlehit", "basic", hit.length, hitClip, true);

        #endregion

        #region BASIC

        IdlePlayable = new IdleState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false);
        RunPlayable = new RunState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false);
        SprintPlayable = new SprintState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic", sprint.length, sprintClip, false);
        JumpPlayable = new JumpState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumpidle", "basic", jumpidle.length, idleJumpClip, false);
        BlockPlayable = new BlockState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "block", "basic", block.length, blockClip, true);
        Punch1Playable = new PunchState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch1", "basic", punch1.length, punch1Clip, true);
        Punch2Playable = new MiddlePunchState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch2", "basic", punch2.length, punch2Clip, true);
        Punch3Playable = new FinalPunchState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch3", "basic", punch3.length, punch3Clip, true);
        JumpPunchPlayable = new JumpPunchState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumppunch", "basic", jumpPunch.length, jumpPunchClip, true);

        #endregion

        #region SWORD

        SwordIdlePlayable = new SwordIdleState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordidle", "basic", swordIdle.length, swordIdleClip, false);
        SwordRunPlayable = new SwordRunState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordrun", "basic", swordRun.length, swordRunClip, false);
        SwordAttackFirstPlayable = new SwordFirstAttackState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfirstattack", "basic", swordFirstAttack.length, swordFirstAttackClip, true);
        SwordAttackSecondPlayable = new SwordSecondAttackState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsecondattack", "basic", swordSecondAttack.length, swordSecondAttackClip, true);
        SwordFinalAttackPlayable = new SwordFinalAttackState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordfinalattack", "basic", swordFinalAttack.length, swordFinalAttackClip, true);
        SwordSprintPlayable = new SwordSprintState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordsprint", "basic", swordSprint.length, swordSprintClip, false);
        SwordBlockPlayable = new BlockState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordblock", "basic", swordBlock.length, swordBlockClip, true);
        SwordJumpAttackPlayable = new SwordJumpAttack(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "swordjumpattack", "basic", swordJumpSlash.length, swordJumpAttackClip, true);

        #endregion

        #region SPEAR

        SpearIdlePlayable = new SpearIdleState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearidle", "basic", spearIdle.length, spearIdleClip, false);
        SpearRunPlayable = new SpearRunState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearrun", "basic", swordRun.length, spearRunClip, false);
        SpearSprintPlayable = new SpearSprintState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearsprint", "basic", swordSprint.length, spearSprintClip, false);
        SpearFirstAttackPlayable = new SpearFirstAttackState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfirstattack", "basic", spearFirstAttack.length, spearFirstAattackClip, true);
        SpearFinalAttackPlayable = new SpearFinalAttackState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearfinalattack", "basic", spearFinalAttack.length, spearFinalAattackClip, true);
        SpearBlockPlayable = new BlockState(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearblock", "basic", swordBlock.length, spearBlockClip, true);
        SpearJumpAttackPlayable = new SpearJumpAttack(this, simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "spearjumpattack", "basic", spearJumpAttack.length, spearJumpAttackClip, true);

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

            if (hitObject.GetComponent<PlayerPlayables>().healthV2.IsStagger) return;

            // Avoid duplicate hits
            if (!hitEnemiesFirstFist.Contains(hitObject))
            {
                //bareHandsMovement.CanDamage = false;

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

                //Debug.Log($"First Fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();

                if (isFinal) healthV2.IsStagger = true;
                else  healthV2.IsHit = true;

                healthV2.ApplyDamage(tempdamage, "", Object);

                // Process the hit
                //HandleHit(hitObject, tempdamage);


                //Hit++;
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

            if (hitObject.GetComponent<PlayerPlayables>().healthV2.IsStagger) return;

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

                healthV2.IsHit = true;

                healthV2.ApplyDamage(tempdamage, "", Object);

                // Process the hit
                //HandleHit(hitObject, tempdamage);


                //Hit++;
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
