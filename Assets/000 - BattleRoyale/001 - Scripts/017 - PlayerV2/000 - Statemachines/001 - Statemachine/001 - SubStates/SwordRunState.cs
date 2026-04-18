using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SwordRunState : PlayerOnGround
{
    bool playedStep1;
    bool playedStep2;
    double lastNormalizedTime;

    public SwordRunState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasStateAuthority) return;

        playedStep1 = false;
        playedStep2 = false;
        lastNormalizedTime = 0.0;
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
        base.NetworkUpdate();

        playerMovement.MoveCharacter();

        HandleFootsteps();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

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

    private AnimationPlayable GetNextState()
    {
        var actionState = GetActionState();

        if (actionState != null)
            return actionState;

        return GetWeaponMovementState();
    }

    private AnimationPlayable GetActionState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;


        if (playerMovement.IsJumping && playerPlayables.stamina.Stamina >= 20f)
            return playerPlayables.lowerBodyMovement.JumpPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (playerMovement.IsTrapping)
            return playerPlayables.lowerBodyMovement.TrappingPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.lowerBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.lowerBodyMovement.RepairPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.lowerBodyMovement.SwordBlockPlayable;

        if (playerMovement.Attacking)
            return playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable;

        // if (playerPlayables.healthV2.IsHit)
        //     return playerPlayables.lowerBodyMovement.HitPlayable;

        return null;
    }

    private AnimationPlayable GetWeaponMovementState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
            return playerPlayables.lowerBodyMovement.RunPlayable;

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "002")
                return playerPlayables.lowerBodyMovement.SpearRunPlayable;

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                return playerPlayables.lowerBodyMovement.SwordIdlePlayable;

            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                return playerPlayables.lowerBodyMovement.SwordSprintPlayable;

            return playerPlayables.lowerBodyMovement.SwordRunPlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                return playerPlayables.lowerBodyMovement.RifleRunPlayable;

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                return playerPlayables.lowerBodyMovement.BowRunPlayable;
        }

        return null;
    }
}
