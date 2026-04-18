using Fusion.Addons.SimpleKCC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Remoting.Messaging;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerFistFinalPunch : UpperNoAimState
{
    bool doneResetHit;

    public PlayerFistFinalPunch(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        doneResetHit = false;
        playerPlayables.FinalAttack = false;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.SetPunchRotation(0f);
    }

    public override void NetworkLocalUpdate()
    {
        base.NetworkLocalUpdate();

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            playerPlayables.SetPunchRotation(1f);
        else
            playerPlayables.SetPunchRotation(0f);
    }


    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        HandleDamageWindow();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private void HandleDamageWindow()
    {
        if (!playerPlayables.HasStateAuthority) return;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - playerMovement.PunchStartTick;

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int startStartTick = Mathf.CeilToInt(totalPunchTicks * 0.18f);
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (elapsedTicks >= startStartTick && elapsedTicks <= finishStartTick)
        {
            if (!doneResetHit)
            {
                playerPlayables.upperBodyMovement.ResetFirstAttack();
                doneResetHit = true;
            }

            playerPlayables.upperBodyMovement.PerformFirstAttack(true);
        }
    }

    private UpperBodyAnimations GetNextState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;


        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsSprint && (playerMovement.XMovement != 0f || playerMovement.YMovement != 0f))
            return playerPlayables.upperBodyMovement.SprintPlayables;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - playerMovement.PunchStartTick;

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (elapsedTicks >= finishStartTick)
            return playerPlayables.upperBodyMovement.IdlePlayables;

        return null;
    }
}
