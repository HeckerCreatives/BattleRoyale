using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PunchState : PlayerOnGround
{
    bool canMove;

    public PunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        canMove = true;
    }

    public override void Exit()
    {
        base.Exit();

        canMove = false;
    }

    public override void NetworkUpdate()
    {
        playerMovement.RotatePlayer();

        //HandleMoveWindow();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
            playerPlayables.stamina.RecoverStamina(5f);
    }

    private AnimationPlayable GetNextState()
    {
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        var comboState = GetComboState();
        if (comboState != null)
            return comboState;

        return GetRecoveryState();
    }

    private AnimationPlayable GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.lowerBodyMovement.StaggerHitPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        return null;
    }

    private AnimationPlayable GetComboState()
    {
        double animTime = animationClipPlayable.GetTime();

        bool comboWindow = animTime >= animationLength * 0.8f;

        if (comboWindow && playerMovement.Attacking)
            return playerPlayables.lowerBodyMovement.Punch2Playable;

        return null;
    }

    private AnimationPlayable GetRecoveryState()
    {
        double animTime = animationClipPlayable.GetTime();

        bool finishedPunch = animTime >= animationLength * 0.9f;
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;


        //if (playerMovement.IsBlocking)
        //    return playerPlayables.lowerBodyMovement.BlockPlayable;

        if (canRoll)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (isMoving)
        {
            return playerMovement.IsSprint
                ? playerPlayables.lowerBodyMovement.SprintPlayable
                : playerPlayables.lowerBodyMovement.RunPlayable;
        }

        if (!finishedPunch)
            return null;

        return playerPlayables.lowerBodyMovement.IdlePlayable;
    }
}
