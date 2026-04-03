using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SpearIdleState : PlayerOnGround
{
    public SpearIdleState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void NetworkUpdate()
    {
        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private AnimationPlayable GetNextState()
    {
        var animationState = GetAnimationState();

        if (animationState != null)
            return animationState;

        return GetWeaponState();
    }

    private AnimationPlayable GetAnimationState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (playerMovement.IsJumping)
            return playerPlayables.lowerBodyMovement.JumpPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.lowerBodyMovement.SwordBlockPlayable;

        // if (playerPlayables.healthV2.IsHit)
        //     return playerPlayables.lowerBodyMovement.HitPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.lowerBodyMovement.StaggerHitPlayable;

        if (playerMovement.IsTrapping)
            return playerPlayables.lowerBodyMovement.TrappingPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.lowerBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.lowerBodyMovement.RepairPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        return null;
    }

    private AnimationPlayable GetWeaponState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            return playerPlayables.lowerBodyMovement.IdlePlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                return playerPlayables.lowerBodyMovement.SwordIdlePlayable;

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SpearSprintPlayable;

                return playerPlayables.lowerBodyMovement.SpearRunPlayable;
            }

            return playerPlayables.lowerBodyMovement.SpearIdlePlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                return playerPlayables.lowerBodyMovement.RifleIdlePlayable;

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                return playerPlayables.lowerBodyMovement.BowIdlePlayable;
        }

        return null;
    }
}
