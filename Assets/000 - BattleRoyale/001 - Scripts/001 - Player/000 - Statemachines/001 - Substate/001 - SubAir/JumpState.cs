using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpState : GroundState
{
    Vector3 inputVector;
    public bool Jumping { get; private set; }

    public JumpState(PlayerStateChanger changer, MovementData movementData, Animator playerAnimator, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, playerAnimator, controller, gameController, environment, animationName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        Jumping = true;

        if (Environment.Grounded)
            Environment.SetVelocityY(MovementData.JumpForce);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (AnimationExiting) return;

        if (!AnimationFinished) return;

        if (!Environment.Grounded) return;

        Changer.ChangeState(Controller.Idle);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        JumpHighLow();
    }

    private void JumpHighLow()
    {
        if (GameControl.JumpStop)
        {
            Environment.SetVelocityY(Environment.CurrentVelocity.y * MovementData.JumpHeightMultiplier);
            GameControl.JumpTurnOff();
            Jumping = false;
        }

        else if (!GameControl.Jump) GameControl.JumpTurnOff();
    }
}
