using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayablesChanger
{
    public AnimationPlayable CurrentState { get; private set; }

    public MonoBehaviour coroutineHost;

    public void Initialize(AnimationPlayable currentState)
    {
        currentState.SetCoroutineHost(coroutineHost);
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(AnimationPlayable currentState)
    {
        CurrentState.Exit();
        currentState.SetCoroutineHost(coroutineHost);
        CurrentState = currentState;
        CurrentState.Enter();
    }
}
