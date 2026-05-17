using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotRunPlayable : BotAnimationPlayable
{
    float randPlaceTrap;

    public BotRunPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
            // Transition immediately if loadout changed for this fight.
            if (botPlayables.Inventroy.WeaponIndex == 2)
            {
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    return botPlayables.BasicMovement.SwordRunPlayable;
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                    return botPlayables.BasicMovement.SpearRun;
            }
            else if (botPlayables.Inventroy.WeaponIndex == 3)
            {
                if (botPlayables.Inventroy.SecondaryWeapon != null && botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                    return botPlayables.BasicMovement.RifleRunPlayable;
                if (botPlayables.Inventroy.SecondaryWeapon != null && !botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                    return botPlayables.BasicMovement.BowRunPlayable;
            }

            if (botMovement.TryEvadeEnemyMeleeStrike(botMovement.FistMeleeEvadeRadius))
                return null;

            botMovement.MoveToTarget();

            if (botMovement.CanPunch())
            {
                if (botPlayables.Inventroy.WeaponIndex == 1)
                {
                    if (!botMovement.CanInitiateMeleeAttack())
                    {
                        botMovement.FaceTarget();
                        botMovement.ApplyStrafe();
                        return null;
                    }

                    botMovement.RegisterMeleeAttackCommitted();
                    return botPlayables.BasicMovement.FistFirstPunch;
                }
                else if (botPlayables.Inventroy.WeaponIndex == 2)
                {
                    if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    {
                        if (!botMovement.CanInitiateMeleeAttack())
                        {
                            botMovement.FaceTarget();
                            botMovement.ApplyStrafe();
                            return null;
                        }

                        botMovement.RegisterMeleeAttackCommitted();
                        return botPlayables.BasicMovement.SwordAttackOnePlayable;
                    }
                }
            }
            else if (botPlayables.Inventroy.WeaponIndex == 3)
            {
                if (botPlayables.Inventroy.SecondaryWeapon != null && botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                    return botPlayables.BasicMovement.RifleRunPlayable;
                if (botPlayables.Inventroy.SecondaryWeapon != null && !botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                    return botPlayables.BasicMovement.BowRunPlayable;
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

                if (botPlayables.Inventroy.WeaponIndex == 1)
                    return botPlayables.BasicMovement.IdlePlayable;
                else if (botPlayables.Inventroy.WeaponIndex == 2)
                {
                    if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                        return botPlayables.BasicMovement.SwordIdlePlayable;
                }
                else if (botPlayables.Inventroy.WeaponIndex == 3)
                {
                    if (botPlayables.Inventroy.SecondaryWeapon != null && botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                        return botPlayables.BasicMovement.RifleIdlePlayable;
                    else if (botPlayables.Inventroy.SecondaryWeapon != null && !botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                        return botPlayables.BasicMovement.BowIdlePlayable;
                }
            }
        }

        return null;
}
}


