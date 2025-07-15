using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class HitState : PlayerOnGround
{
    float timer;
    bool canAction;

    public HitState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (!characterController.IsGrounded)
        {
            playerPlayables.healthV2.IsHit = false;
            playerPlayables.healthV2.IsSecondHit = false;

            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playerPlayables.healthV2.IsHit = false;
            playerPlayables.healthV2.IsSecondHit = false;


            playablesChanger.ChangeState(playerPlayables.basicMovement.StaggerHitPlayable);
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            playerPlayables.healthV2.IsHit = false;
            playerPlayables.healthV2.IsSecondHit = false;

            if (playerMovement.IsRoll)
                playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

            if (playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.Punch1Playable);

            if (playerMovement.IsBlocking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
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
