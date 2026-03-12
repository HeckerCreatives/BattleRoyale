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

    public void ChangeState(AnimationPlayable nextState)
    {
        if (CurrentState == nextState)
            return;

        CurrentState.Exit();

        nextState.Enter();

        CurrentState = nextState;
    }
}
