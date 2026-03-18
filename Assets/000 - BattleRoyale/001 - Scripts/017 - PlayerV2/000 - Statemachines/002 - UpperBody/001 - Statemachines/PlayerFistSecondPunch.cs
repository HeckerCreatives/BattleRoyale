using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerFistSecondPunch : UpperNoAimState
{
    bool hasResetHitEnemies;

    public PlayerFistSecondPunch(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.fistSoundController.PlayAttackOne();
            return;
        }

        hasResetHitEnemies = false;
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

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            playerPlayables.SetPunchRotation(1f);
        else
            playerPlayables.SetPunchRotation(0f);

        double animTime = animationClipPlayable.GetTime();
        double normalizedTime = animTime / animationLength;

        HandleDamageWindow(normalizedTime);

        var nextState = GetNextState(normalizedTime);

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private void HandleDamageWindow(double normalizedTime)
    {
        // Original:
        // damageWindowStart = TickRateAnimation + 0.22f;
        // damageWindowEnd   = TickRateAnimation + 0.27f;
        //
        // Since these were absolute seconds, convert to normalized time.
        double start = 0.22 / animationLength;
        double end = 0.27 / animationLength;

        if (normalizedTime >= start && normalizedTime <= end)
        {
            if (!hasResetHitEnemies)
            {
                playerPlayables.upperBodyMovement.ResetSecondAttack();
                hasResetHitEnemies = true;
            }

            playerPlayables.upperBodyMovement.PerformSecondAttack();
        }
    }

    private UpperBodyAnimations GetNextState(double normalizedTime)
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.upperBodyMovement.DeathPlayable;

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.upperBodyMovement.StaggerHitPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.upperBodyMovement.FallingPlayables;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.upperBodyMovement.RollPlayables;

        if (playerMovement.IsSprint && (playerMovement.XMovement != 0f || playerMovement.YMovement != 0f))
            return playerPlayables.upperBodyMovement.SprintPlayables;

        // Original:
        // nextPunchWindow = TickRateAnimation + (animationLength - 0.2f);
        //
        // Meaning combo window opens 0.2 seconds before end.
        double comboWindowStart = (animationLength - 0.2) / animationLength;

        if (normalizedTime >= comboWindowStart && playerMovement.Attacking)
        {
            playerPlayables.FinalAttack = true;
            return playerPlayables.upperBodyMovement.FinalPunch;
        }

        if (normalizedTime >= 0.9)
            return playerPlayables.upperBodyMovement.IdlePlayables;

        return null;
    }
}
