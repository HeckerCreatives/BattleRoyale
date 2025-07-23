using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SwordIdleState : PlayerOnGround
{
    public SwordIdleState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {

        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);
            return;
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
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordBlockPlayable);
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

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.TrappingPlayable);
            return;
        }

        if (playerMovement.IsHealing)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.HealPlayable);
            return;
        }

        if (playerMovement.IsRepairing)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.RepairPlayable);
            return;
        }

        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        {
            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                playablesChanger.ChangeState(playerPlayables.basicMovement.SwordSprintPlayable);

            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);

            return;
        }

        if (playerMovement.Attacking)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordAttackFirstPlayable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);
            return;
        }
    }
}
