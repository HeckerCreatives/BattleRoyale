using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperSwordFinalAttack : UpperNoAimState
{
    bool hasResetHitEnemies;

    public PlayerUpperSwordFinalAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.inventory.PrimaryWeapon.SoundController.PlayAttackOne();

            return;
        }

        hasResetHitEnemies = false;
        playerPlayables.FinalAttack = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        double animTime = animationClipPlayable.GetTime();
        double normalizedTime = animTime / animationLength;

        HandleDamageWindow(normalizedTime);

        var nextState = GetNextState(normalizedTime);

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private void HandleDamageWindow(double normalizedTime)
    {
        if (!playerPlayables.HasStateAuthority) return;
        // original:
        // damageWindowStart = +0.2f
        // damageWindowEnd   = +0.8f
        // these are absolute seconds from animation start
        double damageStartNormalized = 0.2 / animationLength;
        double damageEndNormalized = 0.8 / animationLength;

        if (normalizedTime >= damageStartNormalized && normalizedTime <= damageEndNormalized)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
                hasResetHitEnemies = true;
            }

            playerPlayables.inventory.PrimaryWeapon.DamagePlayer(true);
        }
    }

    private UpperBodyAnimations GetNextState(double normalizedTime)
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.upperBodyMovement.StaggerHitPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsSprint && (playerMovement.XMovement != 0f || playerMovement.YMovement != 0f))
            return playerPlayables.upperBodyMovement.SwordSprint;


        // original:
        // timer = TickRateAnimation + animationLength
        // meaning transition after full clip
        if (normalizedTime >= 1.0)
            return playerPlayables.upperBodyMovement.SwordIdlePlayable;

        return null;
    }
}
