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
    float stopMoveTimer;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool canMove;
    bool hasResetHitEnemies;

    public PunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        hasResetHitEnemies = false;
        timer = playerPlayables.TickRateAnimation + (animationLength * 0.9f);
        nextPunchWindow = playerPlayables.TickRateAnimation + (animationLength * 0.8f);
        moveTimer = playerPlayables.TickRateAnimation + (animationLength * 0.3f);
        stopMoveTimer = playerPlayables.TickRateAnimation + (animationLength * 0.5f);
        damageWindowStart = playerPlayables.TickRateAnimation + (animationLength * 0.18f);
        damageWindowEnd = playerPlayables.TickRateAnimation + (animationLength * 0.23f);
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
        playerMovement.RotatePlayer();

        if (playerPlayables.TickRateAnimation >= damageWindowStart && playerPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.basicMovement.ResetFirstAttack(); // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }

            playerPlayables.basicMovement.PerformFirstAttack();
        }

        Animation();

        if (playerPlayables.TickRateAnimation >= moveTimer && playerPlayables.TickRateAnimation <= stopMoveTimer)
        {
            characterController.Move(characterController.TransformDirection * 0.75f, 0f);
            canMove = false;
        }

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);
            return;
        }


        if (playerPlayables.healthV2.IsHit)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsSecondHit)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.MiddleHitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.StaggerHitPlayable);
            return;
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
            return;
        }

        if (playerPlayables.TickRateAnimation >= nextPunchWindow && canAction)
        {
            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.basicMovement.Punch2Playable);
                return;
            }
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {

            if (playerMovement.IsBlocking)
            {
                playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);
                return;
            }

            if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            {
                playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);
                return;
            }

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
    }
}
