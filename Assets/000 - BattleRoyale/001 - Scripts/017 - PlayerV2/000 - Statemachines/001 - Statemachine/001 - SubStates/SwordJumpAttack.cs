using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SwordJumpAttack : AnimationPlayable
{
    float timer;
    bool canAction;

    public SwordJumpAttack(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.healthV2.FallDamageValue = 0;
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
        if (characterController.IsGrounded && canAction && playerPlayables.TickRateAnimation >= timer)
        {
            playerMovement.IsJumping = false;
            playerMovement.JumpImpulse = 0;

            if (playerPlayables.healthV2.FallDamageValue > 0)
                playerPlayables.healthV2.FallDamae();

            Animation();
        }

        FallDamage();
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void FallDamage()
    {
        if (characterController.RealVelocity.y <= -20f)
        {
            playerPlayables.healthV2.FallDamageValue = Mathf.Abs(characterController.RealVelocity.y) - 5f;
        }
    }

    private void Animation()
    {
        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        {
            if (playerMovement.IsSprint)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);

            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
        }
        else
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
    }
}
