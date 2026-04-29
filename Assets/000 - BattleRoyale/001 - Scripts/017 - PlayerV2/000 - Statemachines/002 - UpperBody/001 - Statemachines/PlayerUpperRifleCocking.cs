using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRifleCocking : UpperNoAimState
{
    private int _lastEnterProcessedTick = -1;

    public PlayerUpperRifleCocking(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
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
            playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (playerPlayables.HasStateAuthority)
            playerMovement.AuthoritiveAniamtionTick = playerPlayables.Runner.Tick;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

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

        if (playerMovement.Attacking && playerPlayables.inventory.SecondaryWeapon.Supplies > 0)
            return playerPlayables.upperBodyMovement.RifleAimPlayable;

        if (isMoving)
        {
            if (canSprint)
                return playerPlayables.upperBodyMovement.RifleSprintPlayable;

            return playerPlayables.upperBodyMovement.RifleRunPlayable;
        }

        if (canRoll)
            return playerPlayables.upperBodyMovement.RollPlayables;

        return playerPlayables.upperBodyMovement.RifleIdle;
    }
}
