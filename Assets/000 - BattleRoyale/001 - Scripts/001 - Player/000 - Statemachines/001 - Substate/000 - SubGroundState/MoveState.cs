using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveState : GroundState
{
    Vector3 inputVector;

    public MoveState(PlayerStateChanger changer, MovementData movementData, Animator playerAnimator, PlayerStateController controller, GameplayController gameController, PlayerEnvironment environment, string animationName) :
        base(changer, movementData, playerAnimator, controller, gameController, environment, animationName)
    {
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void LogicUpdate()
    {
        ChangeAnimation();

        //Direction.FlipPlayer(GameControl.MovementDirection);
    }

    public override void PhysicsUpdate()
    {
        Environment.CalculateFoward();
        Environment.CalculateGroundAngle();
        MovePlayer();
    }

    private void MovePlayer()
    {
        inputVector = new Vector3(GameControl.MovementDirection.x, 0f, GameControl.MovementDirection.y).normalized;
        inputVector = GameControl.mainCamera.transform.TransformDirection(inputVector);
        inputVector.y = 0f;
        inputVector = inputVector.normalized;

        Environment.ApplySlopeGravity();
        Environment.SetVelocityMovement(inputVector * MovementData.MovementSpeed);

        Environment.PlayerRB.rotation = Quaternion.Slerp(Environment.PlayerRB.rotation,
                        Quaternion.LookRotation(inputVector.normalized), 20 *
                        Time.fixedDeltaTime);
    }

    private void ChangeAnimation()
    {
        if (AnimationExiting) return;


        if (GameControl.Jump)
            Changer.ChangeState(Controller.Jump);
        else if (GameControl.MovementDirection == Vector2.zero)
            Changer.ChangeState(Controller.Idle);
    }
}
