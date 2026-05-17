using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotRifleShootPlayable : BotAnimationPlayable
{
    float timer;
    bool canAction;

    public BotRifleShootPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = botPlayables.TickRateAnimation + (animationLength * 0.9f);
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
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
            if (botPlayables.Inventroy.RifleMagazine <= 0)
            {
                return botPlayables.BasicMovement.RifleReloadPlayable;
            }

            if (botMovement.detectedTarget != null && botMovement.IsInRangedRange())
            {
                return botPlayables.BasicMovement.RifleAimPlayable;
            }

            return botPlayables.BasicMovement.RifleRunPlayable;
        }

        return null;
}
}

