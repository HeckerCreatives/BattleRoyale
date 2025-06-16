
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationPlayable
{

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

    //  ======================

    public AnimationPlayable(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername)
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
    }

    public virtual void Enter()
    {
        Debug.Log($"ENTER mixer count: {animations.Count}  animationanme: {animationname}");
        mixerPlayable.SetInputWeight(animations.IndexOf(animationname), 1f);
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
