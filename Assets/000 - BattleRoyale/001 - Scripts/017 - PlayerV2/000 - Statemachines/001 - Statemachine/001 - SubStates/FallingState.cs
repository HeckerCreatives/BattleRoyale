using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class FallingState : AnimationPlayable
{

    public FallingState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.IsFalling = true;
        playerMovement.Jumping = false;
        playerPlayables.healthV2.IsStagger = false;
    }

    public override void Exit()
    {
        base.Exit();

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.JumpImpulse = 0f;
        playerPlayables.healthV2.FallDamageValue = 0f;
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();
        playerMovement.Jumping = false;
        playerMovement.Falling();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
        {
            FallDamage();
        }
    }

    private void FallDamage()
    { 
        if (characterController.RealVelocity.y <= -20f) 
            playerPlayables.healthV2.FallDamageValue = Mathf.Abs(characterController.RealVelocity.y) - 5f; 
    }

    private AnimationPlayable GetNextState()
    {
        // Highest priority: death
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        // Air attack has priority while still airborne
        if (!characterController.IsGrounded && playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                return playerPlayables.lowerBodyMovement.JumpPunchPlayable;

            if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    return playerPlayables.lowerBodyMovement.SwordJumpAttackPlayable;

                if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    return playerPlayables.lowerBodyMovement.SpearJumpAttackPlayable;
            }
        }

        // If landed, resolve landing effects and choose next grounded state
        if (characterController.IsGrounded)
        {
            playerMovement.JumpImpulse = 0;
            playerMovement.Attacking = false;  // <-- Clear stale attack flag on land

            if (playerPlayables.healthV2.FallDamageValue > 0)
                playerPlayables.healthV2.FallDamae();

            return GetGroundedState();
        }

        // Still airborne
        return GetAirborneState();
    }

    private AnimationPlayable GetGroundedState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                return playerPlayables.lowerBodyMovement.IdlePlayable;

            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                return playerPlayables.lowerBodyMovement.SprintPlayable;

            return playerPlayables.lowerBodyMovement.RunPlayable;
        }

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.SwordIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SwordSprintPlayable;

                return playerPlayables.lowerBodyMovement.SwordRunPlayable;
            }

            if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.SpearIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SpearSprintPlayable;

                return playerPlayables.lowerBodyMovement.SpearRunPlayable;
            }
        }

        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.RifleIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.RifleSprintPlayable;

                return playerPlayables.lowerBodyMovement.RifleRunPlayable;
            }

            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    return playerPlayables.lowerBodyMovement.BowIdlePlayable;

                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.BowSprintPlayable;

                return playerPlayables.lowerBodyMovement.BowRunPlayable;
            }
        }

        return null;
    }

    private AnimationPlayable GetAirborneState()
    {
        if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                return playerPlayables.lowerBodyMovement.RifleFallingPlayable;
        }

        return null;
    }
}
