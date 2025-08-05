using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperGettingUp : UpperBodyAnimations
{
    float timer;
    bool canAction;

    public PlayerUpperGettingUp(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

            return;
        }

        if (!characterController.IsGrounded)
        {
            playerPlayables.healthV2.IsGettingUp = false;
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
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
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
                    return;
                }

                if (playerPlayables.healthV2.IsHitUpper)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
                    return;
                }

                if (playerPlayables.healthV2.IsStagger)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
                    return;
                }

                if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
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
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BlockPlayable);
                return;
            }

            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);
                return;
            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
            else
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.IsBlocking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordBlockPlayable);
                    return;
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);
                    return;
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
                }
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.IsBlocking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearBlockPlayable);
                    return;
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearFirstAttackPlayable);
                    return;
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);
                }
            }
        }
    }
}
