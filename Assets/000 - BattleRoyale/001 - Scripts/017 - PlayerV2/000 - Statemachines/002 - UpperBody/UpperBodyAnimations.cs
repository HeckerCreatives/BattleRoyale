using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class UpperBodyAnimations
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
    public UpperBodyChanger playablesChanger;
    public PlayerPlayables playerPlayables;
    public SimpleKCC characterController;
    public AnimationClipPlayable animationClipPlayable;
    public bool oncePlay;
    public bool canAnimateUpper;

    //  ======================

    public float blendDuration = 0.25f; // Duration of blend in seconds

    public Coroutine blendCoroutine;
    public Coroutine weightCoroutine;
    private AnimationMixerPlayable mixerAnimations;

    //  ======================

    public UpperBodyAnimations(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper)
    {
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
        this.canAnimateUpper = canAnimateUpper;
    }

    public virtual void Enter()
    {
        if (oncePlay && canAnimateUpper)
        {
            animationClipPlayable.SetTime(0f);
            animationClipPlayable.Play();
        }

        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        if (playerPlayables.HasStateAuthority)
        {
            mixerPlayable.SetInputWeight(animIndex, canAnimateUpper ? 1f : 0f);
            playerPlayables.PlayableState = mixername;
            playerPlayables.PlayableUpperBoddyAnimationIndex = animIndex;
            playerPlayables.SetAnimationUpperTick();

            return;
        }

        if (!canAnimateUpper) return;

        if (ltExit != 0) LeanTween.cancel(ltExit);

        ltEnter = LeanTween.value(playerPlayables.gameObject, mixerPlayable.GetInputWeight(animIndex), 1f, playerPlayables.enterSpeed)
        .setOnUpdate((float weight) =>
        {
            mixerPlayable.SetInputWeight(animIndex, weight);
        }).setOnComplete(() => mixerPlayable.SetInputWeight(animIndex, 1f)).setEase(LeanTweenType.easeInSine).id;

        //mixerPlayable.SetInputWeight(animIndex, 1f);
    }

    public virtual void Exit()
    {
        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        if (playerPlayables.HasStateAuthority)
        {
            mixerPlayable.SetInputWeight(animIndex, 0f);
            return;
        }

        if (!canAnimateUpper) return;

        if (ltEnter != 0) LeanTween.cancel(ltEnter);

        ltExit = LeanTween.value(playerPlayables.gameObject, mixerPlayable.GetInputWeight(animIndex), 0f, playerPlayables.exitSpeed)
        .setOnUpdate((float weight) =>
        {
            mixerPlayable.SetInputWeight(animIndex, weight);
        }).setOnComplete(() => mixerPlayable.SetInputWeight(animIndex, 0f)).setEase(LeanTweenType.easeOutSine).id;

        //mixerPlayable.SetInputWeight(animIndex, 0f);
    }

    public virtual void NetworkUpdate() { }

    public virtual void NetworkLocalServerUpdate() { }

    public virtual void NetworkLocalUpdate() 
    {
        if (playerPlayables.HasInputAuthority) return;
    }
}
