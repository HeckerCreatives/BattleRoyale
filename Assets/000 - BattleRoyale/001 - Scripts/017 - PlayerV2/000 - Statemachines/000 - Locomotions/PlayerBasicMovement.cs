using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerBasicMovement : MonoBehaviour
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

    //  ======================

    public AnimationMixerPlayable mixerPlayable;
    //private List<AnimationClipPlayable> clipPlayables;

    public IdleState IdlePlayable { get; private set; }
    public RunState RunPlayable { get; private set; }
    public SprintState SprintPlayable { get; private set; }
    public RollState RollPlayable { get; private set; }
    public PunchState Punch1Playable { get; private set; }
    public MiddlePunchState Punch2Playable { get; private set; }
    public FinalPunchState Punch3Playable { get; private set; }

    //  ======================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 8);

        var idleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, idle);
        var runClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, run);
        var sprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, sprint);
        var rollClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, roll);
        var punch1Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch1);
        var punch2Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch2);
        var punch3Clip = AnimationClipPlayable.Create(playerPlayables.playableGraph, punch3);

        playerPlayables.playableGraph.Connect(idleClip, 0, mixerPlayable, 1);
        playerPlayables.playableGraph.Connect(runClip, 0, mixerPlayable, 2);
        playerPlayables.playableGraph.Connect(sprintClip, 0, mixerPlayable, 3);
        playerPlayables.playableGraph.Connect(rollClip, 0, mixerPlayable, 4);
        playerPlayables.playableGraph.Connect(punch1Clip, 0, mixerPlayable, 5);
        playerPlayables.playableGraph.Connect(punch2Clip, 0, mixerPlayable, 6);
        playerPlayables.playableGraph.Connect(punch3Clip, 0, mixerPlayable, 7);

        IdlePlayable = new IdleState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic", idle.length, idleClip, false);
        RunPlayable = new RunState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic", run.length, runClip, false);
        SprintPlayable = new SprintState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic", sprint.length, runClip, false);
        RollPlayable = new RollState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic", roll.length, rollClip, true);
        Punch1Playable = new PunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch1", "basic", punch1.length, punch1Clip, true);
        Punch2Playable = new MiddlePunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch2", "basic", punch2.length, punch2Clip, true);
        Punch3Playable = new FinalPunchState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "punch3", "basic", punch2.length, punch3Clip, true);

        return mixerPlayable;
    }
}
