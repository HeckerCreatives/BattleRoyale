using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BowCharge : PlayerOnGround
{
    float timer;

    public BowCharge(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
    }

    public override void NetworkUpdate()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

        }

        if (playerMovement.IsJumping)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPlayable);

        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);

        }

        if (playerMovement.IsHealing)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HealPlayable);

        }

        if (playerMovement.IsRepairing)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RepairPlayable);

        }

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.TrappingPlayable);

        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);

        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

        }

        if (!playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowShotPlayable);

        WeaponsChecker();

        if (playerPlayables.TickRateAnimation >= timer)
        {
            if (playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowShotPlayable);
        }
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearIdlePlayable);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleIdlePlayable);
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowRunPlayable);
                }
            }
        }
    }
}
