using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerFistFirstPunch : UpperNoAimState
{
    float timer;
    float nextPunchWindow;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool hasResetHitEnemies;

    public PlayerFistFirstPunch(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.fistSoundController.PlayAttackOne();

        hasResetHitEnemies = false;
        timer = playerPlayables.TickRateAnimation + (animationLength * 0.9f);
        nextPunchWindow = playerPlayables.TickRateAnimation + (animationLength * 0.8f);
        damageWindowStart = playerPlayables.TickRateAnimation + (animationLength * 0.18f);
        damageWindowEnd = playerPlayables.TickRateAnimation + (animationLength * 0.23f);
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        if (playerPlayables.TickRateAnimation >= damageWindowStart && playerPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.upperBodyMovement.ResetFirstAttack(); // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }

            playerPlayables.upperBodyMovement.PerformFirstAttack();
        }

        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
        }


        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);

        }
        //if (playerPlayables.healthV2.IsHitUpper)
        //{
        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
        //    return;
        //}

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
        }

        if (playerPlayables.TickRateAnimation >= nextPunchWindow && canAction)
        {
            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SecondPunch);
            }
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {

            if (playerMovement.IsBlocking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BlockPlayable);
            }

            if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
            }

            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
            }
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
        }
    }
}
