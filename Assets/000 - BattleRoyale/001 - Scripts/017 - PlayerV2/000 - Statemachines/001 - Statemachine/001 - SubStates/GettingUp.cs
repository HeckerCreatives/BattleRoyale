using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class GettingUp : PlayerOnGround
{
    float timer;
    bool canAction;

    public GettingUp(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }

    public override void NetworkUpdate()
    {
        Animation();
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

        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= timer)
            {

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

                if (playerPlayables.healthV2.IsStagger)
                {
                    playablesChanger.ChangeState(playerPlayables.basicMovement.StaggerHitPlayable);
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

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
                }
            }
        }
    }
}
