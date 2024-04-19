using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandingState : GroundState
{
    public LandingState(PlayerStateChanger changer, MovementData movementData, Animator playerAnimator, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
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
        AnimationChanger();
    }

    private void AnimationChanger()
    {
        Debug.Log(AnimationFinished);
        if (AnimationExiting) return;

        if (!Environment.Grounded) return;

        if (!AnimationFinished) return;

        if (GameControl.MovementDirection != Vector2.zero)
            Changer.ChangeState(Controller.Move);
        else
            Changer.ChangeState(Controller.Idle);
    }
}
