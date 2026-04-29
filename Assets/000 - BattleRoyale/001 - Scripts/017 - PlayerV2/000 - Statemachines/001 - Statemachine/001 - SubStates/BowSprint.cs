using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BowSprint : PlayerOnGround
{
    bool playedStep1;
    bool playedStep2;
    double lastNormalizedTime;

    public BowSprint(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
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
        playerMovement.MoveCharacter();

        var nextState = GetNextLowerSprintState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
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

    private AnimationPlayable GetNextLowerSprintState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var health = playerPlayables.healthV2;

        // action / override priority first
        if (health.IsDead)
            return lower.DeathPlayable;

        if (playerMovement.IsJumping && playerPlayables.stamina.Stamina >= 20f)
            return lower.JumpPlayable;

        if (!characterController.IsGrounded)
            return lower.FallingPlayable;

        if (playerMovement.IsBlocking)
            return lower.BlockPlayable;

        if (playerMovement.IsHealing)
            return lower.HealPlayable;

        if (playerMovement.IsRepairing)
            return lower.RepairPlayable;

        if (playerMovement.IsTrapping)
            return lower.TrappingPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
            return lower.RollPlayable;

        return GetLowerSprintLocomotionState();
    }

    private AnimationPlayable GetLowerSprintLocomotionState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina > 0f;

        if (!isMoving)
            return lower.BowIdlePlayable;

        switch (inventory.WeaponIndex)
        {
            case 1:
                return lower.SprintPlayable;

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (canSprint)
                    {
                        if (primaryId == "001") return lower.SwordSprintPlayable;
                        if (primaryId == "002") return lower.SpearSprintPlayable;
                    }

                    if (primaryId == "001") return lower.SwordRunPlayable;
                    if (primaryId == "002") return lower.SpearRunPlayable;
                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (!canSprint && isMoving)
                    {
                        if (secondaryId == "004") return lower.BowRunPlayable;
                    }

                    if (canSprint)
                        if (secondaryId == "003") return lower.RifleSprintPlayable;

                    break;
                }
        }

        return null;
    }
}
