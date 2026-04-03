using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperGettingUp : UpperNoAimState
{

    public PlayerUpperGettingUp(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextState()
    {
        var upper = playerPlayables.upperBodyMovement;

        if (playerPlayables.healthV2.IsDead)
            return upper.DeathPlayable;

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        if (animationClipPlayable.GetTime() < animationLength * 0.9f)
            return null;

        if (playerMovement.IsJumping)
            return upper.JumpPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return upper.StaggerHitPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return upper.RollPlayables;

        return GetWeaponIdleState();
    }

    private UpperBodyAnimations GetWeaponIdleState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        bool moving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (playerMovement.IsBlocking)
                        return upper.BlockPlayable;

                    if (playerMovement.Attacking)
                        return upper.FirstPunch;

                    if (!moving)
                        return upper.IdlePlayables;

                    return canSprint ? upper.SprintPlayables : upper.RunPlayables;
                }

            case 2:
                {
                    string id = inventory.PrimaryWeaponID();

                    if (playerMovement.IsBlocking)
                        return id == "001" ? upper.SwordBlockPlayable : upper.SpearBlockPlayable;

                    if (playerMovement.Attacking)
                        return id == "001"
                            ? upper.SwordAttackFirstPlayable
                            : upper.SpearFirstAttackPlayable;

                    if (!moving)
                        return id == "001"
                            ? upper.SwordIdlePlayable
                            : upper.SpearIdle;

                    if (canSprint)
                        return id == "001"
                            ? upper.SwordSprint
                            : upper.SpearSprintPlayable;

                    return id == "001"
                        ? upper.SwordRunPlayable
                        : upper.SpearRunPlayable;
                }

            case 3:
                {
                    string id = inventory.SecondaryWeaponID();

                    if (playerMovement.Attacking && inventory.SecondaryWeapon.Supplies > 0)
                    {
                        return id == "003"
                            ? upper.RifleShootPlayable
                            : upper.BowDrawArrowPlayable;
                    }

                    if (!moving)
                        return id == "003"
                            ? upper.RifleIdle
                            : upper.BowIdlePlayable;

                    if (canSprint)
                        return id == "003"
                            ? upper.RifleSprintPlayable
                            : upper.BowSprintPlayable;

                    return id == "003"
                        ? upper.RifleRunPlayable
                        : upper.BowRunPlayable;
                }
        }

        return upper.IdlePlayables;
    }
}
