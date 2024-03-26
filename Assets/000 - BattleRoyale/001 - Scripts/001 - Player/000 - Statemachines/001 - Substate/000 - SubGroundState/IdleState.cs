using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : GroundState
{
    public IdleState(PlayerStateChanger changer, MovementData movementData, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, controller, gameController, environment, animationName)
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

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        //Environment.PlayerRB.rotation = Quaternion.identity;
    }

    private void AnimationChanger()
    {
        if (!AnimationExiting)
        {
            if (Environment.Grounded)
            {
                //if (GameControl.Jump)
                //{
                //    Changer.ChangeState(Controller.Jump);
                //}
                if (GameControl.MovementDirection != Vector2.zero)
                {
                    //Direction.FlipPlayer(GameControl.MovementDirection);
                    Changer.ChangeState(Controller.Move);
                }
            }
            else
            {
                //Changer.ChangeState(Controller.InAir);
            }
        }
    }
}
