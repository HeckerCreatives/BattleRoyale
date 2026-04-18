using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperHit : UpperNoAimState
{
    public PlayerUpperHit(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }


    public override void NetworkUpdate()
    {
        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextState()
    {
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        return GetRecoveryState();
    }

    private UpperBodyAnimations GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;


        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        return null;
    }

    private UpperBodyAnimations GetRecoveryState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.PunchStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);
        int rollStartTick = Mathf.CeilToInt(totalPunchTicks * 0.3f);

        bool finishedPunch = elapsedTicks >= finishStartTick;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f && elapsedTicks >= rollStartTick;

        if (canRoll)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (!finishedPunch)
            return null;

        return GetGroundedState();
    }

    private UpperBodyAnimations GetGroundedState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                return playerPlayables.upperBodyMovement.IdlePlayables;

            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                return playerPlayables.upperBodyMovement.SprintPlayables;

            return playerPlayables.upperBodyMovement.RunPlayables;
        }

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.upperBodyMovement.SwordIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.upperBodyMovement.SwordSprint;

                return playerPlayables.upperBodyMovement.SwordRunPlayable;
            }

            if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.upperBodyMovement.SpearIdle;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.upperBodyMovement.SpearSprintPlayable;

                return playerPlayables.upperBodyMovement.SpearRunPlayable;
            }
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.upperBodyMovement.RifleIdle;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.upperBodyMovement.RifleSprintPlayable;

                return playerPlayables.upperBodyMovement.RifleRunPlayable;
            }

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.upperBodyMovement.BowIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.upperBodyMovement.BowSprintPlayable;

                return playerPlayables.upperBodyMovement.BowRunPlayable;
            }
        }

        return null;
    }
}
