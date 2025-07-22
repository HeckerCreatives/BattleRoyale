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
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool canMove;

    public FinalPunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        moveTimer = playerPlayables.TickRateAnimation + 0.30f;
        stopMoveTimer = playerPlayables.TickRateAnimation + 0.50f;
        damageWindowStart = playerPlayables.TickRateAnimation + 0.45f;
        damageWindowEnd = playerPlayables.TickRateAnimation + 0.50f;
        canAction = true;
        canMove = true;
    }

    public override void Exit()
    {
        base.Exit();
        playerPlayables.basicMovement.ResetFirstAttack();
        canAction = false;
    }


    public override void NetworkUpdate()
    {
        if (playerPlayables.TickRateAnimation >= damageWindowStart && playerPlayables.TickRateAnimation <= damageWindowEnd)
        {
            playerPlayables.basicMovement.PerformFinalAttack();
        }

        Animation();

        if (playerPlayables.TickRateAnimation >= moveTimer && playerPlayables.TickRateAnimation <= stopMoveTimer)
        {
            characterController.Move(characterController.TransformDirection * 1.25f, 0f);
            canMove = false;
        }

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private  void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);


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

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.MoveDirection != Vector3.zero)
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
