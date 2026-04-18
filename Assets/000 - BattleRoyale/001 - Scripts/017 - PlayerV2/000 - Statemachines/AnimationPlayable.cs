
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

    private Coroutine _weightBlendRoutine;
    private AnimationPlayable _crossFadeFrom;

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

            //Debug.Log($"once play by {animationname}");
        }

        int mixerIndex = mixers.IndexOf(mixername);

        if (playerPlayables == null)
        {
            Debug.LogError($"[{animationname}] playerPlayables is NULL in Enter()");
            return;
        }

        int animIndex = animations.IndexOf(animationname);
        if (animIndex < 0)
        {
            Debug.LogError($"[{animationname}] not found in animations list!");
            return;
        }

        //mixerPlayable.SetInputWeight(animIndex, 1f);

        if (playerPlayables.HasStateAuthority)
        {
            // If we are crossfading on authority, ensure the previous state's weight is cleared.
            if (_crossFadeFrom != null)
                _crossFadeFrom.SetWeightImmediate(0f);

            ZeroAllWeightsExcept(animIndex);
            mixerPlayable.SetInputWeight(animIndex, 1f);
            playerPlayables.PlayableState = mixername;
            playerPlayables.PlayableLowerBoddyAnimationIndex = animIndex;
            playerPlayables.SetAnimationLowerTick();

            return;
        }

        if (_crossFadeFrom != null)
        {
            StartCrossFade(animIndex, _crossFadeFrom, playerPlayables.enterSpeed, playerPlayables.exitSpeed);
            return;
        }

        StartWeightBlend(animIndex, 1f, playerPlayables.enterSpeed, EaseInSine);

    }

    public virtual void Exit()
    {
        //int mixerIndex = mixers.IndexOf(mixername);
        //int animIndex = animations.IndexOf(animationname);

        //if (playerPlayables.HasStateAuthority)
        //{
        //    mixerPlayable.SetInputWeight(animIndex, 0f);
        //    return;
        //}

        //StartWeightBlend(animIndex, 0f, playerPlayables.exitSpeed, EaseOutSine);

        //mixerPlayable.SetInputWeight(animIndex, 0f);
    }

    /// <summary>
    /// Crossfade into this state from <paramref name="fromState"/> using a single shared easing timeline,
    /// keeping the total weight stable to avoid blending in bind pose (often looks like sinking).
    /// </summary>
    public void BeginCrossFadeFrom(AnimationPlayable fromState)
    {
        _crossFadeFrom = fromState;
        Enter();
        _crossFadeFrom = null;
    }

    public virtual void NetworkUpdate() { }

    public virtual void NetworkLocalUpdate()
    {
        if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) return;
    }

    private void StartWeightBlend(int animIndex, float targetWeight, float duration, Func<float, float> easeFn)
    {
        if (coroutineHost == null)
        {
            if (targetWeight >= 0.999f)
                ZeroAllWeightsExcept(animIndex);
            mixerPlayable.SetInputWeight(animIndex, targetWeight);
            return;
        }

        if (_weightBlendRoutine != null)
            coroutineHost.StopCoroutine(_weightBlendRoutine);

        _weightBlendRoutine = coroutineHost.StartCoroutine(BlendWeight(animIndex, targetWeight, duration, easeFn));
    }

    private void StartCrossFade(int toAnimIndex, AnimationPlayable fromState, float toDuration, float fromDuration)
    {
        if (coroutineHost == null)
        {
            // Fallback to immediate set.
            fromState.SetWeightImmediate(0f);
            mixerPlayable.SetInputWeight(toAnimIndex, 1f);
            return;
        }

        // Stop any running blends on both states.
        if (_weightBlendRoutine != null)
            coroutineHost.StopCoroutine(_weightBlendRoutine);
        fromState.StopBlendIfRunning();

        int fromAnimIndex = fromState.GetAnimIndex();

        float duration = Mathf.Max(0f, toDuration, fromDuration);
        _weightBlendRoutine = coroutineHost.StartCoroutine(CrossFadeWeights(fromAnimIndex, toAnimIndex, duration));
    }

    private IEnumerator CrossFadeWeights(int fromAnimIndex, int toAnimIndex, float duration)
    {
        ZeroAllWeightsExcept(fromAnimIndex, toAnimIndex);

        float fromStart = Mathf.Clamp01(mixerPlayable.GetInputWeight(fromAnimIndex));

        // Ensure "to" starts at complementary weight so sum is stable from frame 0.
        float toStart = Mathf.Clamp01(mixerPlayable.GetInputWeight(toAnimIndex));
        if (toStart < 0.0001f)
            mixerPlayable.SetInputWeight(toAnimIndex, 1f - fromStart);

        if (duration <= 0f)
        {
            mixerPlayable.SetInputWeight(fromAnimIndex, 0f);
            mixerPlayable.SetInputWeight(toAnimIndex, 1f);
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float eased = EaseInOutSine(Mathf.Clamp01(t));

            float fromW = Mathf.Lerp(fromStart, 0f, eased);
            float toW = 1f - fromW; // keep sum stable

            mixerPlayable.SetInputWeight(fromAnimIndex, fromW);
            mixerPlayable.SetInputWeight(toAnimIndex, toW);

            yield return null;
        }

        ZeroAllWeightsExcept(toAnimIndex);
        mixerPlayable.SetInputWeight(fromAnimIndex, 0f);
        mixerPlayable.SetInputWeight(toAnimIndex, 1f);
    }

    private void StopBlendIfRunning()
    {
        if (_weightBlendRoutine != null && coroutineHost != null)
            coroutineHost.StopCoroutine(_weightBlendRoutine);
        _weightBlendRoutine = null;
    }

    private int GetAnimIndex() => animations.IndexOf(animationname);

    private void SetWeightImmediate(float weight)
    {
        int animIndex = GetAnimIndex();
        mixerPlayable.SetInputWeight(animIndex, weight);
    }

    private void ZeroAllWeightsExcept(params int[] keepIndices)
    {
        int inputCount = mixerPlayable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            bool keep = false;
            for (int k = 0; k < keepIndices.Length; k++)
            {
                if (i == keepIndices[k])
                {
                    keep = true;
                    break;
                }
            }

            if (!keep)
                mixerPlayable.SetInputWeight(i, 0f);
        }
    }

    private IEnumerator BlendWeight(int animIndex, float targetWeight, float duration, Func<float, float> easeFn)
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

        if (targetWeight >= 0.999f)
            ZeroAllWeightsExcept(animIndex);

        mixerPlayable.SetInputWeight(animIndex, targetWeight);
    }

    private static float EaseInSine(float t) => 1f - Mathf.Cos((t * Mathf.PI) * 0.5f);
    private static float EaseOutSine(float t) => Mathf.Sin((t * Mathf.PI) * 0.5f);
    private static float EaseInOutSine(float t) => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
}
