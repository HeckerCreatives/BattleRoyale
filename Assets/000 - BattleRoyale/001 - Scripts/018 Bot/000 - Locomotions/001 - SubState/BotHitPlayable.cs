using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotHitPlayable : BotAnimationPlayable
{
    float timer;
    bool canAction;

    public BotHitPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        botPlayables.GetBotData.IsHit = false;
        timer = botPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }


    public override void NetworkUpdate()
    {
        Animation();
    }

    private void Animation()
    {
        if (botPlayables.GetBotData.IsDead)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.DeathPlayable);
            return;
        }

        if (botPlayables.GetBotData.IsHit)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.HitPlayable);
            return;
        }

        if (botPlayables.TickRateAnimation >= timer)
        {
            if (botPlayables.GetBotData.IsHit)
            {
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.HitPlayable);
                return;
            }

            if (botPlayables.GetBotData.IsStagger)
            {
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.StaggerPlayable);
                return;
            }

            ChangeDirection();
        }
    }

    private void ChangeDirection()
    {
        botMovement.PickNewWanderDirection();
        botMovement.WanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
        botPlayablesChanger.ChangeState(botPlayables.BasicMovement.RunPlayable);
    }
}
