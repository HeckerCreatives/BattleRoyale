using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperStopSprint : UpperNoAimState
{
    public PlayerUpperStopSprint(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
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
        var lower = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;

        if (playerPlayables.healthV2.IsDead)
            return lower.DeathPlayable;

        if (!characterController.IsGrounded)
            return lower.FallingPlayables;

        if (canRoll)
            return lower.RollPlayables;

        if (isMoving)
        {
            switch (inventory.WeaponIndex)
            {
                case 1:
                    return lower.RunPlayables;
                case 2:
                    string primaryId = inventory.PrimaryWeaponID();

                    if (primaryId == "001") return lower.SwordRunPlayable;
                    if (primaryId == "002") return lower.SwordIdlePlayable;

                    break;

                case 3:
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (secondaryId == "003") return lower.RifleRunPlayable;
                    if (secondaryId == "004") return lower.BowRunPlayable;

                    break;
            }
        }

        return null;
    }

    private UpperBodyAnimations GetRecoveryState()
    {
        var lower = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.PunchStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        bool finishedPunch = elapsedTicks >= finishStartTick;

        if (!finishedPunch)
            return null;

        switch (inventory.WeaponIndex)
        {
            case 1:
                return lower.IdlePlayables;
            case 2:
                string primaryId = inventory.PrimaryWeaponID();

                if (primaryId == "001") return lower.SwordIdlePlayable;
                if (primaryId == "002") return lower.SpearIdle;

                break;

            case 3:
                string secondaryId = inventory.SecondaryWeaponID();

                if (secondaryId == "003") return lower.RifleIdle;
                if (secondaryId == "004") return lower.BowIdlePlayable;

                break;
        }

        return lower.IdlePlayables;
    }
}
