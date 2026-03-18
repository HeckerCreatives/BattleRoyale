using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperTrap : UpperNoAimState
{
    public PlayerUpperTrap(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        if (playerPlayables.healthV2.IsDead)
        {
            ChangeTo(playerPlayables.upperBodyMovement.DeathPlayable);
            return;
        }

        double animTime = animationClipPlayable.GetTime();
        if (animTime < animationLength - 0.025f)
            return;


        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetNextStateAfterTrap();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }
        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetNextStateAfterTrap();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }

    }

    private UpperBodyAnimations GetNextStateAfterTrap()
    {
        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.upperBodyMovement.BlockPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.upperBodyMovement.StaggerHitPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        return GetWeaponState();
    }

    private UpperBodyAnimations GetWeaponState()
    {
        int weaponIndex = playerPlayables.inventory.WeaponIndex;
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (weaponIndex)
        {
            case 1:
                return GetUnarmedState(isMoving, canSprint);

            case 2:
                return GetPrimaryWeaponState(isMoving, canSprint);

            case 3:
                return GetSecondaryWeaponState(isMoving, canSprint);
        }

        return null;
    }

    private UpperBodyAnimations GetUnarmedState(bool isMoving, bool canSprint)
    {
        if (playerMovement.Attacking)
            return playerPlayables.upperBodyMovement.FirstPunch;

        if (isMoving)
        {
            return canSprint
                ? playerPlayables.upperBodyMovement.SprintPlayables
                : playerPlayables.upperBodyMovement.RunPlayables;
        }

        return playerPlayables.upperBodyMovement.IdlePlayables;
    }

    private UpperBodyAnimations GetPrimaryWeaponState(bool isMoving, bool canSprint)
    {
        string weaponId = playerPlayables.inventory.PrimaryWeaponID();

        if (weaponId == "001")
        {
            if (playerMovement.Attacking)
                return playerPlayables.upperBodyMovement.SwordAttackFirstPlayable;

            if (isMoving)
            {
                return canSprint
                    ? playerPlayables.upperBodyMovement.SwordSprint
                    : playerPlayables.upperBodyMovement.SwordRunPlayable;
            }

            return playerPlayables.upperBodyMovement.SwordIdlePlayable;
        }

        if (weaponId == "002")
        {
            if (playerMovement.Attacking)
                return playerPlayables.upperBodyMovement.SpearFirstAttackPlayable;

            if (isMoving)
            {
                return canSprint
                    ? playerPlayables.upperBodyMovement.SpearSprintPlayable
                    : playerPlayables.upperBodyMovement.SpearRunPlayable;
            }

            return playerPlayables.upperBodyMovement.SpearIdle;
        }

        return null;
    }

    private UpperBodyAnimations GetSecondaryWeaponState(bool isMoving, bool canSprint)
    {
        string weaponId = playerPlayables.inventory.SecondaryWeaponID();

        if (weaponId == "003")
        {
            if (isMoving)
            {
                return canSprint
                    ? playerPlayables.upperBodyMovement.RifleSprintPlayable
                    : playerPlayables.upperBodyMovement.RifleRunPlayable;
            }

            return playerPlayables.upperBodyMovement.RifleIdle;
        }

        if (weaponId == "004")
        {
            if (isMoving)
            {
                return canSprint
                    ? playerPlayables.upperBodyMovement.BowSprintPlayable
                    : playerPlayables.upperBodyMovement.BowRunPlayable;
            }

            return playerPlayables.upperBodyMovement.BowIdlePlayable;
        }

        return null;
    }

    private void ChangeTo(UpperBodyAnimations nextState)
    {
        if (nextState == null || playablesChanger.CurrentState == nextState)
            return;

        playablesChanger.ChangeState(nextState);
    }
}
