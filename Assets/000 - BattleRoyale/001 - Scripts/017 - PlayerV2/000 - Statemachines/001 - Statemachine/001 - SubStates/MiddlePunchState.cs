using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class MiddlePunchState : PlayerOnGround
{
    float timer;
    float nextPunchWindow;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool hasResetHitEnemies;

    public MiddlePunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        hasResetHitEnemies = false;
        timer = playerPlayables.TickRateAnimation + (animationLength * 0.9f);
        nextPunchWindow = playerPlayables.TickRateAnimation + (animationLength - 0.2f);
        damageWindowStart = playerPlayables.TickRateAnimation + 0.22f;
        damageWindowEnd = playerPlayables.TickRateAnimation + 0.27f;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.basicMovement.ResetSecondAttack();
        canAction = false;
    }


    public override void NetworkUpdate()
    {
        playerMovement.RotatePlayer();

        if (playerPlayables.TickRateAnimation >= damageWindowStart && playerPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.basicMovement.ResetSecondAttack(); // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }
            playerPlayables.basicMovement.PerformSecondAttack();
        }

        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);

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


        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= nextPunchWindow && playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.Punch3Playable);

            if (playerPlayables.TickRateAnimation >= timer)
            {
                if (playerMovement.IsBlocking)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);

                if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
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
        }
    }
}
