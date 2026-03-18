using Fusion.Addons.SimpleKCC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerFistFinalPunch : UpperNoAimState
{
    bool doneResetHit;

    public PlayerFistFinalPunch(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.fistSoundController.PlayAttackOne();
            return;
        }

        doneResetHit = false;
        playerPlayables.FinalAttack = false;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.SetPunchRotation(0f);
    }

    public override void NetworkLocalUpdate()
    {
        base.NetworkLocalUpdate();

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            playerPlayables.SetPunchRotation(1f);
        else
            playerPlayables.SetPunchRotation(0f);
    }


    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            playerPlayables.SetPunchRotation(1f);
        else
            playerPlayables.SetPunchRotation(0f);

        double animTime = Math.Min(animationClipPlayable.GetTime(), animationLength);

        HandleDamageWindow(animTime);

        var nextState = GetNextState(animTime);

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private void HandleDamageWindow(double animTime)
    {
        if (animTime >= 0.45 && animTime <= 0.50)
        {
            if (!doneResetHit)
            {
                playerPlayables.upperBodyMovement.ResetFirstAttack();
                doneResetHit = true;
            }

            playerPlayables.upperBodyMovement.PerformFirstAttack(true);
        }
    }

    private UpperBodyAnimations GetNextState(double animTime)
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.upperBodyMovement.StaggerHitPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsSprint && (playerMovement.XMovement != 0f || playerMovement.YMovement != 0f))
            return playerPlayables.upperBodyMovement.SprintPlayables;

        if (animTime >= animationLength * 0.9f)
            return playerPlayables.upperBodyMovement.IdlePlayables;

        return null;
    }
}
