using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotHitPlayable : BotAnimationPlayable
{
    private float timer;
    private bool canAction;

    public BotHitPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay)
        : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();
        timer = botPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();
        canAction = false;
        botPlayables.GetBotData.HitKnockbackDir = Vector3.zero;
    }

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        if (!botController.IsGrounded)
            return botPlayables.BasicMovement.FallingPlayable;

        if (botPlayables.GetBotData.IsDead)
            return botPlayables.BasicMovement.DeathPlayable;

        if (!canAction || botPlayables.TickRateAnimation < timer)
        {
            Vector3 kb = botPlayables.GetBotData.HitKnockbackDir;
            if (kb.sqrMagnitude > 0.001f)
                botController.Move(kb * 12f * botPlayables.Runner.DeltaTime, 0f);
            return null;
        }

        botPlayables.GetBotData.IsHit = false;

        if (botPlayables.GetBotData.IsStagger)
            return botPlayables.BasicMovement.StaggerPlayable;

        if (botPlayables.GetBotData.IsGettingUp)
            return botPlayables.BasicMovement.GettingUpPlayable;

        if (botPlayables.Inventroy.WeaponIndex == 1)
            return botPlayables.BasicMovement.RunPlayable;

        if (botPlayables.Inventroy.WeaponIndex == 2)
        {
            if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                return botPlayables.BasicMovement.SwordRunPlayable;
            if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                return botPlayables.BasicMovement.SpearRun;
        }

        if (botPlayables.Inventroy.WeaponIndex == 3)
        {
            if (botPlayables.Inventroy.GetSecondaryWeaponID() == "003")
                return botPlayables.BasicMovement.RifleRunPlayable;
            if (botPlayables.Inventroy.GetSecondaryWeaponID() == "004")
                return botPlayables.BasicMovement.BowRunPlayable;
        }

        return botPlayables.BasicMovement.RunPlayable;
    }
}
