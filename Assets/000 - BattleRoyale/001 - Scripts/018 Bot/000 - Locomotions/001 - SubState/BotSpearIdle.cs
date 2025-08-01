using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotSpearIdle : BotAnimationPlayable
{
    public BotSpearIdle(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        if (!botController.IsGrounded)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.FallingPlayable);
            return;
        }

        if (botPlayables.GetBotData.IsHit)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.HitPlayable);
            return;
        }

        if (botPlayables.GetBotData.IsStagger)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.StaggerPlayable);
            return;
        }

        if (botPlayables.GetBotData.IsDead)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.DeathPlayable);
            return;
        }


        if (botPlayables.GetBotData.Inventory.HealCount > 0 && botPlayables.GetBotData.CurrentHealth < 100)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.HealingPlayable);
            return;
        }

        if (botPlayables.GetBotData.Inventory.RepairCount > 0 && botPlayables.GetBotData.Inventory.Armor != null)
        {
            if (botPlayables.GetBotData.Inventory.Armor.Supplies < 100)
            {
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.RepairArmorPlayable);
                return;
            }
        }

        MovePlayer();
    }

    private void MovePlayer()
    {
        botMovement.DetectTarget();

        if (botMovement.detectedTarget != null)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SpearRun);
        }
        else
        {
            botController.Move(Vector3.zero, 0f);

            if (botMovement.IdleBeforeWanderTimer.Expired(botMovement.Runner))
            {
                botMovement.WanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));

                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SpearRun);
            }
        }
    }
}
