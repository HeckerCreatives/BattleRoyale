using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class RifleRunState : PlayerOnGround
{
    bool playedStep1;
    bool playedStep2;
    double lastNormalizedTime;

    public RifleRunState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasStateAuthority) return;

        playedStep1 = false;
        playedStep2 = false;

        lastNormalizedTime = animationLength > 0f
            ? (animationClipPlayable.GetTime() / animationLength) % 1.0
            : 0.0;

        if (lastNormalizedTime >= 0.35) playedStep1 = true;
        if (lastNormalizedTime >= 0.85) playedStep2 = true;
    }

    public override void Exit()
    {
        base.Exit();

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

        var nextState = GetNextLowerRunState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
            playerPlayables.stamina.RecoverStamina(5f);
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

        if (!playedStep1 && normalizedTime >= 0.35)
        {
            playerPlayables.PlayFootstepSound();
            playedStep1 = true;
        }

        if (!playedStep2 && normalizedTime >= 0.85)
        {
            playerPlayables.PlayFootstepSound();
            playedStep2 = true;
        }

        lastNormalizedTime = normalizedTime;
    }

    private AnimationPlayable GetNextLowerRunState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var health = playerPlayables.healthV2;

        if (health.IsDead)
            return lower.DeathPlayable;

        if (health.IsStagger)
            return lower.StaggerHitPlayable;

        if (playerMovement.IsJumping && playerPlayables.stamina.Stamina >= 20f)
            return lower.JumpPlayable;

        if (!characterController.IsGrounded)
            return lower.FallingPlayable;

        if (playerMovement.IsBlocking)
            return lower.BlockPlayable;

        //if (playerMovement.Attacking && playerPlayables.inventory.BowAmmo() > 0)
        //    return lower.BowShootingMovePlayable;

        if (playerMovement.IsHealing)
            return lower.HealPlayable;

        if (playerMovement.IsRepairing)
            return lower.RepairPlayable;

        if (playerMovement.IsTrapping)
            return lower.TrappingPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return lower.RollPlayable;

        return GetWeaponBasedRunState();
    }

    private AnimationPlayable GetWeaponBasedRunState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool isSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    return lower.RunPlayable;
                }

            case 2:
                {
                    if (!isMoving)
                    {
                        if (inventory.PrimaryWeaponID() == "001")
                            return lower.SwordIdlePlayable;

                        if (inventory.PrimaryWeaponID() == "002")
                            return lower.SpearIdlePlayable;
                    }
                    else
                    {
                        if (inventory.PrimaryWeaponID() == "001")
                            return lower.SwordRunPlayable;

                        if (inventory.PrimaryWeaponID() == "002")
                            return lower.SpearRunPlayable;
                    }

                    break;
                }

            case 3:
                {
                    if (inventory.SecondaryWeaponID() == "003")
                    {
                        if (IsUpperBodyAiming())
                            return isMoving
                                ? (AnimationPlayable)lower.RifleAimMovePlayable
                                : lower.RifleAimIdlePlayable;

                        if (!isMoving)
                            return lower.RifleIdlePlayable;

                        if (isSprint)
                            return lower.RifleSprintPlayable;

                        return null;
                    }

                    if (inventory.SecondaryWeaponID() == "004")
                    {
                        if (!isMoving)
                            return lower.BowIdlePlayable;

                        return lower.BowRunPlayable;
                    }

                    break;
                }
        }

        return lower.IdlePlayable;
    }
}
