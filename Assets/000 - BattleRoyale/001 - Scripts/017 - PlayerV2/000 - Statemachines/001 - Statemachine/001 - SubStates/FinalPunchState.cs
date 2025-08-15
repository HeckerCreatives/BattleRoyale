using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class FinalPunchState : PlayerOnGround
{
    float timer;
    float moveTimer;
    float stopMoveTimer;
    bool canAction;

    public FinalPunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        moveTimer = playerPlayables.TickRateAnimation + 0.30f;
        stopMoveTimer = playerPlayables.TickRateAnimation + 0.50f;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();
        canAction = false;
    }


    public override void NetworkUpdate()
    {
        if (playerPlayables.TickRateAnimation >= moveTimer && playerPlayables.TickRateAnimation <= stopMoveTimer)
        {
            characterController.Move(characterController.TransformDirection * 1.25f, 0f);
        }

        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);


        //if (playerPlayables.healthV2.IsHit)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
        //    return;
        //}

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
            return;
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);

        }
    }
}
