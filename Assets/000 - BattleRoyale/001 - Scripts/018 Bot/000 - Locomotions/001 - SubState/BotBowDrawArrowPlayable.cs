using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotBowDrawArrowPlayable : BotAnimationPlayable
{
    float finishTimer;
    bool canAction;

    public BotBowDrawArrowPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        finishTimer = botPlayables.TickRateAnimation + (animationLength * 0.9f);
        canAction = true;

        // Bow string: bend toward the pulling hand on every peer.
        botPlayables.Inventroy.SecondaryWeapon?.SetDrawn(true);
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;

        // Bow string returns to rest. Runs on every peer.
        botPlayables.Inventroy.SecondaryWeapon?.SetDrawn(false);
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        if (!botController.IsGrounded)
        {
            return botPlayables.BasicMovement.FallingPlayable;
        }

        if (botPlayables.GetBotData.IsHit)
        {
            return botPlayables.BasicMovement.HitPlayable;
        }

        if (botPlayables.GetBotData.IsStagger)
        {
            return botPlayables.BasicMovement.StaggerPlayable;
        }

        if (botPlayables.GetBotData.IsDead)
        {
            return botPlayables.BasicMovement.DeathPlayable;
        }

        if (!botMovement.HasBowAmmo())
        {
            return botPlayables.BasicMovement.BowIdlePlayable;
        }

        if (botMovement.detectedTarget == null)
        {
            return botPlayables.BasicMovement.BowRunPlayable;
        }

        botMovement.FaceTarget();
        botMovement.StopMovement();

        if (canAction && botPlayables.TickRateAnimation >= finishTimer)
        {
            if (botMovement.IsTargetTooClose())
                return botPlayables.BasicMovement.BowRunPlayable;
            return botPlayables.BasicMovement.BowChargePlayable;
        }

        return null;
}
}

