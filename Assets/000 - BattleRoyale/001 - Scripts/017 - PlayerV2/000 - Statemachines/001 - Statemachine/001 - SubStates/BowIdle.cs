using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BowIdle : PlayerOnGround
{
    public BowIdle(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        var nextState = GetNextLowerBodyState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
            playerPlayables.stamina.RecoverStamina(5f);
    }

    private AnimationPlayable GetNextLowerBodyState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (playerMovement.IsJumping)
            return playerPlayables.lowerBodyMovement.JumpPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.lowerBodyMovement.BlockPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.lowerBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.lowerBodyMovement.RepairPlayable;

        if (playerMovement.IsTrapping)
            return playerPlayables.lowerBodyMovement.TrappingPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (playerMovement.Attacking)
            return playerPlayables.lowerBodyMovement.BowDrawIdlePlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        return GetWeaponBasedIdleOrMoveState();
    }

    private AnimationPlayable GetWeaponBasedIdleOrMoveState()
    {
        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;

        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            return playerPlayables.lowerBodyMovement.IdlePlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "001")
                return playerPlayables.lowerBodyMovement.SwordIdlePlayable;

            if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "002")
                return playerPlayables.lowerBodyMovement.SpearIdlePlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeapon.WeaponID == "003")
                return playerPlayables.lowerBodyMovement.RifleIdlePlayable;

            if (playerPlayables.inventory.SecondaryWeapon.WeaponID == "004")
            {
                if (isMoving)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return playerPlayables.lowerBodyMovement.BowSprintPlayable;

                    return playerPlayables.lowerBodyMovement.BowRunPlayable;
                }

                return null;
            }
        }

        return null;
    }
}
