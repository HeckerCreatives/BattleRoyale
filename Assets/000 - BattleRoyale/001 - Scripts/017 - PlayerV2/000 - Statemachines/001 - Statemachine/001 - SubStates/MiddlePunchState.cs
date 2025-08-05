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

    public MiddlePunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
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

        canAction = false;
    }


    public override void NetworkUpdate()
    {
        playerMovement.RotatePlayer();

        //if (playerPlayables.TickRateAnimation >= damageWindowStart && playerPlayables.TickRateAnimation <= damageWindowEnd)
        //{
        //    if (!hasResetHitEnemies)
        //    {
        //        playerPlayables.lowerBodyMovement.ResetSecondAttack(); // Clear BEFORE performing attack
        //        hasResetHitEnemies = true;
        //    }
        //    playerPlayables.lowerBodyMovement.PerformSecondAttack();
        //}

        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

        if (playerPlayables.healthV2.IsHit)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
            return;
        }


        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= nextPunchWindow && playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.Punch3Playable);

            if (playerPlayables.TickRateAnimation >= timer)
            {
                if (playerMovement.IsBlocking)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);

                if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);

                if (playerMovement.MoveDirection != Vector3.zero)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);
            }
        }
    }
}
