using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class RunState : PlayerOnGround
{
    float firstStepTimer;
    float secondStepTimer;

    public RunState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.CancelInvoke();

            playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), animationLength * 0.35f, animationLength);
            playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), animationLength * 0.85f, animationLength);
        }
    }

    public override void Exit()
    {
        base.Exit();

        if (!playerPlayables.HasStateAuthority)
            playerPlayables.CancelInvoke();
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

    private AnimationPlayable GetNextLowerRunState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var health = playerPlayables.healthV2;

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

        if (playerPlayables.FinalAttack)
            return lower.Punch3Playable;

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

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (!isMoving)
                        return lower.IdlePlayable;

                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return lower.SprintPlayable;

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
                    if (!isMoving)
                    {
                        if (inventory.SecondaryWeaponID() == "003")
                            return lower.RifleIdlePlayable;

                        if (inventory.SecondaryWeaponID() == "004")
                            return lower.BowIdlePlayable;
                    }
                    else
                    {
                        if (inventory.SecondaryWeaponID() == "003")
                            return lower.RifleRunPlayable;

                        if (inventory.SecondaryWeaponID() == "004")
                            return lower.BowRunPlayable;
                    }

                    break;
                }
        }

        return lower.IdlePlayable;
    }
}
