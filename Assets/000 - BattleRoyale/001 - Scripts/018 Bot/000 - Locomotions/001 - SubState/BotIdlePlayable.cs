using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotIdlePlayable : BotAnimationPlayable
{
    public BotIdlePlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        botMovement.PickNewWanderDirection();
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


        if (botPlayables.GetBotData.Inventory.HealCount > 0 && botPlayables.GetBotData.CurrentHealth < 100)
        {
            return botPlayables.BasicMovement.HealingPlayable;
        }

        if (botPlayables.GetBotData.Inventory.RepairCount > 0 && botPlayables.GetBotData.Inventory.Armor != null)
        {
            if (botPlayables.GetBotData.Inventory.Armor.Supplies < 100)
            {
                return botPlayables.BasicMovement.RepairArmorPlayable;
            }
        }

        return MovePlayer();
}

    private BotAnimationPlayable MovePlayer()
    {
        botMovement.DetectTarget();
        botMovement.EvaluateCombatLoadout();

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
            else if (botPlayables.Inventroy.WeaponIndex == 3)
            {
                if (botPlayables.Inventroy.SecondaryWeapon != null && botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                    return botPlayables.BasicMovement.RifleRunPlayable;
                else if (botPlayables.Inventroy.SecondaryWeapon != null && !botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                    return botPlayables.BasicMovement.BowRunPlayable;
            }
        }
        else
        {
            if (botMovement.TryRunLootBehaviour())
            {
                if (botPlayables.Inventroy.WeaponIndex == 1)
                    return botPlayables.BasicMovement.RunPlayable;
                if (botPlayables.Inventroy.WeaponIndex == 2)
                    return botPlayables.Inventroy.GetPrimaryWeaponID() == "001"
                        ? botPlayables.BasicMovement.SwordRunPlayable
                        : botPlayables.BasicMovement.SpearRun;
                if (botPlayables.Inventroy.WeaponIndex == 3)
                    return botPlayables.Inventroy.SecondaryWeapon != null && botPlayables.Inventroy.SecondaryWeapon.IsRifle
                        ? botPlayables.BasicMovement.RifleRunPlayable
                        : botPlayables.BasicMovement.BowRunPlayable;
            }

            if (botMovement.TryStrategicIdleBehaviour())
            {
                botController.Move(Vector3.zero, 0f);
                return null;
            }

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
                else if (botPlayables.Inventroy.WeaponIndex == 3)
                {
                    if (botPlayables.Inventroy.SecondaryWeapon != null && botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                        return botPlayables.BasicMovement.RifleRunPlayable;
                    else if (botPlayables.Inventroy.SecondaryWeapon != null && !botPlayables.Inventroy.SecondaryWeapon.IsRifle)
                        return botPlayables.BasicMovement.BowRunPlayable;
                }
            }
        }

        return null;
}
}


