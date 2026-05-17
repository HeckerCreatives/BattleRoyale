using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotSwordAttackTwo : BotAnimationPlayable
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

    public BotSwordAttackTwo(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        botPlayables.SlashSwordParticles(1);
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

        botPlayables.SlashSwordParticlesStop(1);
        canAction = false;
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        if (botPlayables.TickRateAnimation >= damageWindowStart && botPlayables.TickRateAnimation <= damageWindowEnd)
        {
            if (!hasResetHitEnemies)
            {
                botPlayables.Inventroy.PrimaryWeapon.ClearHitEnemies();  // Clear BEFORE performing attack
                hasResetHitEnemies = true;
            }

            if (botPlayables.Inventroy.PrimaryWeapon == null) return null;
            botPlayables.Inventroy.PrimaryWeapon.DamagePlayer(false, true);
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
            if (botPlayables.TickRateAnimation < nextPuncDelay)
                return null;

            if (botMovement.CanSwordAttack())
                return botPlayables.BasicMovement.SwordAttackThreePlayable;

            botMovement.PickNewWanderDirection();
            botMovement.IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
            return botPlayables.BasicMovement.SwordIdlePlayable;
        }

        return null;
}
}





