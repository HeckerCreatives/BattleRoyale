using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSwordJumpAttack : UpperBodyAnimations
{
    float timer;
    bool canAction;
    bool hasResetHitEnemies;

    public PlayerUpperSwordJumpAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.healthV2.FallDamageValue = 0;
        hasResetHitEnemies = false;
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
        playerMovement.MoveCharacter();

        if (!hasResetHitEnemies)
        {
            playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
            hasResetHitEnemies = true;
        }

        playerPlayables.inventory.PrimaryWeapon.DamagePlayer();

        if (characterController.IsGrounded && canAction && playerPlayables.TickRateAnimation >= timer) Animation();
    }

    private void Animation()
    {
        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        {
            if (playerMovement.IsSprint)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);

            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
        }
        else
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
    }
}
