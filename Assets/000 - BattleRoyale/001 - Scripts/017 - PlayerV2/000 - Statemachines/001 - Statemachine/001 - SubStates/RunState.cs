using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class RunState : PlayerOnGround
{
    public RunState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();
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

        if (playerMovement.IsBlocking)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);

        if (playerPlayables.FinalAttack)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.Punch3Playable);

        if (playerPlayables.healthV2.IsHit)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsSecondHit)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.MiddleHitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
            return;
        }

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.TrappingPlayable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);

            if (playerMovement.IsSprint)
            {
                if (playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);
            }
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
    }
}
