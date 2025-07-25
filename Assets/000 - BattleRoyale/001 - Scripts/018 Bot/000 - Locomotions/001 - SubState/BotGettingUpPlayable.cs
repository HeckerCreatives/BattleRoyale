using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotGettingUpPlayable : BotAnimationPlayable
{
    float timer;
    bool canAction;

    public BotGettingUpPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        botPlayables.GetBotData.IsGettingUp = true;

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
        //if (playerPlayables.healthV2.IsDead)
        //    playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        //if (!characterController.IsGrounded)
        //{
        //    playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
        //    return;
        //}

        if (canAction)
        {
            if (botPlayables.TickRateAnimation >= timer)
            {
                botPlayables.GetBotData.IsGettingUp = false;

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

                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.IdlePlayable);
            }
        }
    }
}
