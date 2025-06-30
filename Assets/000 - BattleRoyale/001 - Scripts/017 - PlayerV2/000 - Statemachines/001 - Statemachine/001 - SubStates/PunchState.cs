using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PunchState : PlayerOnGround
{
    float timer;
    float nextPunchWindow;
    float moveTimer;
    bool canAction;
    bool canMove;

    public PunchState(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        nextPunchWindow = playerPlayables.TickRateAnimation + (animationLength - 0.2f);
        moveTimer = playerPlayables.TickRateAnimation + 0.30f;
        canAction = true;
        canMove = true;
    }

    public override void Exit()
    {
        base.Exit();
        canAction = false;
    }

    public override void NetworkUpdate()
    {
        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);

        if (playerPlayables.TickRateAnimation >= nextPunchWindow && canAction)
        {
            if (playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.Punch2Playable);
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.IsBlocking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);

            if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
                playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
        }

        if (playerPlayables.TickRateAnimation >= moveTimer && canMove)
        {
            characterController.Move(characterController.TransformDirection * 25f, 0f);
            canMove = false;
        }
    }
}
