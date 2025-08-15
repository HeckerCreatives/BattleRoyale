using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SwordSprintState : PlayerOnGround
{
    public SwordSprintState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.CancelInvoke();

        playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), (animationLength * 0.20f), animationLength);
        playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), (animationLength * 0.80f), animationLength);
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.CancelInvoke();
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();
        WeaponsChecker();
        Animation();
        playerPlayables.stamina.DecreaseStamina(20f);
    }

    private void Animation()
    {
        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 50f)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);

        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPlayable);

        if (playerMovement.IsBlocking)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordBlockPlayable);

        if (playerMovement.Attacking)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable);

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.TrappingPlayable);
        }

        //if (playerPlayables.healthV2.IsHit)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
        //    return;
        //}

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
        }
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.stamina.Stamina > 0f)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.MoveDirection != Vector3.zero)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

                return;
            }

            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeapon.WeaponID == "002")
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);
                }
            }
            else if (playerPlayables.inventory.WeaponIndex == 3)
            {
                if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleSprintPlayable);
                }
                else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                {
                    if (playerMovement.MoveDirection != Vector3.zero)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowSprintPlayable);
                }
            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
            }
            else if (!playerMovement.IsSprint)
            {
                if (playerMovement.MoveDirection != Vector3.zero)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
            }
        }
        else
        {
            if (playerMovement.MoveDirection == Vector3.zero)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
        }
    }
}
