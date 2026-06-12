using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotBowShotPlayable : BotAnimationPlayable
{
    float timer;
    bool canAction;

    public BotBowShotPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = botPlayables.TickRateAnimation + (animationLength * 0.9f);
        canAction = true;

        // Bow string: still bent toward the hand during the release frame.
        // (Exit flips it back to false when leaving bow aim.)
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

        botMovement.StopMovement();

        if (canAction && botPlayables.TickRateAnimation >= timer)
        {
            if (botMovement.detectedTarget != null && botPlayables.Inventroy.BowMagazine > 0 && botMovement.IsInRangedRange())
            {
                return botPlayables.BasicMovement.BowDrawArrowPlayable;
            }

            return botPlayables.BasicMovement.BowRunPlayable;
        }

        return null;
}
}

