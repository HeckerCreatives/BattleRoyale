using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSpearSprint : UpperNoAimState
{
    public PlayerUpperSpearSprint(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextState()
    {
        var actionState = GetActionState();

        if (actionState != null)
            return actionState;

        return GetSprintMovementState();
    }

    private UpperBodyAnimations GetActionState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsTrapping)
            return playerPlayables.upperBodyMovement.TrapPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.upperBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.upperBodyMovement.RepairPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.upperBodyMovement.SwordBlockPlayable;

        // if (playerPlayables.healthV2.IsHitUpper)
        //     return playerPlayables.upperBodyMovement.HitPlayable;

        return null;
    }

    private UpperBodyAnimations GetSprintMovementState()
    {
        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool hasSprintStamina = playerPlayables.stamina.Stamina > 0f;
        bool wantsSprint = playerMovement.IsSprint;

        if (!isMoving)
            return playerPlayables.upperBodyMovement.IdlePlayables;

        if (!wantsSprint || !hasSprintStamina)
            return playerPlayables.upperBodyMovement.SpearRunPlayable;

        if (playerPlayables.inventory.WeaponIndex == 1)
            return playerPlayables.upperBodyMovement.SprintPlayables;

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "001")
                return playerPlayables.upperBodyMovement.SwordSprint;

            return null;
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                return playerPlayables.upperBodyMovement.RifleSprintPlayable;

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                return playerPlayables.upperBodyMovement.BowSprintPlayable;
        }

        return null;
    }
}
