using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRifleReload : UpperNoAimState
{
    float timer;
    float reloadtimer;
    bool doneReload;

    public PlayerUpperRifleReload(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.inventory.SecondaryWeapon.SoundController.PlayReload();

        timer = playerPlayables.TickRateAnimation + animationLength;
        reloadtimer = playerPlayables.TickRateAnimation + (animationLength * 0.9f);
        doneReload = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        if (playerPlayables.TickRateAnimation >= reloadtimer && !doneReload)
        {
            int ammoNeeded = 10 - playerPlayables.inventory.SecondaryWeapon.Supplies; // How much ammo is needed to fill the magazine
            int ammoToLoad = Mathf.Min(ammoNeeded, playerPlayables.inventory.RifleMagazine); // Take only what's available

            playerPlayables.inventory.SecondaryWeapon.Supplies += ammoToLoad;
            playerPlayables.inventory.RifleMagazine -= ammoToLoad;
        }

        playerMovement.WeaponSwitcher();
        WeaponsChecker();
        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        if (!characterController.IsGrounded)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleFallingPlayable);

        if (playerPlayables.healthV2.IsStagger)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);

        if (playerMovement.IsHealing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HealPlayable);

        if (playerMovement.IsRepairing)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RepairPlayable);

        if (playerMovement.IsTrapping)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.TrapPlayable);

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
    }

    private void WeaponsChecker()
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
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
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
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
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
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.TickRateAnimation >= timer)
            {
                if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                {
                    if (!characterController.IsGrounded)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleFallingPlayable);

                    if (playerMovement.Attacking && playerPlayables.inventory.SecondaryWeapon.Supplies > 0)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleShootPlayable);
                    else
                    {
                        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                        {
                            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);

                            else
                                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
                        }
                        else
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
                    }
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
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
            }
        }
    }
}
