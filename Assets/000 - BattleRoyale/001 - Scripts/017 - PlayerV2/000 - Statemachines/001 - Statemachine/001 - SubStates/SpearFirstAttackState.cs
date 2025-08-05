using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SpearFirstAttackState : PlayerOnGround
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

    public SpearFirstAttackState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
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
        damageWindowStart = playerPlayables.TickRateAnimation + (animationLength * 0.5f);
        damageWindowEnd = playerPlayables.TickRateAnimation + (animationLength * 0.9f);
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
                playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies(); // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }

            playerPlayables.inventory.PrimaryWeapon.DamagePlayer();
        }

        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);
            return;
        }


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

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

            return;
        }

        if (playerPlayables.TickRateAnimation >= nextPunchWindow && canAction)
        {
            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearFinalAttackPlayable);
                return;
            }
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.IsBlocking)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearBlockPlayable);
                return;
            }

            if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);
                return;
            }

            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearIdlePlayable);
        }
    }
}
