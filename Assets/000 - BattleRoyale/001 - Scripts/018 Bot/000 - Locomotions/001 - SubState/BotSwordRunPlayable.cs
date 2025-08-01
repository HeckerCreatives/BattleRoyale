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

    public override void NetworkUpdate()
    {
        if (botPlayables.GetBotData.IsDead)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.DeathPlayable);
            return;
        }

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

        if (botPlayables.Inventroy.TrapCount > 0 && randPlaceTrap <= 20)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.TrapPlayable);
            return;
        }

        MoveBot();
    }

    private void MoveBot()
    {
        botMovement.DetectTarget();

        if (botMovement.detectedTarget != null)
        {
            botMovement.MoveToTarget();

            if (botMovement.CanSwordAttack())
            {
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SwordAttackOnePlayable);
            }
        }
        else
        {
            botMovement.MoveInDirection();

            if (botMovement.WanderTimer.Expired(botMovement.Runner))
            {
                botMovement.IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));

                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SwordIdlePlayable);
            }
        }
    }
}
