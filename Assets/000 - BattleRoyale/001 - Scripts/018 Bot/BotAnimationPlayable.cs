using Fusion.Addons.SimpleKCC;
using System.Collections;
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

    private Coroutine _weightBlendRoutine;

    //  ======================

    public BotMovementController botMovement;
    public AnimationMixerPlayable mixerPlayable;
    public BotPlayableChanger botPlayablesChanger;
    public BotPlayables botPlayables;
    public SimpleKCC botController;
    AnimationClipPlayable animationClipPlayable;
    bool oncePlay;

    //  ======================

    private MonoBehaviour coroutineHost; // host to start coroutine

    //  ======================

    public BotAnimationPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay)
    {
        coroutineHost = host;
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

        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        StartWeightBlend(animIndex, 1f, botPlayables.enterSpeed);

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
        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        StartWeightBlend(animIndex, 0f, botPlayables.exitSpeed);
    }

    ////public virtual void LogicUpdate()
    ////{
    ////    if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) return;
    ////}

    public virtual void NetworkUpdate() { }

    private void StartWeightBlend(int animIndex, float targetWeight, float duration)
    {
        if (coroutineHost == null)
        {
            mixerPlayable.SetInputWeight(animIndex, targetWeight);
            return;
        }

        if (_weightBlendRoutine != null)
            coroutineHost.StopCoroutine(_weightBlendRoutine);

        _weightBlendRoutine = coroutineHost.StartCoroutine(BlendWeight(animIndex, targetWeight, duration));
    }

    private IEnumerator BlendWeight(int animIndex, float targetWeight, float duration)
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
            mixerPlayable.SetInputWeight(animIndex, Mathf.Lerp(startWeight, targetWeight, Mathf.Clamp01(t)));
            yield return null;
        }

        mixerPlayable.SetInputWeight(animIndex, targetWeight);
    }
}
