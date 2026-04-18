using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperIdle : UpperNoAimState
{
    public PlayerUpperIdle(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();


        var nextState = GetNextUpperBodyState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextUpperBodyState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var health = playerPlayables.healthV2;

        // Highest priority first
        if (health.IsDead)
            return upper.DeathPlayable;

        if (playerMovement.IsJumping)
            return upper.JumpPlayable;

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        if (playerMovement.IsBlocking)
            return upper.BlockPlayable;

        if (playerMovement.IsHealing)
            return upper.HealPlayable;

        if (playerMovement.IsRepairing)
            return upper.RepairPlayable;

        if (playerMovement.IsTrapping)
            return upper.TrapPlayable;

        if (playerMovement.Attacking)
            return upper.FirstPunch;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return upper.RollPlayables;

        return GetUpperWeaponState();
    }

    private UpperBodyAnimations GetUpperWeaponState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                if (isMoving)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return upper.SprintPlayables;

                    return upper.RunPlayables;
                }
                return this;

            case 2:
                switch (inventory.PrimaryWeaponID())
                {
                    case "001": return upper.SwordIdlePlayable;
                    case "002": return upper.SpearIdle;
                }
                break;

            case 3:
                switch (inventory.SecondaryWeaponID())
                {
                    case "003": return upper.RifleIdle;
                    case "004": return upper.BowIdlePlayable;
                }
                break;
        }

        return this;
    }
}
