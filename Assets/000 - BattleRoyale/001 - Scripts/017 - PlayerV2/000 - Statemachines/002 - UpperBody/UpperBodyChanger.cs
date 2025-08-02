using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpperBodyChanger
{
    public UpperBodyAnimations CurrentState { get; private set; }

    public void Initialize(UpperBodyAnimations currentState)
    {
        CurrentState = currentState;
        CurrentState.Enter();
    }

    public void ChangeState(UpperBodyAnimations nextState)
    {
        CurrentState.Exit();
        nextState.Enter();
        CurrentState = nextState;
    }
}
