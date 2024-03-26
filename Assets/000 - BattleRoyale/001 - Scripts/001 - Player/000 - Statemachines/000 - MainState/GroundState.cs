using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundState : PlayerStatemachine
{
    public GroundState(PlayerStateChanger changer, MovementData movementData,
        PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, controller, gameController, environment, animationName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        // AIR STATE HERE
        if (!AnimationExiting)
        {
            if (!Environment.Grounded)
            {
                //  PUT IN AIR HERE
                //Changer.ChangeState(Controller.InAir);
            }
        }
    }
}
