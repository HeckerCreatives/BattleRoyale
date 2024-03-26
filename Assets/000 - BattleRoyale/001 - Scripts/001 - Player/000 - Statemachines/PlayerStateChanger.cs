using System.Collections;
using System.Collections.Generic;

public class PlayerStateChanger
{
    public PlayerStatemachine CurrentState { get; private set; }

    public void Initialize(PlayerStatemachine startingState)
    {
        CurrentState = startingState;
        CurrentState.Enter();
    }

    public void ChangeState(PlayerStatemachine newState)
    {
        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }
}
