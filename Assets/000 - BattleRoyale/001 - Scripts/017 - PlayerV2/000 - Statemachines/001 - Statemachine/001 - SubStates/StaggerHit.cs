using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class StaggerHit : PlayerOnGround
{
    float timer;
    float moveTimer;

    public StaggerHit(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        moveTimer = playerPlayables.TickRateAnimation + 0.8f;
    }

    public override void NetworkUpdate()
    {
        if (animationClipPlayable.GetTime() < animationLength - (animationLength * 0.5f))
            characterController.Move(playerMovement.MainCharObj.forward * -5f, 0f);

        playerPlayables.stamina.RecoverStamina(5f);

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

    private AnimationPlayable GetNextState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (animationClipPlayable.GetTime() >= animationLength - 0.02f)
            return null;

        playerPlayables.healthV2.IsStagger = false;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (playerMovement.IsRoll)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        return playerPlayables.lowerBodyMovement.GettingUpPlayable;
    }
}
