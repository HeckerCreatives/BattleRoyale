using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperBowDraw : UpperWithAimState
{
    public PlayerUpperBowDraw(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority)
        {
            playerMovement.AnimationTick = playerPlayables.Runner.Tick;
            //playerPlayables.ChangeCamera(true); // AUTO-DISABLED shoulder zoom — uncomment to restore
        }

        // Bow string: bend toward the pulling hand on every peer that runs
        // this state's Enter. SetDrawn is null-safe and a no-op for rifles.
        playerPlayables.inventory.SecondaryWeapon?.SetDrawn(true);

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.AuthoritiveAniamtionTick = playerPlayables.Runner.Tick;
    }

    public override void Exit()
    {
        base.Exit();

        // Bow string returns to rest. Runs on every peer.
        playerPlayables.inventory.SecondaryWeapon?.SetDrawn(false);

        if (!playerPlayables.HasInputAuthority) return;

        //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
        playerPlayables.cameraRotation.ExitBowAimCrosshair();
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        playerPlayables.cameraRotation.HandleCameraAimInputBow();

        var nextState = GetNextLowerBodyState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetNextLowerBodyState()
    {
        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.AuthoritiveAniamtionTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (playerPlayables.healthV2.IsDead)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.DeathPlayable;
        }

        if (playerMovement.IsJumping)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.JumpPlayable;
        }

        if (playerMovement.IsBlocking)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.BlockPlayable;
        }

        if (playerMovement.IsHealing)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.HealPlayable;
        }

        if (playerMovement.IsRepairing)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.RepairPlayable;
        }

        if (playerMovement.IsTrapping)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.TrapPlayable;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.RollPlayables;
        }

        if (elapsedTicks < finishStartTick)
            return null;

        return playerPlayables.upperBodyMovement.BowChargePlayable;
    }
}
