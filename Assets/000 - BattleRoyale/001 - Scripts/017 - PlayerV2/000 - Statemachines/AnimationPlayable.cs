
using Fusion.Addons.SimpleKCC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Rendering;

public class AnimationPlayable
{
    public float animationLength;

    public string animationname;
    string mixername;

    public List<string> animations;
    List<string> mixers;

    int ltEnter;
    int ltExit;

    //  ======================

    public PlayerMovementV2 playerMovement;
    public AnimationMixerPlayable mixerPlayable;
    public PlayablesChanger playablesChanger;
    public PlayerPlayables playerPlayables;
    public SimpleKCC characterController;
    public AnimationClipPlayable animationClipPlayable;
    public bool oncePlay;
    public bool lower;


    //  ======================

    public float blendDuration = 0.25f; // Duration of blend in seconds

    public Coroutine blendCoroutine;
    public Coroutine weightCoroutine;

    private MonoBehaviour coroutineHost; // host to start coroutine

    //  ======================

    public AnimationPlayable(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower)
    {
        coroutineHost = host;
        this.characterController = characterController;
        this.playablesChanger = playablesChanger;
        this.playerMovement = playerMovement;
        this.playerPlayables = playerPlayables;
        mixerPlayable = mixerAnimations;
        this.animations = animations;
        this.mixers = mixers;
        this.animationname = animationname;
        this.mixername = mixername;
        this.animationLength = animationLength;
        this.animationClipPlayable = animationClipPlayable;
        this.oncePlay = oncePlay;
        lower = isLower;
    }

    public virtual void Enter()
    {
        if (oncePlay)
        {
            animationClipPlayable.SetTime(0f);
            animationClipPlayable.Play();
        }

        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);


        if (playerPlayables.HasStateAuthority)
        {
            playerPlayables.PlayableState = mixername;
            playerPlayables.PlayableLowerBoddyAnimationIndex = animIndex;
            playerPlayables.SetAnimationLowerTick();
        }

        if (ltExit != 0) LeanTween.cancel(ltExit);

        ltEnter = LeanTween.value(playerPlayables.gameObject, mixerPlayable.GetInputWeight(animIndex), 1f, playerPlayables.enterSpeed)
        .setOnUpdate((float weight) =>
        {
            mixerPlayable.SetInputWeight(animIndex, weight);
        }).setOnComplete(() => mixerPlayable.SetInputWeight(animIndex, 1f)).setEase(LeanTweenType.linear).id;
    }

    public virtual void Exit()
    {
        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        if (ltEnter != 0) LeanTween.cancel(ltEnter);

        ltExit = LeanTween.value(playerPlayables.gameObject, mixerPlayable.GetInputWeight(animIndex), 0f, playerPlayables.exitSpeed)
        .setOnUpdate((float weight) =>
        {
            mixerPlayable.SetInputWeight(animIndex, weight);
        }).setOnComplete(() => mixerPlayable.SetInputWeight(animIndex, 0f)).setEase(LeanTweenType.linear).id;

    }

    //public virtual void LogicUpdate()
    //{
    //    if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) return;
    //}

    public virtual void NetworkUpdate() { }
}
