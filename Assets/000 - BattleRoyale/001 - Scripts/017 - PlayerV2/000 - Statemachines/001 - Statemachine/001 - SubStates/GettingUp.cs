using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class GettingUp : PlayerOnGround
{
    public GettingUp(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority) playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.GettingUpStartTick = playerPlayables.Runner.Tick;

        playerPlayables.healthV2.IsGettingUp = true;
        playerPlayables.healthV2.IsStagger = false;
        playerMovement.CannotJump = true;

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

        playerPlayables.healthV2.IsStagger = false;
        playerPlayables.healthV2.IsGettingUp = false;
        playerMovement.CannotJump = false;
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
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.GettingUpStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (elapsedTicks < finishStartTick)
            return null;

        if (playerMovement.IsJumping)
            return playerPlayables.lowerBodyMovement.JumpPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        return GetWeaponGroundedState();
    }

    private AnimationPlayable GetWeaponGroundedState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.IsBlocking)
                return playerPlayables.lowerBodyMovement.BlockPlayable;

            if (playerMovement.Attacking)
                return playerPlayables.lowerBodyMovement.Punch1Playable;

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                return playerPlayables.lowerBodyMovement.IdlePlayable;

            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                return playerPlayables.lowerBodyMovement.SprintPlayable;

            return playerPlayables.lowerBodyMovement.RunPlayable;
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.IsBlocking)
                    return playerPlayables.lowerBodyMovement.SwordBlockPlayable;

                if (playerMovement.Attacking)
                    return playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable;

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.SwordIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SwordSprintPlayable;

                return playerPlayables.lowerBodyMovement.SwordRunPlayable;
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.IsBlocking)
                    return playerPlayables.lowerBodyMovement.SpearBlockPlayable;

                if (playerMovement.Attacking)
                    return playerPlayables.lowerBodyMovement.SpearFirstAttackPlayable;

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.SpearIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SpearSprintPlayable;

                return playerPlayables.lowerBodyMovement.SpearRunPlayable;
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.RifleIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.RifleSprintPlayable;

                return playerPlayables.lowerBodyMovement.RifleRunPlayable;
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
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
