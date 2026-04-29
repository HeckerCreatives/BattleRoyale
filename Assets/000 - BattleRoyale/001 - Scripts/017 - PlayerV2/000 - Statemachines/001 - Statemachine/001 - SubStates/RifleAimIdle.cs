using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RifleAimIdle : PlayerOnGround
{
    public RifleAimIdle(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.MoveWithAim();
        playerMovement.RotatePlayer();

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

        //if (!playerMovement.Attacking)
        //    return playerPlayables.lowerBodyMovement.BowIdlePlayable; 

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        return GetWeaponBasedIdleOrMoveState();
    }

    private AnimationPlayable GetWeaponBasedIdleOrMoveState()
    {
        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool isAiming = playerMovement.Attacking || IsUpperBodyAiming();

        if (isMoving)
        {
            if (isAiming)
                return playerPlayables.lowerBodyMovement.RifleAimMovePlayable;

            return playerPlayables.lowerBodyMovement.RifleRunPlayable;
        }
        else
        {
            if (isAiming)
                return null;

            return playerPlayables.lowerBodyMovement.RifleIdlePlayable;
        }
    }
}
