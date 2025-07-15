using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayablesChanger
{
    public AnimationPlayable CurrentState { get; private set; }

    public void Initialize(AnimationPlayable currentState)
    {
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(AnimationPlayable currentState)
    {
        CurrentState.Exit();
        CurrentState = currentState;
        CurrentState.Enter();
    }
}
