using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerFistFinalPunch : UpperBodyAnimations
{
    float timer;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool doneResetHit;

    public PlayerFistFinalPunch(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        doneResetHit = false;
        timer = playerPlayables.TickRateAnimation + animationLength;
        damageWindowStart = playerPlayables.TickRateAnimation + 0.45f;
        damageWindowEnd = playerPlayables.TickRateAnimation + 0.50f;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();
        playerPlayables.FinalAttack = false;
        canAction = false;
    }

    public override void NetworkUpdate()
    {
        if (playerPlayables.TickRateAnimation >= damageWindowStart && playerPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!doneResetHit)
            {
                playerPlayables.upperBodyMovement.ResetFirstAttack();
                doneResetHit = true;
            }

            playerPlayables.upperBodyMovement.PerformFirstAttack(true);
        }

        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);


        if (playerPlayables.healthV2.IsHitUpper)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
            return;
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
            }
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);

        }
    }
}
