using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class HealState : AnimationPlayable
{
    float healtimer;
    float timer;
    bool canAction;
    bool doneHeal;

    public HealState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        healtimer = playerPlayables.TickRateAnimation + (animationLength * 0.5f);
        timer = playerPlayables.TickRateAnimation + animationLength;

        if (playerPlayables.inventory.PrimaryWeapon != null && playerPlayables.inventory.WeaponIndex == 2) playerPlayables.inventory.PrimaryWeapon.IsEquipped = false;

        doneHeal = false;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.inventory.PrimaryWeapon != null && playerPlayables.inventory.WeaponIndex == 2) playerPlayables.inventory.PrimaryWeapon.IsEquipped = true;

        doneHeal = false;
        canAction = false;
    }

    public override void NetworkUpdate()
    {
        if (!doneHeal && playerPlayables.TickRateAnimation > healtimer)
        {
            playerPlayables.healthV2.HealHealth();
            doneHeal = true;
        }

        if (playerPlayables.TickRateAnimation < healtimer)
        {
            Animations();
        }

        if (canAction && playerPlayables.TickRateAnimation >= timer)
        {
            
            Animations();
        }
    }

    private void Animations()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);
        }

        if (!characterController.IsGrounded)
        {
            
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
            return;
        }

        if (playerMovement.IsJumping)
        {
            
            playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPlayable);
            return;
        }

        if (playerMovement.IsBlocking)
        {
            
            playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);
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

        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        {
            

            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);

            return;
        }

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
        {
            if (canAction && playerPlayables.TickRateAnimation >= timer)
            {
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
                canAction = false;
                return;
            }
        }

        if (playerMovement.Attacking)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.Punch1Playable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);
            return;
        }
    }
}
