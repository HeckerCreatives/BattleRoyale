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

    private Coroutine _weightBlendRoutine;

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
        if (oncePlay)
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

        StartWeightBlend(animIndex, 1f, playerPlayables.enterSpeed, EaseInSine);

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

        StartWeightBlend(animIndex, 0f, playerPlayables.exitSpeed, EaseOutSine);

        //mixerPlayable.SetInputWeight(animIndex, 0f);
    }

    public virtual void NetworkUpdate() { }

    public virtual void NetworkLocalServerUpdate() { }

    public virtual void NetworkLocalUpdate() 
    {
        if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) return;
    }

    private void StartWeightBlend(int animIndex, float targetWeight, float duration, System.Func<float, float> easeFn)
    {
        if (playerPlayables == null)
        {
            mixerPlayable.SetInputWeight(animIndex, targetWeight);
            return;
        }

        if (_weightBlendRoutine != null)
            playerPlayables.StopCoroutine(_weightBlendRoutine);

        _weightBlendRoutine = playerPlayables.StartCoroutine(BlendWeight(animIndex, targetWeight, duration, easeFn));
    }

    private IEnumerator BlendWeight(int animIndex, float targetWeight, float duration, System.Func<float, float> easeFn)
    {
        float startWeight = mixerPlayable.GetInputWeight(animIndex);

        if (duration <= 0f)
        {
            mixerPlayable.SetInputWeight(animIndex, targetWeight);
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = easeFn(Mathf.Clamp01(t));
            mixerPlayable.SetInputWeight(animIndex, Mathf.Lerp(startWeight, targetWeight, eased));
            yield return null;
        }

        mixerPlayable.SetInputWeight(animIndex, targetWeight);
    }

    private static float EaseInSine(float t) => 1f - Mathf.Cos((t * Mathf.PI) * 0.5f);
    private static float EaseOutSine(float t) => Mathf.Sin((t * Mathf.PI) * 0.5f);
}
