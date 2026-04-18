using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperSpearFirstAttack : UpperNoAimState
{
    bool hasResetHitEnemies;

    public PlayerUpperSpearFirstAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        hasResetHitEnemies = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        double animTime = animationClipPlayable.GetTime();
        double normalizedTime = animTime / animationLength;

        HandleDamageWindow(normalizedTime);

        var nextState = GetNextState(normalizedTime);

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void HandleDamageWindow(double normalizedTime)
    {
        if (!playerPlayables.HasStateAuthority) return;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - playerMovement.SwordStartTick;

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int startStartTick = Mathf.CeilToInt(totalPunchTicks * 0.5f);
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (elapsedTicks >= startStartTick && elapsedTicks <= finishStartTick)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.inventory.PrimaryWeapon.ClearHitEnemies();
                hasResetHitEnemies = true;
            }

            playerPlayables.inventory.PrimaryWeapon.DamagePlayer();
        }
    }

    private UpperBodyAnimations GetNextState(double normalizedTime)
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsJumping)
            return playerPlayables.upperBodyMovement.JumpPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsSprint && (playerMovement.XMovement != 0f || playerMovement.YMovement != 0f))
            return playerPlayables.upperBodyMovement.SpearSprintPlayable;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.SwordStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int startStartTick = Mathf.CeilToInt(totalPunchTicks * 0.8f);
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        if (elapsedTicks >= startStartTick)
        {
            if (playerMovement.Attacking)
                return playerPlayables.upperBodyMovement.SpearFinalAttackPlayable;
        }

        if (elapsedTicks >= finishStartTick)
            return playerPlayables.upperBodyMovement.SpearIdle;

        return null;
    }
}
