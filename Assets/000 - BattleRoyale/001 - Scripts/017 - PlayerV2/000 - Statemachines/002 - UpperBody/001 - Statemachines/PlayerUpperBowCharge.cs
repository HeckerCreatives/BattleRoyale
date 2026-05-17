using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperBowCharge : UpperWithAimState
{
    public PlayerUpperBowCharge(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority)
            playerPlayables.ChangeCamera(true);
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

        playerPlayables.ChangeCamera(false);
        playerPlayables.cameraRotation.ExitBowAimCrosshair();
    }

    private UpperBodyAnimations GetNextLowerBodyState()
    {
        if (playerPlayables.healthV2.IsDead)
        {
            playerPlayables.ChangeCamera(false);
            return playerPlayables.upperBodyMovement.DeathPlayable;
        }

        if (playerMovement.IsHealing)
        {
            playerPlayables.ChangeCamera(false);
            return playerPlayables.upperBodyMovement.HealPlayable;
        }

        if (playerMovement.IsRepairing)
        {
            playerPlayables.ChangeCamera(false);
            return playerPlayables.upperBodyMovement.RepairPlayable;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {
            playerPlayables.ChangeCamera(false);
            return playerPlayables.upperBodyMovement.RollPlayables;
        }

        if (!playerMovement.Attacking)
            return playerPlayables.upperBodyMovement.BowShotPlayable;

        return null;
    }
}
