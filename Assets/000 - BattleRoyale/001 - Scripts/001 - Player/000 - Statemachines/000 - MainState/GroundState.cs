using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundState : PlayerStatemachine
{
    public GroundState(PlayerStateChanger changer, MovementData movementData, Animator playerAnimator, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, playerAnimator, controller, gameController, environment, animationName)
    {
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (AnimationExiting) return;

        if (AnimationFinished) return;

        if (Environment.Grounded) return;

        Changer.ChangeState(Controller.Air);
    }
}
