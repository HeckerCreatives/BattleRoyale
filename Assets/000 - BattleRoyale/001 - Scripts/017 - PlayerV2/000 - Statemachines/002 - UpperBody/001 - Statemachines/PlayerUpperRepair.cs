using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRepair : UpperNoAimState
{
    float healtimer;
    float timer;
    bool canAction;

    public PlayerUpperRepair(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        healtimer = playerPlayables.TickRateAnimation + (animationLength * 0.5f);
        timer = playerPlayables.TickRateAnimation + animationLength;

        if (playerPlayables.inventory.PrimaryWeapon != null && playerPlayables.inventory.WeaponIndex == 2) playerPlayables.inventory.PrimaryWeapon.IsEquipped = false;
        else if (playerPlayables.inventory.SecondaryWeapon != null && playerPlayables.inventory.WeaponIndex == 3) playerPlayables.inventory.SecondaryWeapon.IsEquipped = false;

        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.inventory.PrimaryWeapon != null && playerPlayables.inventory.WeaponIndex == 2) playerPlayables.inventory.PrimaryWeapon.IsEquipped = true;
        else if (playerPlayables.inventory.SecondaryWeapon != null && playerPlayables.inventory.WeaponIndex == 3) playerPlayables.inventory.SecondaryWeapon.IsEquipped = true;

        canAction = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        if (playerPlayables.TickRateAnimation < healtimer)
        {
            WeaponChecker();
            Animations();
        }

        if (canAction && playerPlayables.TickRateAnimation >= timer)
        {
            WeaponChecker();
            Animations();
        }
    }

    private void Animations()
    {
        if (playerPlayables.healthV2.IsDead)
        {

            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
        }

        //if (playerPlayables.healthV2.IsHitUpper)
        //{

        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
        //    
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

    private void WeaponChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);

                return;
            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                if (canAction && playerPlayables.TickRateAnimation >= timer)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
                    canAction = false;
                    return;
                }
            }

            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);
                return;
            }

            if (playerMovement.IsBlocking)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BlockPlayable);
            }

            if (!characterController.IsGrounded)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
            }

            if (playerMovement.IsJumping)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);

                    return;
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    if (canAction && playerPlayables.TickRateAnimation >= timer)
                    {
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                        canAction = false;
                        return;
                    }
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);
                    return;
                }

                if (playerMovement.IsBlocking)
                {

                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordBlockPlayable);
                }
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);

                    return;
                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    if (canAction && playerPlayables.TickRateAnimation >= timer)
                    {
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
                        canAction = false;
                        return;
                    }
                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearFirstAttackPlayable);
                    return;
                }

                if (playerMovement.IsBlocking)
                {

                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearBlockPlayable);
                }
            }


            if (!characterController.IsGrounded)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
            }

            if (playerMovement.IsJumping)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
                }
                else
                {
                    if (canAction && playerPlayables.TickRateAnimation >= timer)
                    {
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
                        canAction = false;
                    }
                }

                if (!characterController.IsGrounded)
                {

                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleFallingPlayable);
                }

                if (playerMovement.IsJumping)
                {

                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleJumpPlayable);
                }
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);
                }
                else
                {
                    if (canAction && playerPlayables.TickRateAnimation >= timer)
                    {
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
                        canAction = false;
                    }
                }

                if (!characterController.IsGrounded)
                {

                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowFallingPlayable);
                }

                if (playerMovement.IsJumping)
                {

                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowJumpPlayable);
                }
            }
        }
    }
}
