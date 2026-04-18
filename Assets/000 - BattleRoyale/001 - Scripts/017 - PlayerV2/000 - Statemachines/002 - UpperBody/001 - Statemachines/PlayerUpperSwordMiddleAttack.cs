using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerUpperSwordMiddleAttack : UpperNoAimState
{
    bool hasResetHitEnemies;

    public PlayerUpperSwordMiddleAttack(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
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

        HandleDamageWindow();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void HandleDamageWindow()
    {
        if (!playerPlayables.HasStateAuthority) return;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - playerMovement.SwordStartTick;

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int startStartTick = Mathf.CeilToInt(totalPunchTicks * 0.18f);
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

    private UpperBodyAnimations GetNextState()
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
            return playerPlayables.upperBodyMovement.SwordSprint;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.SwordStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int startStartTick = Mathf.CeilToInt(totalPunchTicks * 0.8f);
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        // combo window opens at 80%
        if (elapsedTicks >= startStartTick && playerMovement.Attacking)
        {
            playerPlayables.FinalAttack = true;
            return playerPlayables.upperBodyMovement.SwordFinalAttackPlayable;
        }

        if (elapsedTicks >= finishStartTick)
            return playerPlayables.upperBodyMovement.SwordIdlePlayable;

        return null;
    }
}
