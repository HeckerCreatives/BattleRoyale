using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : GroundState
{
    public IdleState(PlayerStateChanger changer, MovementData movementData, Animator playerAnimator, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, playerAnimator, controller, gameController, environment, animationName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        Environment.SetVelocityZero();
    }

    public override void LogicUpdate()
    {
        base.Enter();

        AnimationChanger();
    }

    private void AnimationChanger()
    {
        if (AnimationExiting) return;

        if (AnimationFinished) return;

        if (Environment.Grounded)
        {
            if (GameControl.Jump)
                Changer.ChangeState(Controller.Jump);
            if (GameControl.MovementDirection != Vector2.zero)
                Changer.ChangeState(Controller.Move);
        }
        else
            Changer.ChangeState(Controller.Air);
    }
}
