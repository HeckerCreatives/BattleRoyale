using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperTrap : UpperNoAimState
{
    float timer;
    bool canAction;

    public PlayerUpperTrap(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

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

        if (playerPlayables.healthV2.IsDead)
        {

            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (!characterController.IsGrounded)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
                
            }

            if (playerMovement.IsJumping)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
                
            }

            if (playerMovement.IsBlocking)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BlockPlayable);
                
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

            WeaponChecker();
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


            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
                canAction = false;

            }

            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);
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


                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                    canAction = false;

                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);
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


                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
                    canAction = false;

                }

                if (playerMovement.Attacking)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearFirstAttackPlayable);
                }
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

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
                    canAction = false;

                }

                //if (playerMovement.Attacking)
                //{
                //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);
                //}
            }
            if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {


                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);


                }

                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
                    canAction = false;

                }

                //if (playerMovement.Attacking)
                //{
                //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordAttackFirstPlayable);
                //}
            }
        }
    }
}
