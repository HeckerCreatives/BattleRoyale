using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SwordSprintState : PlayerOnGround
{
    public SwordSprintState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        Animation();

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

        playerPlayables.stamina.DecreaseStamina(20f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPlayable);

        if (playerMovement.IsBlocking)
            playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);

        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordAttackFirstPlayable);

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.basicMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.TrappingPlayable);
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

        if (playerPlayables.stamina.Stamina > 0f)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);
            }
            else if (!playerMovement.IsSprint)
            {
                if (playerMovement.MoveDirection == Vector3.zero)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);
            }
        }
        else
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);
        }
    }
}
