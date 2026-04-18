using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperBowDraw : UpperBodyAnimations
{
    public PlayerUpperBowDraw(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        if (!playerPlayables.HasInputAuthority) return;

        playerPlayables.ChangeCamera(true);
    }

    public override void Exit()
    {
        playerPlayables.ChangeCamera(false);
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        var nextState = GetNextLowerBodyState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
            playerPlayables.stamina.RecoverStamina(5f);
    }

    private UpperBodyAnimations GetNextLowerBodyState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (playerMovement.IsBlocking)
            return playerPlayables.upperBodyMovement.BlockPlayable;

        if (playerMovement.IsHealing)
            return playerPlayables.upperBodyMovement.HealPlayable;

        if (playerMovement.IsRepairing)
            return playerPlayables.upperBodyMovement.RepairPlayable;

        if (playerMovement.IsTrapping)
            return playerPlayables.upperBodyMovement.TrapPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (!playerMovement.Attacking)
        {
            if (!characterController.IsGrounded)
                return playerPlayables.upperBodyMovement.FallingPlayables;

            return playerPlayables.upperBodyMovement.BowIdlePlayable;
        }

        return null;
    }
}
