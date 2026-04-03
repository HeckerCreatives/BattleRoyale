using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperSwordMiddleAttack : UpperNoAimState
{
    bool hasResetHitEnemies;

    public PlayerUpperSwordMiddleAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.inventory.PrimaryWeapon.SoundController.PlayAttackTwo();
            //playerPlayables.SlashSwordParticles(0);

            return;
        }

        hasResetHitEnemies = false;
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

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void HandleDamageWindow(double normalizedTime)
    {
        if (!playerPlayables.HasStateAuthority) return;
            // original:
            // damageWindowStart = +0.22f
            // damageWindowEnd   = +0.27f
            // these were absolute seconds, so convert to normalized time
        double damageStartNormalized = 0.22 / animationLength;
        double damageEndNormalized = 0.27 / animationLength;

        if (normalizedTime >= damageStartNormalized && normalizedTime <= damageEndNormalized)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
                hasResetHitEnemies = true;
            }

            playerPlayables.inventory.PrimaryWeapon.DamagePlayer();
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


        double finalAttackWindowNormalized = (animationLength - 0.2f) / animationLength;

        if (normalizedTime >= finalAttackWindowNormalized && playerMovement.Attacking)
        {
            playerPlayables.FinalAttack = true;
            return playerPlayables.upperBodyMovement.SwordFinalAttackPlayable;
        }

        if (normalizedTime >= 0.9)
            return playerPlayables.upperBodyMovement.SwordIdlePlayable;

        return null;
    }
}
