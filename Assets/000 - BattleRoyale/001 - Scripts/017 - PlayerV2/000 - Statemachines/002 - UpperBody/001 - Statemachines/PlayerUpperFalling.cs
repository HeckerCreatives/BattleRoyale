using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperFalling : UpperNoAimState
{
    public PlayerUpperFalling(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();

        Animation();
        WeaponsChecker();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        //if (playerPlayables.healthV2.IsHit)
        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
    }


    private void WeaponsChecker()
    {
        if (playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPunchPlayable);
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordJumpAttackPlayable);
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearJumpAttackPlayable);
            }
        }

        if (characterController.IsGrounded)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
                else if (playerMovement.IsSprint)
                {
                    if (playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
            }
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                    else if (playerMovement.IsSprint)
                    {
                        if (playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
                }
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
                    else if (playerMovement.IsSprint)
                    {
                        if (playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);
                }
            }
            else if (playerPlayables.inventory.WeaponIndex == 3)
            {
                if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
                    else if (playerMovement.IsSprint)
                    {
                        if (playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
                }
                else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
                    else if (playerMovement.IsSprint)
                    {
                        if (playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);
                }
            }
        }
        else
        {
            if (playerPlayables.inventory.WeaponIndex == 3)
            {
                if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleFallingPlayable);

                else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowFallingPlayable);
            }
        }
    }
}
