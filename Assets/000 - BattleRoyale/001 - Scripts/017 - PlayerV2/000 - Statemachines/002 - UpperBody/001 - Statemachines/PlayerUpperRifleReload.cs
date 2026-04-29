using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRifleReload : UpperNoAimState
{
    private int _lastEnterProcessedTick = -1;
    bool doneReload;

    public PlayerUpperRifleReload(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        int currentTick = playerPlayables.Runner != null ? playerPlayables.Runner.Tick : -1;

        if (_lastEnterProcessedTick == currentTick)
            return;

        _lastEnterProcessedTick = currentTick;

        if (playerPlayables.HasInputAuthority)
        {
            playerMovement.AnimationTick = playerPlayables.Runner.Tick;
            playerPlayables.ShowReloadProgress();
        }

        if (playerPlayables.HasStateAuthority)
        {
            playerMovement.AuthoritiveAniamtionTick = playerPlayables.Runner.Tick;
            doneReload = false;
        }

        if (!playerPlayables.HasStateAuthority)
            playerPlayables.inventory.SecondaryWeapon.SoundController.PlayReload();
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.HasInputAuthority)
            playerPlayables.HideReloadProgress();
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.AuthoritiveAniamtionTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (playerPlayables.HasInputAuthority)
        {
            playerPlayables.SetReloadProgress(Mathf.Clamp01(elapsedTicks / animationLength));
        }

        if (elapsedTicks >= finishStartTick && !doneReload)
        {
            int ammoNeeded = 10 - playerPlayables.inventory.SecondaryWeapon.Supplies; // How much ammo is needed to fill the magazine
            int ammoToLoad = Mathf.Min(ammoNeeded, playerPlayables.inventory.RifleMagazine); // Take only what's available

            playerPlayables.inventory.SecondaryWeapon.Supplies += ammoToLoad;
            playerPlayables.inventory.RifleMagazine -= ammoToLoad;

            doneReload = true;
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
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.AuthoritiveAniamtionTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

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
            if (elapsedTicks >= finishStartTick)
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
