using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class UpperBodyChanger
{
    public UpperBodyAnimations CurrentState { get; private set; }

    public void Initialize(UpperBodyAnimations currentState)
    {
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(UpperBodyAnimations nextState, bool canSameEnter = false)
    {
        if (!canSameEnter && CurrentState == nextState)
            return;

        CurrentState.Exit();

        nextState.Enter();

        CurrentState = nextState;
    }
}
