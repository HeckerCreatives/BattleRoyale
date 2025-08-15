using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSwordMiddleAttack : UpperNoAimState
{
    float timer;
    float nextPunchWindow;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    float moveTimer;
    float stopMoveTimer;
    bool canMove;
    bool hasResetHitEnemies;

    public PlayerUpperSwordMiddleAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();


        playerPlayables.inventory.PrimaryWeapon.SoundController.PlayAttackTwo();

        hasResetHitEnemies = false;
        timer = playerPlayables.TickRateAnimation + (animationLength * 0.9f);
        nextPunchWindow = playerPlayables.TickRateAnimation + (animationLength - 0.2f);
        damageWindowStart = playerPlayables.TickRateAnimation + 0.22f;
        damageWindowEnd = playerPlayables.TickRateAnimation + 0.27f;
        moveTimer = playerPlayables.TickRateAnimation + (animationLength * 0.3f);
        stopMoveTimer = playerPlayables.TickRateAnimation + (animationLength * 0.5f);
        canAction = true;
        canMove = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
        canMove = false;
    }


    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

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
            
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
            
        }

        //if (playerPlayables.healthV2.IsHitUpper)
        //{
        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
        //    
        //}

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
            
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);

        }

        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= nextPunchWindow && playerMovement.Attacking)
            {
                playerPlayables.FinalAttack = true;
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordFinalAttackPlayable);
                
            }

            if (playerPlayables.TickRateAnimation >= timer && canAction)
            {
                if (playerMovement.IsBlocking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordBlockPlayable);
                    
                }

                if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
                    
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
}
