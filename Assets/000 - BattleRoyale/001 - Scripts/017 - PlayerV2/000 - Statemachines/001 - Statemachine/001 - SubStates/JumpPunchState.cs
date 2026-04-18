using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class JumpPunchState : PlayerOnGround
{
    private int groundedSinceTick = -1;
    public JumpPunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        groundedSinceTick = -1;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.JumpAttackStartTick = playerPlayables.Runner.Tick;
        playerMovement.IsFalling = true;
        playerPlayables.healthV2.FallDamageValue = 0;
        playerMovement.Jumping = false;
    }

    public override void Exit()
    {
        base.Exit();

        groundedSinceTick = -1;

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.IsFalling = false;
        playerMovement.JumpImpulse = 0;
    }

    public override void NetworkUpdate()
    {
        playerMovement.Jumping = false;
        playerMovement.Falling();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            OnLanding();
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
        {
            FallDamage();
        }

        playerMovement.MoveCharacter();
    }

    private void FallDamage()
    {
        if (characterController.RealVelocity.y <= -20f)
        {
            playerPlayables.healthV2.FallDamageValue = Mathf.Abs(characterController.RealVelocity.y) - 5f;
        }
    }

    private void OnLanding()
    {
        if (!characterController.IsGrounded) return;

        playerMovement.IsJumping = false;
        playerMovement.JumpImpulse = 0;

        if (playerPlayables.healthV2.FallDamageValue > 0)
            playerPlayables.healthV2.FallDamae();
    }

    private AnimationPlayable GetNextState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.JumpAttackStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        var interruptState = GetInterruptState();

        if (interruptState != null)
            return interruptState;

        if (elapsedTicks < finishStartTick)
            return null;

        return GetLandingState();
    }

    private AnimationPlayable GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        return null;
    }

    private AnimationPlayable GetLandingState()
    {
        // Stay in this state until we actually touch ground
        if (!characterController.IsGrounded)
        {
            groundedSinceTick = -1;
            return null;
        }

        if (groundedSinceTick < 0)
            groundedSinceTick = playerPlayables.Runner.Tick;

        int ticksGrounded = playerPlayables.Runner.Tick - groundedSinceTick;
        if (ticksGrounded < 2)
            return null; // Wait for stable ground contact

        // Clear attack flag on landing so FallingState won't re-trigger jump attack
        if (playerPlayables.HasStateAuthority)
        {
            playerMovement.Attacking = false;   // <-- KEY FIX
            playerMovement.IsJumping = false;
            playerMovement.JumpImpulse = 0;
        }

        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;

        if (isMoving)
        {
            return playerMovement.IsSprint
                ? playerPlayables.lowerBodyMovement.SprintPlayable
                : playerPlayables.lowerBodyMovement.RunPlayable;
        }

        return playerPlayables.lowerBodyMovement.IdlePlayable;
    }
}
