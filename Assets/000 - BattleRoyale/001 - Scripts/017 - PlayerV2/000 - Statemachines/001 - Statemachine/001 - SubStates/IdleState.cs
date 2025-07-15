using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class IdleState : PlayerOnGround
{
    public IdleState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void NetworkUpdate()
    {
        characterController.Move(playerMovement.MoveDirection, playerMovement.JumpImpulse);

        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {

        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

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
            playablesChanger.ChangeState(playerPlayables.basicMovement.HitPlayable);
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

        if (playerMovement.Attacking)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.Punch1Playable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);
            return;
        }
    }
}
