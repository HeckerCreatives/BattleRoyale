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

    //  ======================

    public AnimationMixerPlayable mixerPlayable;
    //private List<AnimationClipPlayable> clipPlayables;

    public IdleState IdlePlayable { get; private set; }
    public RunState RunPlayable { get; private set; }
    public SprintState SprintPlayable { get; private set; }
    public RollState RollPlayable { get; private set; }

    //  ======================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(playerPlayables.playableGraph, 5);

        var idleClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, idle);
        var runClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, run);
        var sprintClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, sprint);
        var rollClip = AnimationClipPlayable.Create(playerPlayables.playableGraph, roll);

        playerPlayables.playableGraph.Connect(idleClip, 0, mixerPlayable, 1);
        playerPlayables.playableGraph.Connect(runClip, 0, mixerPlayable, 2);
        playerPlayables.playableGraph.Connect(sprintClip, 0, mixerPlayable, 3);
        playerPlayables.playableGraph.Connect(rollClip, 0, mixerPlayable, 4);

        IdlePlayable = new IdleState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "idle", "basic");
        RunPlayable = new RunState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "run", "basic");
        SprintPlayable = new SprintState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "sprint", "basic");
        RollPlayable = new RollState(simpleKCC, playerPlayables.changer, playerMovementV2, playerPlayables, mixerPlayable, animationnames, mixernames, "roll", "basic");

        return mixerPlayable;
    }
}
