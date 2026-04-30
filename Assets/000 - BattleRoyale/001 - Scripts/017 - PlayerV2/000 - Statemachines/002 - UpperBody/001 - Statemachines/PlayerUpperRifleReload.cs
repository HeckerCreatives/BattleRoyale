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

        int startTick = playerPlayables.HasStateAuthority ? playerMovement.AuthoritiveAniamtionTick : playerMovement.AnimationTick;
        int elapsedTicks = playerPlayables.Runner.Tick - startTick;

        int totalPunchTicks = Mathf.CeilToInt(animationLength / playerPlayables.Runner.DeltaTime);
        int doneReloadTick = Mathf.CeilToInt(totalPunchTicks * 0.7f);
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (playerPlayables.HasInputAuthority)
            playerPlayables.SetReloadProgress(Mathf.Clamp01((float)elapsedTicks / totalPunchTicks));

        if (elapsedTicks >= doneReloadTick && !doneReload)
        {
            int ammoNeeded = 10 - playerPlayables.inventory.SecondaryWeapon.Supplies;
            int ammoToLoad = Mathf.Min(ammoNeeded, playerPlayables.inventory.RifleMagazine);

            playerPlayables.inventory.SecondaryWeapon.Supplies += ammoToLoad;
            playerPlayables.inventory.RifleMagazine -= ammoToLoad;

            doneReload = true;
        }

        playerMovement.WeaponSwitcher();

        var nextState = GetNextUpperBodyState(elapsedTicks, finishStartTick);

        if (nextState != null && playablesChanger.CurrentState != nextState)
            playablesChanger.ChangeState(nextState);
    }

    private UpperBodyAnimations GetNextUpperBodyState(int elapsedTicks, int finishStartTick)
    {
        var upper = playerPlayables.upperBodyMovement;

        if (playerPlayables.healthV2.IsDead)
            return upper.DeathPlayable;

        if (!characterController.IsGrounded)
            return upper.RifleFallingPlayable;

        if (playerMovement.IsHealing)
            return upper.HealPlayable;

        if (playerMovement.IsRepairing)
            return upper.RepairPlayable;

        if (playerMovement.IsTrapping)
            return upper.TrapPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return upper.RollPlayables;

        return GetWeaponExitState(elapsedTicks, finishStartTick);
    }

    private UpperBodyAnimations GetWeaponExitState(int elapsedTicks, int finishStartTick)
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;
        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool isSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                if (isMoving)
                    return isSprint ? upper.SprintPlayables : upper.RunPlayables;
                return upper.IdlePlayables;

            case 2:
                switch (inventory.PrimaryWeaponID())
                {
                    case "001":
                        if (isMoving)
                            return isSprint ? upper.SwordSprint : upper.SwordRunPlayable;
                        return upper.SwordIdlePlayable;
                    case "002":
                        if (isMoving)
                            return isSprint ? upper.SpearSprintPlayable : upper.SpearRunPlayable;
                        return upper.SpearIdle;
                }
                break;

            case 3:
                switch (inventory.SecondaryWeaponID())
                {
                    case "003":
                        if (elapsedTicks < finishStartTick)
                            return null;

                        if (playerMovement.Attacking && inventory.SecondaryWeapon.Supplies > 0)
                            return upper.RifleAimPlayable;

                        if (isMoving)
                            return isSprint ? upper.RifleSprintPlayable : upper.RifleRunPlayable;

                        return upper.RifleIdle;

                    case "004":
                        if (isMoving)
                            return isSprint ? upper.BowSprintPlayable : upper.BowRunPlayable;
                        return upper.BowIdlePlayable;
                }
                break;
        }

        return null;
    }
}
