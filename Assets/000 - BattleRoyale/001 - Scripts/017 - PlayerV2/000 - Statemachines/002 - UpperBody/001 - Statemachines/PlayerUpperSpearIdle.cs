using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSpearIdle : UpperNoAimState
{
    public PlayerUpperSpearIdle(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();

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

        return GetWeaponState();
    }

    private UpperBodyAnimations GetActionState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsTrapping)
            return playerPlayables.upperBodyMovement.TrapPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.upperBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.upperBodyMovement.RepairPlayable;

        if (playerMovement.Attacking)
            return playerPlayables.upperBodyMovement.SpearFirstAttackPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.upperBodyMovement.SpearBlockPlayable;

        // if (playerPlayables.healthV2.IsHitUpper)
        //     return playerPlayables.upperBodyMovement.HitPlayable;

        return null;
    }

    private UpperBodyAnimations GetWeaponState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
            return playerPlayables.upperBodyMovement.IdlePlayables;

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                return playerPlayables.upperBodyMovement.SpearIdle;

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.upperBodyMovement.SwordSprint;

                return playerPlayables.upperBodyMovement.SwordRunPlayable;
            }

            return playerPlayables.upperBodyMovement.SwordIdlePlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                return playerPlayables.upperBodyMovement.RifleIdle;

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                return playerPlayables.upperBodyMovement.BowIdlePlayable;
        }

        return null;
    }
}
