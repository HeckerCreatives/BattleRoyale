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
        CurrentState.Exit();
        CurrentState = currentState;
        CurrentState.Enter();
    }
}
