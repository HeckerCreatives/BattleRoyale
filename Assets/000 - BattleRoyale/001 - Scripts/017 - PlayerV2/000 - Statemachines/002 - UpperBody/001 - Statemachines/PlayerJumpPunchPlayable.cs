using Fusion.Addons.SimpleKCC;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerJumpPunchPlayable : UpperNoAimState
{
    bool hasResetHitEnemies;

    public PlayerJumpPunchPlayable(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper) 
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority) return;

        playerPlayables.healthV2.FallDamageValue = 0;
        hasResetHitEnemies = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        double animTime = Math.Min(animationClipPlayable.GetTime(), animationLength);

        HandleDamage(animTime);

        if (!characterController.IsGrounded || animTime < animationLength)
            return;

        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetNextState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }
        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetNextState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }

    }

    private void HandleDamage(double animTime)
    {
        // If you want full-clip active hit detection, keep this exactly like this.
        if (!hasResetHitEnemies)
        {
            playerPlayables.upperBodyMovement.ResetSecondAttack();
            hasResetHitEnemies = true;
        }

        playerPlayables.upperBodyMovement.PerformSecondAttack();
    }

    private UpperBodyAnimations GetNextState()
    {
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;

        if (isMoving)
        {
            return playerMovement.IsSprint
                ? playerPlayables.upperBodyMovement.SprintPlayables
                : playerPlayables.upperBodyMovement.RunPlayables;
        }

        return playerPlayables.upperBodyMovement.IdlePlayables;
    }
}
