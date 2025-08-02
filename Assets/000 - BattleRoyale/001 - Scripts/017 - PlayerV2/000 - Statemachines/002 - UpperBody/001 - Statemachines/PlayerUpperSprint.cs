using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSprint : UpperBodyAnimations
{
    public PlayerUpperSprint(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.WeaponSwitcher();
        Animation();
        WeaponsChecker();
    }

    private void Animation()
    {
        //if (playerPlayables.healthV2.IsDead)
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

        //if (!characterController.IsGrounded)
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

        //if (playerMovement.IsJumping)
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPlayable);

        //if (playerMovement.IsBlocking)
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);

        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);

        //if (playerMovement.IsHealing)
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HealPlayable);

        //if (playerMovement.IsRepairing)
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RepairPlayable);

        //if (playerMovement.IsTrapping)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.TrappingPlayable);
        //    return;
        //}

        //if (playerPlayables.healthV2.IsHit)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
        //    return;
        //}

        //if (playerPlayables.healthV2.IsSecondHit)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.MiddleHitPlayable);
        //    return;
        //}

        //if (playerPlayables.healthV2.IsStagger)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
        //    return;
        //}

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
            return;
        }
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.stamina.Stamina > 0f)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (!playerMovement.IsSprint)
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
                }

                return;
            }
            //else if (playerPlayables.inventory.WeaponIndex == 2)
            //{
            //    if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "001")
            //    {
            //        if (playerMovement.MoveDirection != Vector3.zero)
            //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);

            //        return;
            //    }

            //    if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "002")
            //    {
            //        if (playerMovement.MoveDirection != Vector3.zero)
            //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);

            //        return;
            //    }
            //}
        }
        else
        {
            if (playerMovement.MoveDirection == Vector3.zero)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
        }
    }
}
