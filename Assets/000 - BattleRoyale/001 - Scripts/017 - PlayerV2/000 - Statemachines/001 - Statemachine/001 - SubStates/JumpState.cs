using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class JumpState : PlayerOnGround
{
    public JumpState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (!playerMovement.IsJumping)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
        }

        if (playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPunchPlayable);
            else if (playerPlayables.inventory.WeaponIndex == 2)
                playablesChanger.ChangeState(playerPlayables.basicMovement.SwordJumpAttackPlayable);
        }

        if (characterController.IsGrounded)
        {
            playerMovement.IsJumping = false;
            playerMovement.JumpImpulse = 0;

            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
            }
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);
            }
        }
    }

    private void RenderAnimation()
    {
        if (!playerMovement.IsJumping)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
        }

        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPunchPlayable);

        if (characterController.IsGrounded)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
        }
    }
}
