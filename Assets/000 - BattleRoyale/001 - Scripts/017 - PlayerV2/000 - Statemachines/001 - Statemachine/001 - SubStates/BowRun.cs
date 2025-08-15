using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BowRun : PlayerOnGround
{
    public BowRun(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.CancelInvoke();

        playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), (animationLength * 0.35f), animationLength);
        playerPlayables.InvokeRepeating(nameof(playerPlayables.PlayFootstepSound), (animationLength * 0.85f), animationLength);
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
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

        if (playerMovement.IsJumping)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPlayable);

        if (playerPlayables.healthV2.IsStagger)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.TrappingPlayable);

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleRunPlayable);
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowSprintPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowIdlePlayable);

            }
        }
    }
}
