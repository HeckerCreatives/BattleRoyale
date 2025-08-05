using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSwordFirstAttack : UpperBodyAnimations
{
    float timer;
    float nextPunchWindow;
    float moveTimer;
    float stopMoveTimer;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool hasResetHitEnemies;

    public PlayerUpperSwordFirstAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }

    public override void NetworkUpdate()
    {
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
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
            return;
        }


        if (playerPlayables.healthV2.IsHitUpper)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
            return;
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);

            return;
        }

        if (playerPlayables.TickRateAnimation >= nextPunchWindow && canAction)
        {
            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackSecondPlayable);
                return;
            }
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.IsBlocking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordBlockPlayable);
                return;
            }

            if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
                return;
            }


            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
        }
    }
}
