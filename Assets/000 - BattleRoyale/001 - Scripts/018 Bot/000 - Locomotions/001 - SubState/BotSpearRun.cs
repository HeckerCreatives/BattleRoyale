using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotSpearRun : BotAnimationPlayable
{
    float randPlaceTrap;

    public BotSpearRun(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        randPlaceTrap = Random.Range(0, 101);
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        if (botPlayables.GetBotData.IsDead)
        {
            return botPlayables.BasicMovement.DeathPlayable;
        }

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

        if (botPlayables.Inventroy.TrapCount > 0 && randPlaceTrap <= 20)
        {
            return botPlayables.BasicMovement.TrapPlayable;
        }

        return MoveBot();
}

    private BotAnimationPlayable MoveBot()
    {
        botMovement.DetectTarget();
        botMovement.EvaluateCombatLoadout();

        if (botMovement.detectedTarget != null)
        {
            if (botPlayables.Inventroy.WeaponIndex == 3)
            {
                if (botPlayables.Inventroy.GetSecondaryWeaponID() == "003")
                    return botPlayables.BasicMovement.RifleRunPlayable;
                if (botPlayables.Inventroy.GetSecondaryWeaponID() == "004")
                    return botPlayables.BasicMovement.BowRunPlayable;
            }

            if (botMovement.TryEvadeEnemyMeleeStrike(botMovement.SpearMeleeEvadeRadius))
                return null;

            botMovement.MoveToTarget();

            if (botMovement.CanSpearAttack())
            {
                if (!botMovement.CanInitiateMeleeAttack())
                {
                    // Cooldown not ready yet: avoid stare-off by orbiting instead of holding.
                    botMovement.FaceTarget();
                    botMovement.ApplyStrafePublic();
                    return null;
                }

                botMovement.RegisterMeleeAttackCommitted();
                return botPlayables.BasicMovement.SpearAttackOne;
            }
        }
        else
        {
            if (botMovement.TryRunLootBehaviour())
                return null;

            botMovement.MoveInDirection();

            if (botMovement.WanderTimer.Expired(botMovement.Runner))
            {
                botMovement.IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));

                return botPlayables.BasicMovement.SpearIdle;
            }
        }

        return null;
}
}


