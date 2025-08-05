using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSwordRun : UpperBodyAnimations
{
    public PlayerUpperSwordRun(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.WeaponSwitcher();
        WeaponsChecker();
        Animation();
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {

        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);

        //if (playerMovement.IsBlocking)
        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordBlockPlayable);

        //if (playerMovement.Attacking)
        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);

        if (playerPlayables.healthV2.IsHit)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsSecondHit)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.MiddleHitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
            return;
        }

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.TrapPlayable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            //if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "002")
            //{
            //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
            //    return;
            //}

            //if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);

            //if (playerMovement.IsSprint)
            //{
            //    if (playerPlayables.stamina.Stamina >= 10f)
            //        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);
            //}
        }
    }
}
