using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRifleAim : UpperWithAimState
{
    public PlayerUpperRifleAim(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
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

        if (playerPlayables.HasStateAuthority)
            playerMovement.AuthoritiveAniamtionTick = playerPlayables.Runner.Tick;
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

    public override void Exit()
    {
        base.Exit();

        if (!playerPlayables.HasInputAuthority) return;

        //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
        playerPlayables.cameraRotation.ExitBowAimCrosshair();
    }

    private UpperBodyAnimations GetNextLowerBodyState()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.DeathPlayable;
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

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            //playerPlayables.ChangeCamera(false); // AUTO-DISABLED shoulder zoom — uncomment to restore
            return playerPlayables.upperBodyMovement.RollPlayables;
        }

        // Only shoot if we actually confirmed holding the button
        int aimStartTick = playerPlayables.HasStateAuthority
            ? playerMovement.AuthoritiveAniamtionTick
            : playerMovement.AnimationTick;

        if (playerMovement.ShootReleasedTick > aimStartTick)
            return playerPlayables.upperBodyMovement.RifleShootPlayable;

        return null;
    }
}
