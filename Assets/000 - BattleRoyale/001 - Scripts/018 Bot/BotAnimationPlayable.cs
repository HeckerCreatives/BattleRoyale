using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BotAnimationPlayable
{
    public float animationLength;

    string animationname;
    string mixername;

    List<string> animations;
    List<string> mixers;

    // When true, Enter/Exit do not touch lower-body mixer weights — managed exclusively
    // by BotPlayables.UpdateLowerBodyLocomotionOverride().
    protected bool skipLowerBodyBlend = false;

    //  ======================

    public BotMovementController botMovement;
    public AnimationMixerPlayable mixerPlayable;
    public BotPlayableChanger botPlayablesChanger;
    public BotPlayables botPlayables;
    public SimpleKCC botController;
    AnimationClipPlayable animationClipPlayable;
    bool oncePlay;

    //  ======================

    public BotAnimationPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay)
    {
        this.botController = botController;
        this.botPlayablesChanger = botPlayablesChanger;
        this.botMovement = botMovement;
        this.botPlayables = botPlayables;
        mixerPlayable = mixerAnimations;
        this.animations = animations;
        this.mixers = mixers;
        this.animationname = animationname;
        this.mixername = mixername;
        this.animationLength = animationLength;
        this.animationClipPlayable = animationClipPlayable;
        this.oncePlay = oncePlay;

        if (oncePlay)
        {
            animationClipPlayable.SetTime(0f);
            animationClipPlayable.Pause();
        }
    }

    public virtual void Enter()
    {
        animationClipPlayable.SetTime(0f);

        if (oncePlay)
            animationClipPlayable.Play();

        int animIndex = animations.IndexOf(animationname);
        if (animIndex < 0)
        {
            Debug.LogWarning($"[BotAnimationPlayable] Animation '{animationname}' was not found in animationnames list.");
            return;
        }

        if (!skipLowerBodyBlend)
            mixerPlayable.SetInputWeight(animIndex, 1f);

        if (botPlayables.HasInputAuthority || botPlayables.HasStateAuthority)
        {
            botPlayables.PlayableState = mixername;
            botPlayables.PlayableAnimationIndex = animIndex;
        }

        if (botPlayables.HasStateAuthority)
            botPlayables.SetAnimationTick();
    }

    public virtual void Exit()
    {
        if (skipLowerBodyBlend) return;

        int animIndex = animations.IndexOf(animationname);
        if (animIndex < 0)
            return;

        mixerPlayable.SetInputWeight(animIndex, 0f);
    }

    public virtual BotAnimationPlayable NetworkUpdate() { return null; }
}
