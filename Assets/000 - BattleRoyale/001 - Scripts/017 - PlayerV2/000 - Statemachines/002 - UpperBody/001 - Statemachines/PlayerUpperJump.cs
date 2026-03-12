using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperJump : UpperNoAimState
{
    public PlayerUpperJump(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();


        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetNextUpperJumpState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }

        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetNextUpperJumpState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }
    }

    private UpperBodyAnimations GetNextUpperJumpState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var health = playerPlayables.healthV2;

        if (health.IsDead)
            return upper.DeathPlayable;

        if (playerMovement.Attacking)
        {
            var jumpAttackState = GetUpperJumpAttackState();
            if (jumpAttackState != null)
                return jumpAttackState;
        }

        if (!characterController.IsGrounded)
        {
            if (!playerMovement.IsJumping)
                return GetUpperFallingState();

            return this;
        }

        return GetGroundedUpperLocomotionState();
    }

    private UpperBodyAnimations GetUpperJumpAttackState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        switch (inventory.WeaponIndex)
        {
            case 1:
                return upper.JumpPunchPlayable;

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (primaryId == "001")
                        return upper.SwordJumpAttackPlayable;

                    if (primaryId == "002")
                        return upper.SpearJumpAttackPlayable;

                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (secondaryId == "003")
                        return upper.RifleShootPlayable;

                    // Add bow jump attack later if you have one
                    break;
                }
        }

        return null;
    }

    private UpperBodyAnimations GetUpperFallingState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        if (inventory.WeaponIndex == 3)
        {
            string secondaryId = inventory.SecondaryWeaponID();

            if (secondaryId == "003")
                return upper.RifleFallingPlayable;
        }

        return upper.FallingPlayables;
    }

    private UpperBodyAnimations GetGroundedUpperLocomotionState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (!isMoving)
                        return upper.IdlePlayables;

                    if (canSprint)
                        return upper.SprintPlayables;

                    return upper.RunPlayables;
                }

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (!isMoving)
                    {
                        if (primaryId == "001") return upper.SwordIdlePlayable;
                        if (primaryId == "002") return upper.SpearIdle;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (primaryId == "001") return upper.SwordSprint;
                            if (primaryId == "002") return upper.SpearSprintPlayable;
                        }

                        if (primaryId == "001") return upper.SwordRunPlayable;
                        if (primaryId == "002") return upper.SpearRunPlayable;
                    }

                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (!isMoving)
                    {
                        if (secondaryId == "003") return upper.RifleIdle;
                        if (secondaryId == "004") return upper.BowIdlePlayable;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (secondaryId == "003") return upper.RifleSprintPlayable;
                            if (secondaryId == "004") return upper.BowSprintPlayable;
                        }

                        if (secondaryId == "003") return upper.RifleRunPlayable;
                        if (secondaryId == "004") return upper.BowRunPlayable;
                    }

                    break;
                }
        }

        return upper.IdlePlayables;
    }
}
