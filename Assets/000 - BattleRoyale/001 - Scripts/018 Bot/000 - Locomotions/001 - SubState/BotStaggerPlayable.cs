using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotStaggerPlayable : BotAnimationPlayable
{
    float timer;
    float moveTimer;
    bool canAction;

    public BotStaggerPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = botPlayables.TickRateAnimation + animationLength;
        moveTimer = botPlayables.TickRateAnimation + 0.8f;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }


    public override void NetworkUpdate()
    {
        if (canAction)
        {
            if (botPlayables.TickRateAnimation < moveTimer)
                botController.Move(botController.TransformDirection * -5f, 0f);
        }

        Animation();
    }

    private void Animation()
    {
        if (botPlayables.GetBotData.IsDead)
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.DeathPlayable);

        if (canAction)
        {
            if (botPlayables.TickRateAnimation >= timer)
            {
                botPlayables.GetBotData.IsStagger = false;

                if (!botController.IsGrounded)
                {
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.FallingPlayable);
                    return;
                }

                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.GettingUpPlayable);
            }
        }
    }
}
