
using Fusion.Addons.SimpleKCC;
using System.Collections;
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

    public float blendDuration = 0.25f; // Duration of blend in seconds

    public Coroutine blendCoroutine;

    private MonoBehaviour coroutineHost; // host to start coroutine

    public void SetCoroutineHost(MonoBehaviour host)
    {
        coroutineHost = host;
    }

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
        if (oncePlay)
        {
            animationClipPlayable.SetTime(0f);
            animationClipPlayable.Play();
        }

        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        if (blendCoroutine != null) coroutineHost.StopCoroutine(blendCoroutine);
        blendCoroutine = coroutineHost.StartCoroutine(BlendWeights(mixerPlayable, animIndex, 1f));
        coroutineHost.StartCoroutine(BlendWeights(playerPlayables.finalMixer, mixerIndex, 1f));
    }

    public virtual void Exit()
    {
        int mixerIndex = mixers.IndexOf(mixername);
        int animIndex = animations.IndexOf(animationname);

        if (blendCoroutine != null) coroutineHost.StopCoroutine(blendCoroutine);
        blendCoroutine = coroutineHost.StartCoroutine(BlendWeights(mixerPlayable, animIndex, 0f));
        coroutineHost.StartCoroutine(BlendWeights(playerPlayables.finalMixer, mixerIndex, 0f));
    }

    public virtual void LogicUpdate()
    {
        if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) return;
    }

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
