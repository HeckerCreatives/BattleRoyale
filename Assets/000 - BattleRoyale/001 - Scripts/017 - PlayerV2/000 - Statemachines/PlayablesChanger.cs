using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;

public class PlayablesChanger
{
    public AnimationPlayable CurrentState { get; private set; }

    public void Initialize(AnimationPlayable currentState)
    {
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(AnimationPlayable nextState, bool canSameEnter = false)
    {
        if (!canSameEnter && CurrentState == nextState)
            return;

        if (!canSameEnter)
            CurrentState.Exit();

        // Crossfade weights together so the mixer never dips below 1.0 total weight.
        // Independent ease-in/ease-out can momentarily underweight the pose, blending in bind pose (often looks like sinking).
        if (CurrentState != null)
            nextState.BeginCrossFadeFrom(CurrentState);
        else
            nextState.Enter();

        CurrentState = nextState;
    }
}
