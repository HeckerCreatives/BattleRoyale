using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotBowIdlePlayable : BotAnimationPlayable
{
    public BotBowIdlePlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
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

        if (botPlayables.GetBotData.CurrentHealth < 100 && botPlayables.GetBotData.Inventory.HealCount > 0)
        {
            return botPlayables.BasicMovement.HealingPlayable;
        }

        if (botPlayables.GetBotData.Inventory.Armor != null && botPlayables.GetBotData.Inventory.Armor.Supplies < 100 && botPlayables.GetBotData.Inventory.RepairCount > 0)
        {
            return botPlayables.BasicMovement.RepairArmorPlayable;
        }

        if (botMovement.IsOutsideSafeZone() && MultiplayerServerManager.Instance.CurrentGameState == GameState.ARENA)
        {
            return botPlayables.BasicMovement.BowRunPlayable;
        }

        return MovePlayer();
}

    private BotAnimationPlayable MovePlayer()
    {
        botMovement.DetectTarget();

        // Wrong weapon currently equipped — route to the correct sibling state.
        if (botPlayables.Inventroy.WeaponIndex == 3 && botPlayables.Inventroy.GetSecondaryWeaponID() == "003")
            return botPlayables.BasicMovement.RifleIdlePlayable;
        if (botPlayables.Inventroy.WeaponIndex != 3 || botPlayables.Inventroy.GetSecondaryWeaponID() != "004")
            return botPlayables.BasicMovement.IdlePlayable;

        if (botMovement.detectedTarget != null)
        {
            return botPlayables.BasicMovement.BowRunPlayable;
        }
        else
        {
            botController.Move(Vector3.zero, 0f);

            if (botMovement.ShouldScanLoot())
            {
                botMovement.ResetLootScanTimer();
                botMovement.ScanForLoot();
            }

            if (botMovement.IdleBeforeWanderTimer.Expired(botMovement.Runner))
            {
                botMovement.WanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
                return botPlayables.BasicMovement.BowRunPlayable;
            }
        }

        return null;
}
}


