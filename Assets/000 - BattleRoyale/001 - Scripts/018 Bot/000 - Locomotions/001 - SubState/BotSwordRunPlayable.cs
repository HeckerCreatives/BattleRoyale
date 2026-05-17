using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotSwordRunPlayable : BotAnimationPlayable
{
    float randPlaceTrap;

    public BotSwordRunPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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

            if (botMovement.TryEvadeEnemyMeleeStrike(botMovement.SwordMeleeEvadeRadius))
                return null;

            botMovement.MoveToTarget();

            if (botMovement.CanSwordAttack())
            {
                if (!botMovement.CanInitiateMeleeAttack())
                {
                    // Cooldown not ready yet: don't freeze in-place while facing the opponent.
                    botMovement.FaceTarget();
                    botMovement.ApplyStrafePublic();
                    return null;
                }

                botMovement.RegisterMeleeAttackCommitted();
                return botPlayables.BasicMovement.SwordAttackOnePlayable;
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

                return botPlayables.BasicMovement.SwordIdlePlayable;
            }
        }

        return null;
}
}


