using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirState : PlayerStatemachine
{
    Vector3 inputVector;
    public AirState(PlayerStateChanger changer, MovementData movementData, Animator playerAnimator, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, playerAnimator, controller, gameController, environment, animationName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        //Environment.SetInAirMat();
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        AnimationChange();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        MoveInAir();
    }

    private void MoveInAir()
    {
        if (GameControl.MovementDirection == Vector2.zero) return;

        inputVector = new Vector3(GameControl.MovementDirection.x, 0f, GameControl.MovementDirection.y).normalized;
        inputVector = GameControl.mainCamera.transform.TransformDirection(inputVector);
        inputVector.y = 0f;
        inputVector = inputVector.normalized;

        Environment.SetVelocityMovement(inputVector * MovementData.AirSpeed);

        Environment.PlayerRB.rotation = Quaternion.Slerp(Environment.PlayerRB.rotation,
                        Quaternion.LookRotation(inputVector.normalized), 20 *
                        Time.fixedDeltaTime);
    }

    private void AnimationChange()
    {
        if (AnimationExiting) return;

        if (!Environment.Grounded) return;

        Changer.ChangeState(Controller.Landing);
    }
}
