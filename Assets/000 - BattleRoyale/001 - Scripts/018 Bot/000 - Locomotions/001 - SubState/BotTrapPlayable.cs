using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotTrapPlayable : BotAnimationPlayable
{
    float timer;
    bool canAction;

    public BotTrapPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = botPlayables.TickRateAnimation + animationLength;
        canAction = true;

        botPlayables.GetBotData.SpawnTrap();
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

        if (botPlayables.TickRateAnimation >= timer && canAction)
        {
            return MovePlayer();
        }

        return null;
}

    private BotAnimationPlayable MovePlayer()
    {
        if (botMovement.detectedTarget != null)
        {
            if (botPlayables.Inventroy.WeaponIndex == 1)
                return botPlayables.BasicMovement.RunPlayable;
            else if (botPlayables.Inventroy.WeaponIndex == 2)
            {
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    return botPlayables.BasicMovement.SwordRunPlayable;
                else if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                    return botPlayables.BasicMovement.SpearRun;
            }
        }
        else
        {
            botController.Move(Vector3.zero, 0f);

            if (botMovement.IdleBeforeWanderTimer.Expired(botMovement.Runner))
            {
                botMovement.WanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
                if (botPlayables.Inventroy.WeaponIndex == 1)
                    return botPlayables.BasicMovement.RunPlayable;
                else if (botPlayables.Inventroy.WeaponIndex == 2)
                {
                    if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                        return botPlayables.BasicMovement.SwordRunPlayable;
                    else if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                        return botPlayables.BasicMovement.SpearRun;
                }
            }
        }

        return null;
}
}


