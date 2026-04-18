using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static Unity.Collections.Unicode;

public class JumpState : PlayerOnGround
{
    public JumpState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.PlayJumpSoundEffect();

        if (playerPlayables.HasStateAuthority) playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.JumpStartTick = playerPlayables.Runner.Tick;
        playerMovement.Jumping = true;
    }

    public override void NetworkUpdate()
    {
        UpdateJumpFlags();
        playerMovement.Falling();
        playerMovement.MoveCharacter();

        var nextState = GetNextLowerJumpState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private void UpdateJumpFlags()
    {
        int elapsedTicks = playerPlayables.Runner.Tick - (playerPlayables.HasStateAuthority ? playerMovement.JumpStartTick : playerMovement.AnimationTick);

        int totalJumpTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int jumpStopTick = Mathf.CeilToInt(totalJumpTicks * 0.25f);

        if (elapsedTicks >= jumpStopTick)
            playerMovement.Jumping = false;
        else
            playerMovement.Jumping = true;
    }

    private AnimationPlayable GetNextLowerJumpState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var health = playerPlayables.healthV2;

        int elapsedTicks = playerPlayables.Runner.Tick - (playerPlayables.HasStateAuthority ? playerMovement.JumpStartTick : playerMovement.AnimationTick);

        int totalJumpTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int jumpStopTick = Mathf.CeilToInt(totalJumpTicks * 1f);

        if (health.IsDead)
            return lower.DeathPlayable;

        if (playerMovement.Attacking)
        {
            var jumpAttackState = GetJumpAttackState();
            if (jumpAttackState != null)
                return jumpAttackState;
        }

        if (totalJumpTicks < jumpStopTick) return null;

        if (!characterController.IsGrounded)
        {
            return lower.FallingPlayable;
        }

        return GetGroundedLocomotionState();
    }

    private AnimationPlayable GetJumpAttackState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        switch (inventory.WeaponIndex)
        {
            case 1:
                return lower.JumpPunchPlayable;

            case 2:
            {
                string primaryId = inventory.PrimaryWeaponID();

                if (primaryId == "001")
                    return lower.SwordJumpAttackPlayable;

                if (primaryId == "002")
                    return lower.SpearJumpAttackPlayable;

                break;
            }
        }

        return null;
    }

    private AnimationPlayable GetGroundedLocomotionState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (!isMoving)
                        return lower.IdlePlayable;

                    if (canSprint)
                        return lower.SprintPlayable;

                    return lower.RunPlayable;
                }

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (!isMoving)
                    {
                        if (primaryId == "001") return lower.SwordIdlePlayable;
                        if (primaryId == "002") return lower.SpearIdlePlayable;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (primaryId == "001") return lower.SwordSprintPlayable;
                            if (primaryId == "002") return lower.SpearSprintPlayable;
                        }

                        if (primaryId == "001") return lower.SwordRunPlayable;
                        if (primaryId == "002") return lower.SpearRunPlayable;
                    }

                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (!isMoving)
                    {
                        if (secondaryId == "003") return lower.RifleIdlePlayable;
                        if (secondaryId == "004") return lower.BowIdlePlayable;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (secondaryId == "003") return lower.RifleSprintPlayable;
                            if (secondaryId == "004") return lower.BowSprintPlayable;
                        }

                        if (secondaryId == "003") return lower.RifleRunPlayable;
                        if (secondaryId == "004") return lower.BowRunPlayable;
                    }

                    break;
                }
        }

        return lower.IdlePlayable;
    }
}
