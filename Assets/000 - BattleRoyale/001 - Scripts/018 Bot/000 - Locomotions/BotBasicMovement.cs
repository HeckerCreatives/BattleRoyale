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

    //  =================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(botPlayables.playableGraph, 11);

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
            default: return null;
        }
    }
}
