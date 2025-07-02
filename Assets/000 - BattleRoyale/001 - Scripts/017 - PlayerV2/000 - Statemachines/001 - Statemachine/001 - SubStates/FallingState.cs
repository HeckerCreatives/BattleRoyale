using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class FallingState : AnimationPlayable
{
    public FallingState(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        Animation();
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();
        Animation();
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPunchPlayable);

        if (characterController.IsGrounded)
        {
            playerMovement.JumpImpulse = 0;

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
            else if (playerMovement.IsSprint)
            {
                if (playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
        }
    }
}
