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
    [SerializeField] private AnimationClip spearAttackThree;

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
    public BotSpearAttackThree SpearAttackThree;

    //  =================

    public AnimationMixerPlayable Initialize()
    {
        mixerPlayable = AnimationMixerPlayable.Create(botPlayables.playableGraph, 21);

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
        var spearAttackThreeClip = AnimationClipPlayable.Create(botPlayables.playableGraph, spearAttackThree);

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
        botPlayables.playableGraph.Connect(spearAttackThreeClip, 0, mixerPlayable, 20);

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
        SpearAttackThree = new BotSpearAttackThree(this, simpleKCC, botPlayables.changer, botMovement, botPlayables, mixerPlayable, animationnames, mixernames, "spearAttackThree", "basic", spearAttackThree.length, spearAttackThreeClip, true);

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
            case 20: 
                return SpearAttackThree;
            default: return null;
        }
    }
}
