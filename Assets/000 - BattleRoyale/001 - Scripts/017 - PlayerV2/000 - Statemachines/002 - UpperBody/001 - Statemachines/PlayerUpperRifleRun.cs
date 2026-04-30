using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRifleRun : UpperNoAimState
{
    public PlayerUpperRifleRun(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerMovement.WeaponSwitcher();

        var nextState = GetNextUpperRunState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextUpperRunState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var health = playerPlayables.healthV2;

        // Priority order
        if (health.IsDead)
            return upper.DeathPlayable;

        if (playerMovement.IsJumping)
            return upper.JumpPlayable;

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        if (playerMovement.IsBlocking)
            return upper.BlockPlayable;

        if (playerMovement.IsHealing)
            return upper.HealPlayable;

        if (playerMovement.IsRepairing)
            return upper.RepairPlayable;

        if (playerMovement.IsTrapping)
            return upper.TrapPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return upper.RollPlayables;

        return GetWeaponBasedUpperRunState();
    }

    private UpperBodyAnimations GetWeaponBasedUpperRunState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool isSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                return upper.RunPlayables;
            case 2:
                {
                    if (!isMoving)
                    {
                        if (inventory.PrimaryWeaponID() == "001")
                            return upper.SwordIdlePlayable;

                        if (inventory.PrimaryWeaponID() == "002")
                            return upper.SpearIdle;
                    }
                    else
                    {
                        if (inventory.PrimaryWeaponID() == "001")
                            return upper.SwordRunPlayable;

                        if (inventory.PrimaryWeaponID() == "002")
                            return upper.SpearRunPlayable;
                    }

                    break;
                }

            case 3:
                {
                    if (!isMoving)
                    {
                        if (inventory.SecondaryWeaponID() == "003")
                        {
                            if (playerMovement.Reloading && playerPlayables.inventory.RifleMagazine > 0 && playerPlayables.inventory.SecondaryWeapon.Supplies < 10)
                                return upper.RifleReloadPlayable;

                            return upper.RifleIdle;
                        }

                        if (inventory.SecondaryWeaponID() == "004")
                        {
                            if (playerMovement.Attacking)
                                return upper.BowDrawArrowPlayable;

                            return upper.BowIdlePlayable;
                        }
                    }
                    else
                    {

                        if (inventory.SecondaryWeaponID() == "003")
                        {
                            if (playerMovement.Reloading && playerPlayables.inventory.RifleMagazine > 0 && playerPlayables.inventory.SecondaryWeapon.Supplies < 10)
                                return upper.RifleReloadPlayable;

                            if (playerMovement.Attacking && inventory.SecondaryWeapon.Supplies > 0)
                                return upper.RifleAimPlayable;

                            if (isMoving && isSprint)
                                return upper.RifleSprintPlayable;
                        }

                        if (inventory.SecondaryWeaponID() == "004")
                            return upper.BowRunPlayable;
                    }

                    break;
                }
        }

        return null;
    }
}
