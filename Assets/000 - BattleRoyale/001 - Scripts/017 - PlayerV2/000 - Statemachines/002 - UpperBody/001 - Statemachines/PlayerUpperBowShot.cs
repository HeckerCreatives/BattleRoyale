using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperBowShot : UpperWithAimState
{
    private int _lastEnterProcessedTick = -1;

    public PlayerUpperBowShot(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
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
            //playerPlayables.ChangeCamera(true); // AUTO-DISABLED shoulder zoom — uncomment to restore
        }

        // Bow string: still bent toward the hand during the release frame.
        // (Exit flips it back to false when the player leaves bow aim.)
        playerPlayables.inventory.SecondaryWeapon?.SetDrawn(true);

        if (playerPlayables.HasStateAuthority)
            playerMovement.AuthoritiveAniamtionTick = playerPlayables.Runner.Tick;

        playerPlayables.FireArrow();
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
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.AuthoritiveAniamtionTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (elapsedTicks < finishStartTick)
            return null;


        if (playerMovement.Attacking && playerPlayables.inventory.BowAmmo() > 0)
            return playerPlayables.upperBodyMovement.BowDrawArrowPlayable;

        if (isMoving)
        {
            if (canSprint)
                return playerPlayables.upperBodyMovement.BowSprintPlayable;

            return playerPlayables.upperBodyMovement.BowRunPlayable;
        }

        if (canRoll)
            return playerPlayables.upperBodyMovement.RollPlayables;

        return playerPlayables.upperBodyMovement.BowIdlePlayable;
    }
}
