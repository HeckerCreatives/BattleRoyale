using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotFistMiddlePunch : BotAnimationPlayable
{
    float timer;
    float nextPunchWindow;
    float moveTimer;
    float stopMoveTimer;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool hasResetHitEnemies;
    float nextPuncDelay;

    public BotFistMiddlePunch(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        hasResetHitEnemies = false;
        timer = botPlayables.TickRateAnimation + (animationLength * 0.9f);
        nextPunchWindow = botPlayables.TickRateAnimation + (animationLength * 0.8f);
        moveTimer = botPlayables.TickRateAnimation + (animationLength * 0.3f);
        stopMoveTimer = botPlayables.TickRateAnimation + (animationLength * 0.18f);
        damageWindowEnd = botPlayables.TickRateAnimation + (animationLength * 0.23f);
        nextPuncDelay = timer + 0.05f;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }

    public override void NetworkUpdate()
    {
        if (botPlayables.TickRateAnimation >= damageWindowStart && botPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!hasResetHitEnemies)
            {
                botPlayables.GetBotData.ResetSecondAttack(); // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }

            botPlayables.GetBotData.PerformSecondAttack();
        }

        CheckAnimations();
    }

    private void CheckAnimations()
    {
        if (!botController.IsGrounded)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.FallingPlayable);
            return;
        }

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

        if (botPlayables.GetBotData.IsStagger)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.StaggerPlayable);
            return;
        }

        if (botPlayables.TickRateAnimation >= timer && canAction)
        {
            if (botMovement.CanPunch())
            {
                if (botPlayables.TickRateAnimation >= nextPuncDelay)
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.FistLastPunch);

                //botPlayablesChanger.ChangeState(botPlayables.BasicMovement.FistLastPunch);

                return;
            }

            botMovement.PickNewWanderDirection();
            botMovement.IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.IdlePlayable);
        }
    }
}
