using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerHealPlayable : UpperNoAimState
{
    public PlayerHealPlayable(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority) return;

        SetWeaponEquipped(false);
    }

    public override void Exit()
    {
        base.Exit();

        if (!playerPlayables.HasStateAuthority) return;

        SetWeaponEquipped(true);
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        double animTime = animationClipPlayable.GetTime();

        bool beforeHeal = animTime < animationLength * 0.5f;
        bool finished = animTime >= animationLength - 0.02f;

        // Same behavior as your original:
        // run checks before heal point, and again when full timer is finished
        if (beforeHeal || finished)
        {
            if (playerPlayables.HasInputAuthority)
            {
                var predictedState = GetNextState();

                if (predictedState != null && playablesChanger.CurrentState != predictedState)
                {
                    playablesChanger.ChangeState(predictedState);
                }
            }
            if (playerPlayables.HasStateAuthority)
            {
                var nextState = GetNextState();

                if (nextState != null && playablesChanger.CurrentState != nextState)
                {
                    playablesChanger.ChangeState(nextState);
                }
            }

        }
    }

    private void SetWeaponEquipped(bool equipped)
    {
        if (playerPlayables.inventory.WeaponIndex == 2 && playerPlayables.inventory.PrimaryWeapon != null)
        {
            playerPlayables.inventory.PrimaryWeapon.IsEquipped = equipped;
        }
        else if (playerPlayables.inventory.WeaponIndex == 3 && playerPlayables.inventory.SecondaryWeapon != null)
        {
            playerPlayables.inventory.SecondaryWeapon.IsEquipped = equipped;
        }
    }

    private UpperBodyAnimations GetNextState()
    {
        // Higher-priority interrupts first
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        return GetWeaponState();
    }

    private UpperBodyAnimations GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.upperBodyMovement.StaggerHitPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        return null;
    }

    private UpperBodyAnimations GetWeaponState()
    {
        int weaponIndex = playerPlayables.inventory.WeaponIndex;
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool isIdle = !isMoving;
        bool isSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;
        bool finishedHeal = animationClipPlayable.GetTime() >= animationLength;
        bool isGrounded = characterController.IsGrounded;

        switch (weaponIndex)
        {
            case 1:
                return GetUnarmedState(isMoving, isIdle, isSprint, finishedHeal, isGrounded);

            case 2:
                return GetPrimaryWeaponState(isMoving, isIdle, isSprint, finishedHeal, isGrounded);

            case 3:
                return GetSecondaryWeaponState(isMoving, isIdle, isSprint, finishedHeal, isGrounded);
        }

        return null;
    }

    private UpperBodyAnimations GetUnarmedState(bool isMoving, bool isIdle, bool isSprint, bool finishedHeal, bool isGrounded)
    {
        if (isMoving)
            return isSprint
                ? playerPlayables.upperBodyMovement.SprintPlayables
                : playerPlayables.upperBodyMovement.RunPlayables;

        if (playerMovement.Attacking)
            return playerPlayables.upperBodyMovement.FirstPunch;

        if (playerMovement.IsBlocking)
            return playerPlayables.upperBodyMovement.BlockPlayable;

        if (!isGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (isIdle && finishedHeal)
            return playerPlayables.upperBodyMovement.IdlePlayables;

        return null;
    }

    private UpperBodyAnimations GetPrimaryWeaponState(bool isMoving, bool isIdle, bool isSprint, bool finishedHeal, bool isGrounded)
    {
        string weaponId = playerPlayables.inventory.PrimaryWeaponID();

        if (weaponId == "001")
        {
            if (isMoving)
                return isSprint
                    ? playerPlayables.upperBodyMovement.SwordSprint
                    : playerPlayables.upperBodyMovement.SwordRunPlayable;

            if (playerMovement.Attacking)
                return playerPlayables.upperBodyMovement.SwordAttackFirstPlayable;

            if (playerMovement.IsBlocking)
                return playerPlayables.upperBodyMovement.SwordBlockPlayable;

            if (!isGrounded)
                return playerPlayables.upperBodyMovement.FallingPlayables;

            if (playerMovement.IsJumping)
                return playerPlayables.upperBodyMovement.JumpPlayable;

            if (isIdle && finishedHeal)
                return playerPlayables.upperBodyMovement.SwordIdlePlayable;
        }
        else if (weaponId == "002")
        {
            if (isMoving)
                return isSprint
                    ? playerPlayables.upperBodyMovement.SpearSprintPlayable
                    : playerPlayables.upperBodyMovement.SpearRunPlayable;

            if (playerMovement.Attacking)
                return playerPlayables.upperBodyMovement.SpearFirstAttackPlayable;

            if (playerMovement.IsBlocking)
                return playerPlayables.upperBodyMovement.SpearBlockPlayable;

            if (!isGrounded)
                return playerPlayables.upperBodyMovement.FallingPlayables;

            if (playerMovement.IsJumping)
                return playerPlayables.upperBodyMovement.JumpPlayable;

            if (isIdle && finishedHeal)
                return playerPlayables.upperBodyMovement.SpearIdle;
        }

        return null;
    }

    private UpperBodyAnimations GetSecondaryWeaponState(bool isMoving, bool isIdle, bool isSprint, bool finishedHeal, bool isGrounded)
    {
        string weaponId = playerPlayables.inventory.SecondaryWeaponID();

        if (weaponId == "003")
        {
            if (isMoving)
                return isSprint
                    ? playerPlayables.upperBodyMovement.RifleSprintPlayable
                    : playerPlayables.upperBodyMovement.RifleRunPlayable;

            if (!isGrounded)
                return playerPlayables.upperBodyMovement.RifleFallingPlayable;

            if (playerMovement.IsJumping)
                return playerPlayables.upperBodyMovement.RifleJumpPlayable;

            if (isIdle && finishedHeal)
                return playerPlayables.upperBodyMovement.RifleIdle;
        }
        else if (weaponId == "004")
        {
            if (isMoving)
                return isSprint
                    ? playerPlayables.upperBodyMovement.BowSprintPlayable
                    : playerPlayables.upperBodyMovement.BowRunPlayable;

            if (!isGrounded)
                return playerPlayables.upperBodyMovement.BowFallingPlayable;

            if (playerMovement.IsJumping)
                return playerPlayables.upperBodyMovement.BowJumpPlayable;

            if (isIdle && finishedHeal)
                return playerPlayables.upperBodyMovement.BowIdlePlayable;
        }

        return null;
    }
}
