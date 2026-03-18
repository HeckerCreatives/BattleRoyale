using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class FinalPunchState : PlayerOnGround
{
    public FinalPunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void NetworkUpdate()
    {
        //HandleMoveWindow();


        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
            playerPlayables.stamina.RecoverStamina(5f);
    }

    //private void HandleMoveWindow()
    //{
    //    double animTime = animationClipPlayable.GetTime();

    //    bool moveWindow = animTime >= 0.30f && animTime <= 0.50f;

    //    if (moveWindow)
    //    {
    //        characterController.Move(characterController.TransformDirection * 1.25f, 0f);
    //    }
    //}

    private AnimationPlayable GetNextState()
    {
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        return GetRecoveryState();
    }

    private AnimationPlayable GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.lowerBodyMovement.StaggerHitPlayable;

        return null;
    }

    private AnimationPlayable GetRecoveryState()
    {
        double animTime = animationClipPlayable.GetTime();

        bool finishedPunch = animTime >= animationLength;
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;

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
