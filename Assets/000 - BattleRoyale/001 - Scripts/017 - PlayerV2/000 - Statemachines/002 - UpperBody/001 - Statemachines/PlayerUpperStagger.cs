using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperStagger : UpperNoAimState
{
    float timer;
    bool canAction;

    public PlayerUpperStagger(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
        base.NetworkUpdate();

        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= timer)
            {
                playerPlayables.healthV2.IsStagger = false;
                if (!characterController.IsGrounded)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
                    return;
                }

                if (playerMovement.IsRoll)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.GettingUpPlayable);
            }
        }
    }
}
