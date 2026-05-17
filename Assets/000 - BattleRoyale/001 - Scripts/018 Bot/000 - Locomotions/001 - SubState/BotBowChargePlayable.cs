using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotBowChargePlayable : BotAnimationPlayable
{
    float chargeTimer;
    bool shotFired;
    bool canAction;

    public BotBowChargePlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        chargeTimer = botPlayables.TickRateAnimation + Random.Range(0.15f, 0.5f);
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

        if (!botMovement.HasBowAmmo())
        {
            return botPlayables.BasicMovement.BowIdlePlayable;
        }

        if (botMovement.detectedTarget == null)
        {
            return botPlayables.BasicMovement.BowIdlePlayable;
        }

        botMovement.FaceTarget();
        botMovement.StopMovement();

        if (canAction && !shotFired && botPlayables.TickRateAnimation >= chargeTimer)
        {
            if (botPlayables.Inventroy.SecondaryWeapon != null)
            {
                botPlayables.GetBotData.FireArrow(
                    botPlayables.Inventroy.SecondaryWeapon.ImpactPoint,
                    botMovement.GetTargetAimPosition()
                );
            }

            var wepInv = botPlayables.Inventroy;
            if (wepInv.SecondaryWeapon != null && wepInv.SecondaryWeapon.Supplies > 0)
                wepInv.SecondaryWeapon.Supplies--;
            else
                wepInv.BowMagazine = Mathf.Max(0, wepInv.BowMagazine - 1);
            shotFired = true;
            return botPlayables.BasicMovement.BowShotPlayable;
        }

        return null;
    }
}

