using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class JumpPunchState : PlayerOnGround
{
    public JumpPunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        if (characterController.IsGrounded)
        {
            playerMovement.IsJumping = false;
            playerMovement.JumpImpulse = 0;
            Animation();
        }

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
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
