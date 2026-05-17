using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotPlayableChanger
{
    public BotAnimationPlayable CurrentState { get; private set; }

    public void Initialize(BotAnimationPlayable currentState)
    {
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(BotAnimationPlayable currentState)
    {
        if (currentState == null || CurrentState == currentState)
            return;

        if (CurrentState == null)
        {
            CurrentState = currentState;
            CurrentState.Enter();
            return;
        }

        CurrentState.Exit();
        CurrentState = currentState;
        CurrentState.Enter();
    }
}
