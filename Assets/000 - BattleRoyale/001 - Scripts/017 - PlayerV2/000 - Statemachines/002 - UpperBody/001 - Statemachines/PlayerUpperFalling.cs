using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperFalling : UpperNoAimState
{
    public PlayerUpperFalling(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();

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

    private UpperBodyAnimations GetNextState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (!characterController.IsGrounded)
            return GetAirborneState();

        return GetGroundedState();
    }

    private UpperBodyAnimations GetAirborneState()
    {
        if (playerMovement.Attacking)
        {
            switch (playerPlayables.inventory.WeaponIndex)
            {
                case 1:
                    return playerPlayables.upperBodyMovement.JumpPunchPlayable;

                case 2:
                    switch (playerPlayables.inventory.PrimaryWeaponID())
                    {
                        case "001":
                            return playerPlayables.upperBodyMovement.SwordJumpAttackPlayable;

                        case "002":
                            return playerPlayables.upperBodyMovement.SpearJumpAttackPlayable;
                    }
                    break;
            }
        }

        // Default falling state while still airborne
        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            switch (playerPlayables.inventory.SecondaryWeaponID())
            {
                case "003":
                    return playerPlayables.upperBodyMovement.RifleFallingPlayable;

                case "004":
                    return playerPlayables.upperBodyMovement.BowFallingPlayable;
            }
        }

        // Keep current falling playable for unarmed / melee if this state already is the generic falling state
        return null;
    }

    private UpperBodyAnimations GetGroundedState()
    {
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (playerPlayables.inventory.WeaponIndex)
        {
            case 1:
                if (!isMoving)
                    return playerPlayables.upperBodyMovement.IdlePlayables;

                return canSprint
                    ? playerPlayables.upperBodyMovement.SprintPlayables
                    : playerPlayables.upperBodyMovement.RunPlayables;

            case 2:
                return GetPrimaryGroundedState(isMoving, canSprint);

            case 3:
                return GetSecondaryGroundedState(isMoving, canSprint);
        }

        return null;
    }

    private UpperBodyAnimations GetPrimaryGroundedState(bool isMoving, bool canSprint)
    {
        switch (playerPlayables.inventory.PrimaryWeaponID())
        {
            case "001":
                if (!isMoving)
                    return playerPlayables.upperBodyMovement.SwordIdlePlayable;

                return canSprint
                    ? playerPlayables.upperBodyMovement.SwordSprint
                    : playerPlayables.upperBodyMovement.SwordRunPlayable;

            case "002":
                if (!isMoving)
                    return playerPlayables.upperBodyMovement.SpearIdle;

                return canSprint
                    ? playerPlayables.upperBodyMovement.SpearSprintPlayable
                    : playerPlayables.upperBodyMovement.SpearRunPlayable;
        }

        return null;
    }

    private UpperBodyAnimations GetSecondaryGroundedState(bool isMoving, bool canSprint)
    {
        switch (playerPlayables.inventory.SecondaryWeaponID())
        {
            case "003":
                if (!isMoving)
                    return playerPlayables.upperBodyMovement.RifleIdle;

                return canSprint
                    ? playerPlayables.upperBodyMovement.RifleSprintPlayable
                    : playerPlayables.upperBodyMovement.RifleRunPlayable;

            case "004":
                if (!isMoving)
                    return playerPlayables.upperBodyMovement.BowIdlePlayable;

                return canSprint
                    ? playerPlayables.upperBodyMovement.BowSprintPlayable
                    : playerPlayables.upperBodyMovement.BowRunPlayable;
        }

        return null;
    }
}
