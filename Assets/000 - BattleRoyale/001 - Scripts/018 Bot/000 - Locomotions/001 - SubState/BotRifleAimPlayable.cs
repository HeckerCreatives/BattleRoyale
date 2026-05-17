using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotRifleAimPlayable : BotAnimationPlayable
{
    float fireTimer;
    bool shotFired;
    bool canAction;

    public BotRifleAimPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        fireTimer = botPlayables.TickRateAnimation + Random.Range(0.15f, 0.5f);
        shotFired = false;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
        shotFired = false;
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

        if (!botMovement.HasRifleAmmo())
        {
            return botPlayables.BasicMovement.RifleReloadPlayable;
        }

        if (botMovement.detectedTarget == null)
        {
            return botPlayables.BasicMovement.RifleRunPlayable;
        }

        botMovement.EvaluateCombatLoadout();
        if (botPlayables.Inventroy.WeaponIndex != 3 || botPlayables.Inventroy.GetSecondaryWeaponID() != "003")
        {
            if (botPlayables.Inventroy.WeaponIndex == 3 && botPlayables.Inventroy.GetSecondaryWeaponID() == "004")
                return botPlayables.BasicMovement.BowRunPlayable;
            if (botPlayables.Inventroy.WeaponIndex == 2)
            {
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    return botPlayables.BasicMovement.SwordRunPlayable;
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                    return botPlayables.BasicMovement.SpearRun;
            }
            return botPlayables.BasicMovement.RunPlayable;
        }

        botMovement.FaceTarget();
        botMovement.StopMovement();

        if (canAction && !shotFired && botPlayables.TickRateAnimation >= fireTimer)
        {
            if (botPlayables.Inventroy.SecondaryWeapon != null)
            {
                botPlayables.GetBotData.FireBullet(
                    botPlayables.Inventroy.SecondaryWeapon.ImpactPoint,
                    botMovement.GetTargetAimPosition()
                );
            }

            var wepInv = botPlayables.Inventroy;
            if (wepInv.SecondaryWeapon != null && wepInv.SecondaryWeapon.Supplies > 0)
                wepInv.SecondaryWeapon.Supplies--;
            else
                wepInv.RifleMagazine = Mathf.Max(0, wepInv.RifleMagazine - 1);
            shotFired = true;
            return botPlayables.BasicMovement.RifleCockingPlayable;
        }

        return null;
    }
}

