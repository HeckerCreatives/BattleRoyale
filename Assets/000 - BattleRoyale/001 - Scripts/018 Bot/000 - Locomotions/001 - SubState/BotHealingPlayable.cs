using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotHealingPlayable : BotAnimationPlayable
{
    float healtimer;
    float timer;
    bool canAction;
    bool doneHeal;

    public BotHealingPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        healtimer = botPlayables.TickRateAnimation + (animationLength * 0.5f);
        timer = botPlayables.TickRateAnimation + animationLength;

        if (botPlayables.Inventroy.PrimaryWeapon != null && botPlayables.Inventroy.WeaponIndex == 2) botPlayables.Inventroy.PrimaryWeapon.IsEquipped = false;

        doneHeal = false;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        if (botPlayables.Inventroy.PrimaryWeapon != null && botPlayables.Inventroy.WeaponIndex == 2) botPlayables.Inventroy.PrimaryWeapon.IsEquipped = true;

        doneHeal = false;
        canAction = false;
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        if (!doneHeal && botPlayables.TickRateAnimation > healtimer)
        {
            botPlayables.GetBotData.HealHealth();
            doneHeal = true;
        }

        if (botPlayables.TickRateAnimation < healtimer)
        {
            return Animations();
            return MovePlayer();
        }

        if (canAction && botPlayables.TickRateAnimation >= timer)
        {

            return Animations();
            return MovePlayer();
        }

        return null;
}

    private BotAnimationPlayable Animations()
    {
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

        return null;
}

    private BotAnimationPlayable MovePlayer()
    {
        botMovement.DetectTarget();

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
                if (botPlayables.Inventroy.GetSecondaryWeaponID() == "003")
                    return botPlayables.BasicMovement.RifleRunPlayable;
                else if (botPlayables.Inventroy.GetSecondaryWeaponID() == "004")
                    return botPlayables.BasicMovement.BowRunPlayable;
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
                else if (botPlayables.Inventroy.WeaponIndex == 3)
                {
                    if (botPlayables.Inventroy.GetSecondaryWeaponID() == "003")
                        return botPlayables.BasicMovement.RifleRunPlayable;
                    else if (botPlayables.Inventroy.GetSecondaryWeaponID() == "004")
                        return botPlayables.BasicMovement.BowRunPlayable;
                }
            }
        }

        return null;
}
}



