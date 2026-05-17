using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotSpearAttackThree : BotAnimationPlayable
{
    float timer;
    float nextPunchWindow;
    float moveTimer;
    float stopMoveTimer;
    float damageWindowStart;
    float damageWindowEnd;
    bool canAction;
    bool hasResetHitEnemies;

    public BotSpearAttackThree(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        botPlayables.SlashSpearParticles(0);
        hasResetHitEnemies = false;
        timer = botPlayables.TickRateAnimation + animationLength;
        moveTimer = botPlayables.TickRateAnimation + 0.30f;
        stopMoveTimer = botPlayables.TickRateAnimation + 0.60f;
        damageWindowStart = botPlayables.TickRateAnimation + 0.2f;
        damageWindowEnd = botPlayables.TickRateAnimation + 0.8f;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        botPlayables.SlashSpearParticlesStop(0);
        canAction = false;
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        if (botPlayables.TickRateAnimation >= damageWindowStart && botPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!hasResetHitEnemies)
            {
                botPlayables.Inventroy.PrimaryWeapon.ClearHitEnemies(); // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }
            if (botPlayables.Inventroy.PrimaryWeapon == null) return null;
            botPlayables.Inventroy.PrimaryWeapon.DamagePlayer(true, true);
        }

        if (botPlayables.TickRateAnimation >= moveTimer && botPlayables.TickRateAnimation <= stopMoveTimer)
        {
            botMovement.TryLungeForward(1.25f);
        }

        return CheckAnimations();
}

    private BotAnimationPlayable CheckAnimations()
    {
        if (!botController.IsGrounded)
        {
            return botPlayables.BasicMovement.FallingPlayable;
        }

        if (botPlayables.GetBotData.IsDead)
        {
            return botPlayables.BasicMovement.DeathPlayable;
        }

        if (botPlayables.GetBotData.IsHit)
        {
            return botPlayables.BasicMovement.HitPlayable;
        }

        if (botPlayables.GetBotData.IsStagger)
        {
            return botPlayables.BasicMovement.StaggerPlayable;
        }

        if (botPlayables.TickRateAnimation >= timer && canAction)
        {
            botMovement.PickNewWanderDirection();
            botMovement.WanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
            return botPlayables.BasicMovement.SpearRun;
        }

        return null;
}
}




