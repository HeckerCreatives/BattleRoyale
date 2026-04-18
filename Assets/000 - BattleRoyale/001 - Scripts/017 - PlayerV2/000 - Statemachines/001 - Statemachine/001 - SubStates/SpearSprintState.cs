using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SpearSprintState : PlayerOnGround
{
    bool playedStep1;
    bool playedStep2;
    double lastNormalizedTime;

    public SpearSprintState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority)
        {
            playerPlayables.ChangeFOV(70f);
            playerPlayables.WarpDrive.Play();
        }

        if (playerPlayables.HasStateAuthority) return;

        playedStep1 = false;
        playedStep2 = false;
        lastNormalizedTime = 0.0;
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.HasInputAuthority)
        {
            playerPlayables.ChangeFOV(60f);
            playerPlayables.WarpDrive.Stop();
        }

        if (playerPlayables.HasStateAuthority) return;

        playedStep1 = false;
        playedStep2 = false;
        lastNormalizedTime = 0.0;
    }

    public override void NetworkLocalUpdate()
    {
        base.NetworkLocalUpdate();

        HandleFootsteps();
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.MoveCharacter();

        HandleFootsteps();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        playerPlayables.stamina.DecreaseStamina(20f);
    }

    private void HandleFootsteps()
    {
        if (playerPlayables.HasStateAuthority) return;

        if (animationLength <= 0f)
            return;

        double animTime = animationClipPlayable.GetTime();
        double normalizedTime = (animTime / animationLength) % 1.0;

        // animation looped back to start
        if (normalizedTime < lastNormalizedTime)
        {
            playedStep1 = false;
            playedStep2 = false;
        }

        if (!playedStep1 && normalizedTime >= 0.20f)
        {
            playerPlayables.PlayFootstepSound();
            playedStep1 = true;
        }

        if (!playedStep2 && normalizedTime >= 0.80f)
        {
            playerPlayables.PlayFootstepSound();
            playedStep2 = true;
        }

        lastNormalizedTime = normalizedTime;
    }

    private AnimationPlayable GetNextState()
    {
        var forcedState = GetForcedState();
        if (forcedState != null)
            return forcedState;

        return GetMovementState();
    }

    private AnimationPlayable GetForcedState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;


        if (playerMovement.IsJumping && playerPlayables.stamina.Stamina >= 20f)
            return playerPlayables.lowerBodyMovement.JumpPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (playerMovement.IsTrapping)
            return playerPlayables.lowerBodyMovement.TrappingPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.lowerBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.lowerBodyMovement.RepairPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.lowerBodyMovement.SwordBlockPlayable;

        // if (playerPlayables.healthV2.IsHit)
        //     return playerPlayables.lowerBodyMovement.HitPlayable;

        return null;
    }

    private AnimationPlayable GetMovementState()
    {
        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool hasSprintStamina = playerPlayables.stamina.Stamina > 0f && playerMovement.IsSprint;

        if (!isMoving)
            return playerPlayables.lowerBodyMovement.IdlePlayable;

        if (!hasSprintStamina && isMoving)
            return playerPlayables.lowerBodyMovement.SpearRunPlayable;

        if (playerPlayables.inventory.WeaponIndex == 1)
            return playerPlayables.lowerBodyMovement.SprintPlayable;

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "001")
                return playerPlayables.lowerBodyMovement.SwordSprintPlayable;

            return null;
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                return playerPlayables.lowerBodyMovement.RifleSprintPlayable;

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                return playerPlayables.lowerBodyMovement.BowSprintPlayable;
        }

        return null;
    }
}
