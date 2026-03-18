using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperStagger : UpperNoAimState
{

    public PlayerUpperStagger(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetNextUpperStaggerState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }
        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetNextUpperStaggerState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }

    }

    private UpperBodyAnimations GetNextUpperStaggerState()
    {
        var upper = playerPlayables.upperBodyMovement;

        if (playerPlayables.healthV2.IsDead)
            return upper.DeathPlayable;

        if (animationClipPlayable.GetTime() < animationLength - 0.025f)
            return this;

        playerPlayables.healthV2.IsStagger = false;

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        if (playerMovement.IsRoll)
            return upper.RollPlayables;

        return upper.GettingUpPlayable;
    }
}
