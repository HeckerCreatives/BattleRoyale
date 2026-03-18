using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperSprint : UpperNoAimState
{
    public PlayerUpperSprint(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        var nextState = GetNextUpperSprintState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextUpperSprintState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var health = playerPlayables.healthV2;

        // Priority states first
        if (health.IsDead)
            return upper.DeathPlayable;

        if (health.IsStagger)
            return upper.StaggerHitPlayable;

        if (playerMovement.IsJumping)
            return upper.JumpPlayable;

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        if (playerMovement.IsBlocking)
            return upper.BlockPlayable;

        if (playerMovement.IsHealing)
            return upper.HealPlayable;

        if (playerMovement.IsRepairing)
            return upper.RepairPlayable;

        if (playerMovement.IsTrapping)
            return upper.TrapPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return upper.RollPlayables;

        if (playerMovement.Attacking)
            return upper.FirstPunch;

        return GetUpperSprintLocomotionState();
    }

    private UpperBodyAnimations GetUpperSprintLocomotionState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina > 0f;

        if (!isMoving)
            return upper.IdlePlayables;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (canSprint)
                        return upper.SprintPlayables;

                    return upper.RunPlayables;
                }

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (canSprint)
                    {
                        if (primaryId == "001")
                            return upper.SwordSprint;

                        if (primaryId == "002")
                            return upper.SpearSprintPlayable;
                    }

                    if (primaryId == "001")
                        return upper.SwordRunPlayable;

                    if (primaryId == "002")
                        return upper.SpearRunPlayable;

                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (canSprint)
                    {
                        if (secondaryId == "003")
                            return upper.RifleSprintPlayable;

                        if (secondaryId == "004")
                            return upper.BowSprintPlayable;
                    }

                    if (secondaryId == "003")
                        return upper.RifleRunPlayable;

                    if (secondaryId == "004")
                        return upper.BowRunPlayable;

                    break;
                }
        }

        return upper.RunPlayables;
    }
}
