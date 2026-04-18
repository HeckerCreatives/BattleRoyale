using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class StopSprintState : PlayerOnGround
{
    public StopSprintState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority) playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.StopSprintStartTick = playerPlayables.Runner.Tick;
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private AnimationPlayable GetNextState()
    {
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        return GetRecoveryState();
    }

    private AnimationPlayable GetInterruptState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;

        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (canRoll)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (isMoving)
        {
            switch (inventory.WeaponIndex)
            {
                case 1:
                    return lower.RunPlayable;
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

    private AnimationPlayable GetRecoveryState()
    {
        var lower = playerPlayables.lowerBodyMovement;
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
                    return lower.IdlePlayable;
            case 2:
                string primaryId = inventory.PrimaryWeaponID();

                if (primaryId == "001") return lower.SwordIdlePlayable;
                if (primaryId == "002") return lower.SpearIdlePlayable;

                break;

            case 3:
                string secondaryId = inventory.SecondaryWeaponID();

                if (secondaryId == "003") return lower.RifleIdlePlayable;
                if (secondaryId == "004") return lower.BowIdlePlayable;

                break;
        }

        return lower.IdlePlayable;
    }
}
