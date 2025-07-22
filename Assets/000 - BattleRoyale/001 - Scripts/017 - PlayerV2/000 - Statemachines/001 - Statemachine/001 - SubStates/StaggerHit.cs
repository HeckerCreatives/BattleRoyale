using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class StaggerHit : PlayerOnGround
{
    float timer;
    float moveTimer;
    bool canAction;

    public StaggerHit(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        moveTimer = playerPlayables.TickRateAnimation + 0.8f;
        canAction = true;
    }

    public override void Exit() 
    {
        base.Exit();

        canAction = false;
    }


    public override void NetworkUpdate()
    {
        if (canAction)
        {
            if (playerPlayables.TickRateAnimation < moveTimer)
                characterController.Move(characterController.TransformDirection * -5f, 0f);
        }

        Animation();
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= timer)
            {
                playerPlayables.healthV2.IsStagger = false;

                if (!characterController.IsGrounded)
                {
                    playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
                    return;
                }

                if (playerMovement.IsRoll)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.GettingUpPlayable);
            }
        }
    }
}
