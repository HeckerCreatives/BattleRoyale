using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSpearIdle : UpperBodyAnimations
{
    public PlayerUpperSpearIdle(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.WeaponSwitcher();
        WeaponsChecker();
        Animation();
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
        }

        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                return;
            }

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);
            }
        }
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
            return;
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
            return;
        }

        if (playerMovement.IsJumping)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
            return;
        }

        if (playerMovement.IsBlocking)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearBlockPlayable);
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

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.TrapPlayable);
            return;
        }

        if (playerMovement.IsHealing)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HealPlayable);
            return;
        }

        if (playerMovement.IsRepairing)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RepairPlayable);
            return;
        }

        if (playerMovement.Attacking)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearFirstAttackPlayable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
            return;
        }
    }
}
