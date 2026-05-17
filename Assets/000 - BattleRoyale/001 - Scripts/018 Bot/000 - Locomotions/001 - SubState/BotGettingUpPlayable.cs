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

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        return Animation();
}

    private BotAnimationPlayable Animation()
    {
        //if (playerPlayables.healthV2.IsDead)
        //    playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        //if (!characterController.IsGrounded)
        //{
        //    playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
        //    return null;
        //}

        if (canAction)
        {
            if (botPlayables.TickRateAnimation >= timer)
            {
                botPlayables.GetBotData.IsGettingUp = false;

                if (botPlayables.GetBotData.IsHit)
                {
                    return botPlayables.BasicMovement.HitPlayable;
                }

                if (botPlayables.GetBotData.IsStagger)
                {
                    return botPlayables.BasicMovement.StaggerPlayable;
                }

                return ChangeDirection();
            }
        }

        return null;
}

    private BotAnimationPlayable ChangeDirection()
    {
        botMovement.PickNewWanderDirection();

        if (botPlayables.Inventroy.WeaponIndex == 1)
            return botPlayables.BasicMovement.RunPlayable;
        else if (botPlayables.Inventroy.WeaponIndex == 2)
        {
            if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                return botPlayables.BasicMovement.SwordRunPlayable;
            else if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                return botPlayables.BasicMovement.SpearRun;
        }

        return null;
}
}




