using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class SprintState : PlayerOnGround
{
    public SprintState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority)
        {
            playerPlayables.CancelInvoke();

            playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), animationLength * 0.20f, animationLength);
            playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), animationLength * 0.80f, animationLength);
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.HasInputAuthority)
            playerPlayables.CancelInvoke();
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();


        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetNextLowerSprintState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }

        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetNextLowerSprintState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
            playerPlayables.stamina.DecreaseStamina(20f);
        }
    }

    private AnimationPlayable GetNextLowerSprintState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var health = playerPlayables.healthV2;

        // action / override priority first
        if (health.IsDead)
            return lower.DeathPlayable;

        if (health.IsStagger)
            return lower.StaggerHitPlayable;

        if (playerMovement.IsJumping)
            return lower.JumpPlayable;

        if (!characterController.IsGrounded)
            return lower.FallingPlayable;

        if (playerMovement.IsBlocking)
            return lower.BlockPlayable;

        if (playerMovement.Attacking)
            return lower.Punch1Playable;

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
        {
            switch (inventory.WeaponIndex)
            {
                case 2:
                    {
                        string primaryId = inventory.PrimaryWeaponID();
                        if (primaryId == "001") return lower.SwordIdlePlayable;
                        if (primaryId == "002") return lower.SpearIdlePlayable;
                        break;
                    }

                case 3:
                    {
                        string secondaryId = inventory.SecondaryWeaponID();
                        if (secondaryId == "003") return lower.RifleIdlePlayable;
                        if (secondaryId == "004") return lower.BowIdlePlayable;
                        break;
                    }
            }

            return lower.IdlePlayable;
        }

        switch (inventory.WeaponIndex)
        {
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

                    if (canSprint)
                    {
                        if (secondaryId == "003") return lower.RifleSprintPlayable;
                        if (secondaryId == "004") return lower.BowSprintPlayable;
                    }

                    if (secondaryId == "003") return lower.RifleRunPlayable;
                    if (secondaryId == "004") return lower.BowRunPlayable;
                    break;
                }
        }

        if (canSprint)
            return lower.SprintPlayable;

        return lower.RunPlayable;
    }
}
