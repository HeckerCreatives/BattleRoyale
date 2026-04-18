using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class HitState : PlayerOnGround
{
    public HitState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority) playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.PunchStartTick = playerPlayables.Runner.Tick;
        playerMovement.IsHit = true;

        playerMovement.Swording = false;
        playerMovement.Punching = false;
        playerMovement.SwordingMove = false;
        playerMovement.PunchingMove = false;
        playerMovement.WasPunchingMoveLastTick = false;
        playerMovement.WasRollingMoveLastTick = false;
        playerMovement.WasSwordingMoveLastTick = false;
    }

    public override void Exit()
    {
        base.Exit();

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.IsHit = false;
    }

    public override void NetworkUpdate()
    {
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
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;


        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        return null;
    }

    private AnimationPlayable GetRecoveryState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.PunchStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);
        int rollStartTick = Mathf.CeilToInt(totalPunchTicks * 0.1f);

        bool finishedPunch = elapsedTicks >= finishStartTick;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f && elapsedTicks >= rollStartTick;

        if (canRoll)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (!finishedPunch)
            return null;

        return GetGroundedState();
    }

    private AnimationPlayable GetGroundedState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                return playerPlayables.lowerBodyMovement.IdlePlayable;

            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                return playerPlayables.lowerBodyMovement.SprintPlayable;

            return playerPlayables.lowerBodyMovement.RunPlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.SwordIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SwordSprintPlayable;

                return playerPlayables.lowerBodyMovement.SwordRunPlayable;
            }

            if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.SpearIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SpearSprintPlayable;

                return playerPlayables.lowerBodyMovement.SpearRunPlayable;
            }
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.RifleIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.RifleSprintPlayable;

                return playerPlayables.lowerBodyMovement.RifleRunPlayable;
            }

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.BowIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.BowSprintPlayable;

                return playerPlayables.lowerBodyMovement.BowRunPlayable;
            }
        }

        return null;
    }
}
