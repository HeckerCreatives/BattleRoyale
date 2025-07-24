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

    //  ======================

    public BotMovementController botMovement;
    public AnimationMixerPlayable mixerPlayable;
    public BotPlayableChanger botPlayablesChanger;
    public BotPlayables botPlayables;
    public SimpleKCC botController;
    AnimationClipPlayable animationClipPlayable;
    bool oncePlay;

    //  ======================

    public float blendDuration = 0.25f; // Duration of blend in seconds

    public Coroutine blendCoroutine;
    public Coroutine weightCoroutine;

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

        if (botPlayables.HasInputAuthority || botPlayables.HasStateAuthority)
        {
            botPlayables.PlayableState = mixername;
            botPlayables.PlayableAnimationIndex = animIndex;
        }

        if (botPlayables.HasStateAuthority)
            botPlayables.SetAnimationTick();

        if (blendCoroutine != null) coroutineHost.StopCoroutine(blendCoroutine);
        blendCoroutine = coroutineHost.StartCoroutine(BlendWeights(mixerPlayable, animIndex, 1f));
        coroutineHost.StartCoroutine(BlendWeights(botPlayables.finalMixer, mixerIndex, 1f));
    }

    public virtual void Exit()
    {
        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        if (blendCoroutine != null) coroutineHost.StopCoroutine(blendCoroutine);
        blendCoroutine = coroutineHost.StartCoroutine(BlendWeights(mixerPlayable, animIndex, 0f));
        coroutineHost.StartCoroutine(BlendWeights(botPlayables.finalMixer, mixerIndex, 0f));
    }

    ////public virtual void LogicUpdate()
    ////{
    ////    if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) return;
    ////}

    public virtual void NetworkUpdate() { }

    private IEnumerator BlendWeights(Playable mixer, int index, float targetWeight)
    {
        float startWeight = mixer.GetInputWeight(index);
        float time = 0f;

        while (time < blendDuration)
        {
            time += Time.deltaTime;
            float t = time / blendDuration;
            float newWeight = Mathf.Lerp(startWeight, targetWeight, t);
            mixer.SetInputWeight(index, newWeight);
            yield return null;
        }

        mixer.SetInputWeight(index, targetWeight); // Final snap to target
    }
}
