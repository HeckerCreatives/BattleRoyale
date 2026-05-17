using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotRifleReloadPlayable : BotAnimationPlayable
{
    float reloadApplyTimer;
    float finishTimer;
    bool doneReload;
    bool canAction;

    public BotRifleReloadPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        reloadApplyTimer = botPlayables.TickRateAnimation + (animationLength * 0.7f);
        finishTimer = botPlayables.TickRateAnimation + (animationLength * 0.95f);
        doneReload = false;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
        doneReload = false;
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

        // Apply ammo at 70% through animation
        if (!doneReload && botPlayables.TickRateAnimation >= reloadApplyTimer)
        {
            botPlayables.Inventroy.RifleMagazine = 30;
            doneReload = true;
        }

        if (canAction && botPlayables.TickRateAnimation >= finishTimer)
        {
            if (botMovement.detectedTarget != null && botMovement.IsTargetTooClose())
                return botPlayables.BasicMovement.RifleRunPlayable;
            return botPlayables.BasicMovement.RifleIdlePlayable;
        }

        return null;
}
}

