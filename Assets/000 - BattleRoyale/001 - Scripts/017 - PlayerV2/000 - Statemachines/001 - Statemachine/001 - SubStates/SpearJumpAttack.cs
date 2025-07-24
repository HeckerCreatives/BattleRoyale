using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SpearJumpAttack : AnimationPlayable
{
    float timer;
    bool canAction;
    bool hasResetHitEnemies;

    public SpearJumpAttack(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        hasResetHitEnemies = false;
        playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
        timer = playerPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
        canAction = false;
    }


    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        if (!hasResetHitEnemies)
        {
            playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
            hasResetHitEnemies = true;
        }

        playerPlayables.inventory.PrimaryWeapon.DamagePlayer();

        if (characterController.IsGrounded && canAction && playerPlayables.TickRateAnimation >= timer)
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
                playablesChanger.ChangeState(playerPlayables.basicMovement.SpearSprintPlayable);

            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.SpearRunPlayable);
        }
        else
            playablesChanger.ChangeState(playerPlayables.basicMovement.SpearIdlePlayable);
    }
}
