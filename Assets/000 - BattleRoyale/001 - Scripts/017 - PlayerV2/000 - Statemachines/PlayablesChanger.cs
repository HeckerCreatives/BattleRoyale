using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayablesChanger
{
    public AnimationPlayable CurrentState { get; private set; }
    private MonoBehaviour coroutineHost;

    public PlayablesChanger(MonoBehaviour host)
    {
        coroutineHost = host;
    }

    public void Initialize(AnimationPlayable currentState)
    {
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(AnimationPlayable nextState)
    {
        CurrentState.Exit();
        nextState.Enter();
        CurrentState = nextState;
    }

    private IEnumerator DelayedEnter(AnimationPlayable nextState)
    {
        yield return null; // wait 1 frame
        nextState.Enter();
        CurrentState = nextState;
    }
}
