using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotRifleCockingPlayable : BotAnimationPlayable
{
    private float finishTimer;
    private bool canAction;

    public BotRifleCockingPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();
        finishTimer = botPlayables.TickRateAnimation + (animationLength * 0.9f);
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
            return botPlayables.BasicMovement.FallingPlayable;
        if (botPlayables.GetBotData.IsHit)
            return botPlayables.BasicMovement.HitPlayable;
        if (botPlayables.GetBotData.IsStagger)
            return botPlayables.BasicMovement.StaggerPlayable;
        if (botPlayables.GetBotData.IsDead)
            return botPlayables.BasicMovement.DeathPlayable;
        if (botPlayables.Inventroy.RifleMagazine <= 0)
            return botPlayables.BasicMovement.RifleReloadPlayable;
        if (botMovement.detectedTarget == null)
            return botPlayables.BasicMovement.RifleRunPlayable;

        botMovement.FaceTarget();
        botMovement.StopMovement();

        if (canAction && botPlayables.TickRateAnimation >= finishTimer)
        {
            if (botMovement.detectedTarget != null && botMovement.IsTargetTooClose())
                return botPlayables.BasicMovement.RifleRunPlayable;
            return botPlayables.BasicMovement.RifleShootPlayable;
        }

        return null;
    }
}
