using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSwordSprint : UpperNoAimState
{
    public PlayerUpperSwordSprint(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        WeaponsChecker();
        Animation();

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);

        if (playerMovement.IsBlocking)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordBlockPlayable);

        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.TrapPlayable);
            
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
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.stamina.Stamina > 0f)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.MoveDirection != Vector3.zero)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                
            }
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "002")
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);
                }
            }
            else if (playerPlayables.inventory.WeaponIndex == 3)
            {
                if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);
                }
                else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);
                }
            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
            }
            else if (!playerMovement.IsSprint)
            {
                if (playerMovement.MoveDirection != Vector3.zero)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
            }
        }
        else
        {
            if (playerMovement.MoveDirection == Vector3.zero)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
        }
    }
}
