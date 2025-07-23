using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SwordRunState : PlayerOnGround
{
    public SwordRunState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();
        Animation();
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
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
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPlayable);

        if (playerMovement.IsBlocking)
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordBlockPlayable);

        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordAttackFirstPlayable);

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);

        else if (playerMovement.IsSprint)
        {
            if (playerPlayables.stamina.Stamina >= 10f)
                playablesChanger.ChangeState(playerPlayables.basicMovement.SwordSprintPlayable);
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

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.basicMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.TrappingPlayable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);
    }
}
