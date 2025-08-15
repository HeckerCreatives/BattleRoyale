using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperGettingUp : UpperNoAimState
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
        base.NetworkUpdate();

        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playerPlayables.healthV2.IsGettingUp = false;
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
        }

        if (!characterController.IsGrounded)
        {
            playerPlayables.healthV2.IsGettingUp = false;
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
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
                }

                //if (playerPlayables.healthV2.IsHitUpper)
                //{
                //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
                //    return;
                //}

                if (playerPlayables.healthV2.IsStagger)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
                }

                if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
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
            }

            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);
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
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);
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
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearFirstAttackPlayable);
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
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.Attacking && playerPlayables.inventory.SecondaryWeapon.Supplies > 0)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleShootPlayable);
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
                }
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.Attacking && playerPlayables.inventory.SecondaryWeapon.Supplies > 0)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowDrawArrowPlayable);
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
                else
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);
                }
            }
        }
    }
}
