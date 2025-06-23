
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationPlayable
{
    public float animationLength;

    string animationname;
    string mixername;

    List<string> animations;
    List<string> mixers;

    //  ======================

    public PlayerMovementV2 playerMovement;
    public AnimationMixerPlayable mixerPlayable;
    public PlayablesChanger playablesChanger;
    public PlayerPlayables playerPlayables;
    public SimpleKCC characterController;
    AnimationClipPlayable animationClipPlayable;
    bool oncePlay;

    //  ======================

    public AnimationPlayable(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay    )
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

        if (oncePlay)
        {
            animationClipPlayable.SetTime(0f);
            animationClipPlayable.Pause();
        }
    }

    public virtual void Enter()
    {
        Debug.Log($"ENTER mixer count: {animations.Count}  animationanme: {animationname}");
        mixerPlayable.SetInputWeight(animations.IndexOf(animationname), 1f);

        if (oncePlay)
        {
            animationClipPlayable.SetTime(0f);
            animationClipPlayable.Play();
        }

        playerPlayables.finalMixer.SetInputWeight(mixers.IndexOf(mixername), 1f);
    }

    public virtual void Exit()
    {
        Debug.Log($"EXIT mixer count: {animations.Count}  animationanme: {animationname}");
        playerPlayables.finalMixer.SetInputWeight(mixers.IndexOf(mixername), 0f);
        mixerPlayable.SetInputWeight(animations.IndexOf(animationname), 0f);
    }

    public virtual void LogicUpdate(){ }

    public virtual void NetworkUpdate() { }
}
