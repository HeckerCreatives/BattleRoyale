using Fusion;
using Fusion.Addons.SimpleKCC;
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

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float attackRadius;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    [field: Header("DEBUGGER")]
    [Networked, Capacity(10)] public NetworkLinkedList<NetworkObject> hitEnemiesFirstFist { get; } = new NetworkLinkedList<NetworkObject>();
    [Networked, Capacity(10)] public NetworkLinkedList<NetworkObject> hitEnemiesSecondFist { get; } = new NetworkLinkedList<NetworkObject>();

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

    //  ======================

    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();

    //  ======================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 16);

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

        IdlePlayable = new IdleState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false);
        RunPlayable = new RunState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false);
        SprintPlayable = new SprintState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic", sprint.length, sprintClip, false);
        JumpPlayable = new JumpState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumpidle", "basic", jumpidle.length, idleJumpClip, false);
        FallingPlayable = new FallingState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "falling", "basic", falling.length, fallingClip, false);
        RollPlayable = new RollState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic", roll.length, rollClip, true);
        BlockPlayable = new BlockState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "block", "basic", block.length, blockClip, true);
        Punch1Playable = new PunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch1", "basic", punch1.length, punch1Clip, true);
        Punch2Playable = new MiddlePunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch2", "basic", punch2.length, punch2Clip, true);
        Punch3Playable = new FinalPunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch3", "basic", punch3.length, punch3Clip, true);
        JumpPunchPlayable = new JumpPunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "jumppunch", "basic", jumpPunch.length, jumpPunchClip, true);
        HitPlayable = new HitState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "hit", "basic", hit.length, hitClip, true);
        StaggerHitPlayable = new StaggerHit(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "staggerhit", "basic", staggerHit.length, staggerHitClip, true);
        GettingUpPlayable = new GettingUp(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "gettingup", "basic", gettingUp.length, gettingUpClip, true);

        return mixerPlayable;
    }

    public void PerformFirstAttack()
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

            if (hitObject.GetComponent<PlayerPlayables>().healthV2.IsHit || hitObject.GetComponent<PlayerPlayables>().healthV2.IsStagger) return;

            // Avoid duplicate hits
            if (!hitEnemiesFirstFist.Contains(hitObject))
            {
                //bareHandsMovement.CanDamage = false;

                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 40f,
                    "Body" => 35f,
                    "Thigh" => 30f,
                    "Shin" => 25f,
                    "Foot" => 20f,
                    "Arm" => 30f,
                    "Forearm" => 25f,
                    _ => 0f
                };

                //Debug.Log($"First Fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                hitObject.GetComponent<PlayerHealthV2>().IsHit = true;

                // Process the hit
                //HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesFirstFist.Add(hitObject);

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

            // Avoid duplicate hits
            if (!hitEnemiesSecondFist.Contains(hitObject))
            {
                //bareHandsMovement.CanDamage = false;

                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 40f,
                    "Body" => 35f,
                    "Thigh" => 30f,
                    "Shin" => 25f,
                    "Foot" => 20f,
                    "Arm" => 30f,
                    "Forearm" => 25f,
                    _ => 0f
                };

                hitObject.GetComponent<PlayerHealthV2>().IsSecondHit = true;

                // Process the hit
                //HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesSecondFist.Add(hitObject);

                //Hit++;
            }
        }
    }

    public void PerformFinalAttack()
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

            if (hitObject.GetComponent<PlayerPlayables>().healthV2.IsHit || hitObject.GetComponent<PlayerPlayables>().healthV2.IsStagger) return;

            // Avoid duplicate hits
            if (!hitEnemiesFirstFist.Contains(hitObject))
            {
                //bareHandsMovement.CanDamage = false;

                string tag = hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 40f,
                    "Body" => 35f,
                    "Thigh" => 30f,
                    "Shin" => 25f,
                    "Foot" => 20f,
                    "Arm" => 30f,
                    "Forearm" => 25f,
                    _ => 0f
                };

                //Debug.Log($"First Fist Damage by {loader.Username} to {hitObject.GetComponent<PlayerNetworkLoader>().Username} on {tag}: {tempdamage}");

                hitObject.GetComponent<PlayerHealthV2>().IsStagger = true;

                // Process the hit
                //HandleHit(hitObject, tempdamage);

                // Mark as hit
                hitEnemiesFirstFist.Add(hitObject);

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

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(impactFirstFistPoint.position, attackRadius);


        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(impactSecondFistPoint.position, attackRadius);
    }
}
