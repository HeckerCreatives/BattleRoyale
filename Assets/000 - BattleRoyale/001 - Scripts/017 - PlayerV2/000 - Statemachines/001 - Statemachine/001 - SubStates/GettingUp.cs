using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class GettingUp : PlayerOnGround
{
    float timer;
    bool canAction;

    public GettingUp(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.healthV2.IsGettingUp = true;

        timer = playerPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }

    public override void NetworkUpdate()
    {
        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playerPlayables.healthV2.IsGettingUp = false;
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

            return;
        }

        if (!characterController.IsGrounded)
        {
            playerPlayables.healthV2.IsGettingUp = false;
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);
            return;
        }

        if (canAction)
        {
            if (playerPlayables.TickRateAnimation >= timer)
            {
                playerPlayables.healthV2.IsGettingUp = false;

                WeaponsChecker();

                if (playerMovement.IsJumping)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPlayable);
                    return;
                }

                if (playerPlayables.healthV2.IsHit)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
                    return;
                }

                if (playerPlayables.healthV2.IsStagger)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
                    return;
                }

                if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);
                    return;
                }
                
            }
        }
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.IsBlocking)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);
                return;
            }

            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.Punch1Playable);
                return;
            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);
            else
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.IsBlocking)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordBlockPlayable);
                    return;
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable);
                    return;
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
                }
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.IsBlocking)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearBlockPlayable);
                    return;
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearFirstAttackPlayable);
                    return;
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearIdlePlayable);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
                }
            }
        }
    }
}
