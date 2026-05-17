using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotRifleRunPlayable : BotAnimationPlayable
{
    public BotRifleRunPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
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

        if (botMovement.IsOutsideSafeZone() && MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA)
        {
            botMovement.NavigateToSafeZone();
            return null;
        }

        return MoveBot();
}

    private BotAnimationPlayable MoveBot()
    {
        botMovement.DetectTarget();
        botMovement.EvaluateCombatLoadout();

        // Wrong weapon currently equipped — route to the correct sibling state.
        if (botPlayables.Inventroy.WeaponIndex == 3 && botPlayables.Inventroy.GetSecondaryWeaponID() == "004")
            return botPlayables.BasicMovement.BowRunPlayable;
        if (botPlayables.Inventroy.WeaponIndex != 3 || botPlayables.Inventroy.GetSecondaryWeaponID() != "003")
            return botPlayables.BasicMovement.RunPlayable;

        if (botMovement.detectedTarget != null)
        {
            if (botPlayables.Inventroy.WeaponIndex == 2)
            {
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    return botPlayables.BasicMovement.SwordRunPlayable;
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                    return botPlayables.BasicMovement.SpearRun;
            }

            if (!botMovement.HasRifleAmmo())
            {
                return botPlayables.BasicMovement.RifleReloadPlayable;
            }

            if (botMovement.CanRifleShoot())
            {
                return botPlayables.BasicMovement.RifleAimPlayable;
            }

            botMovement.MaintainRangedSpacing();
        }
        else
        {
            if (botMovement.TryRunLootBehaviour())
                return null;

            botMovement.MoveInDirection();

            if (botMovement.WanderTimer.Expired(botMovement.Runner))
            {
                botMovement.IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
                return botPlayables.BasicMovement.RifleIdlePlayable;
            }
        }

        return null;
}
}



