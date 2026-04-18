using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperSwordJumpAttack : UpperNoAimState
{
    float timer;
    bool canAction;
    bool hasResetHitEnemies;

    public PlayerUpperSwordJumpAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority) return;

        hasResetHitEnemies = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        HandleDamage(animationClipPlayable.GetTime());

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private void HandleDamage(double animTime)
    {
        if (!playerPlayables.HasStateAuthority) return;

        if (!hasResetHitEnemies)
        {
            playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
            hasResetHitEnemies = true;
        }

        playerPlayables.inventory.PrimaryWeapon.DamagePlayer();
    }

    private UpperBodyAnimations GetNextState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.JumpAttackStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 1f);

        if (!characterController.IsGrounded || elapsedTicks < finishStartTick)
            return null;

        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;

        if (isMoving)
        {
            return playerMovement.IsSprint
                ? playerPlayables.upperBodyMovement.SwordSprint
                : playerPlayables.upperBodyMovement.SwordRunPlayable;
        }

        return playerPlayables.upperBodyMovement.SwordIdlePlayable;
    }
}
