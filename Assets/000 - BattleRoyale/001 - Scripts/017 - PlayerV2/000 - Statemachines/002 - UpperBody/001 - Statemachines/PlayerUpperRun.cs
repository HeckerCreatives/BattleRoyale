using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRun : UpperNoAimState
{
    public PlayerUpperRun(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();
        Animation();
        WeaponsChecker();
    }

    private void Animation()
    {
        if (playerMovement.Attacking)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);
            
        }

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);

        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
            
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

        if (playerMovement.IsJumping)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
            
        }

        if (playerMovement.IsBlocking)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BlockPlayable);
            
        }

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.TrapPlayable);
            
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
            
        }
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);

            if (playerMovement.IsSprint)
            {
                if (playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);
            }
        }
    }
}
