using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotStaggerPlayable : BotAnimationPlayable
{
    private float timer;
    private bool canAction;

    public BotStaggerPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay)
        : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();
        timer = botPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();
        canAction = false;
        botPlayables.GetBotData.HitKnockbackDir = Vector3.zero;
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        if (botPlayables.GetBotData.IsDead)
            return botPlayables.BasicMovement.DeathPlayable;

        if (!canAction || botPlayables.TickRateAnimation < timer)
        {
            Vector3 kb = botPlayables.GetBotData.HitKnockbackDir;
            if (kb.sqrMagnitude > 0.001f)
                botController.Move(kb * 18f * botPlayables.Runner.DeltaTime, 0f);
            return null;
        }

        botPlayables.GetBotData.IsStagger = false;

        if (!botController.IsGrounded)
            return botPlayables.BasicMovement.FallingPlayable;

        return botPlayables.BasicMovement.GettingUpPlayable;
    }
}
