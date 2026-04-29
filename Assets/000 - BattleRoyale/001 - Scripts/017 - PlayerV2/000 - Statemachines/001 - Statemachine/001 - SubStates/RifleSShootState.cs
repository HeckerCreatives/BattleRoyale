using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RifleSShootState : PlayerOnGround
{
    public RifleSShootState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void NetworkUpdate()
    {
        if (playerPlayables.healthV2.IsDead)
        { playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable); return; }

        if (!characterController.IsGrounded)
        { playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable); return; }

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        var next = isMoving
            ? (AnimationPlayable)playerPlayables.lowerBodyMovement.RifleAimMovePlayable
            : playerPlayables.lowerBodyMovement.RifleAimIdlePlayable;

        if (playablesChanger.CurrentState != next)
            playablesChanger.ChangeState(next);
    }
}
