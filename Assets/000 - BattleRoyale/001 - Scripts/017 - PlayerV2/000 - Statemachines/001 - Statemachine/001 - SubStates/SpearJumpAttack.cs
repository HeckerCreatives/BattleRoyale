using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SpearJumpAttack : AnimationPlayable
{
    float timer;
    bool canAction;

    public SpearJumpAttack(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority) return;

        playerPlayables.healthV2.FallDamageValue = 0;
        playerMovement.IsJumping = false;
    }

    public override void Exit()
    {
        base.Exit();

        playerMovement.JumpImpulse = 0;
    }

    public override void NetworkUpdate()
    {

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            OnLanding();
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
        {
            FallDamage();

            playerPlayables.stamina.RecoverStamina(5f);
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
        var interruptState = GetInterruptState();

        if (interruptState != null)
            return interruptState;

        if (animationClipPlayable.GetTime() < animationLength * 0.9f) return null;

        return GetLandingState();
    }

    private AnimationPlayable GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.lowerBodyMovement.StaggerHitPlayable;

        return null;
    }

    private AnimationPlayable GetLandingState()
    {
        if (!characterController.IsGrounded)
            return null;

        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;

        if (isMoving)
        {
            return playerMovement.IsSprint
                ? playerPlayables.lowerBodyMovement.SpearSprintPlayable
                : playerPlayables.lowerBodyMovement.SpearRunPlayable;
        }

        return playerPlayables.lowerBodyMovement.SpearIdlePlayable;
    }
}
