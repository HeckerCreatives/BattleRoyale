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

        MovePlayer();
    }

    private void MovePlayer()
    {
        botMovement.DetectTarget();

        if (botMovement.detectedTarget != null)
        {
            if (botPlayables.Inventroy.WeaponIndex == 1)
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.RunPlayable);
            else if (botPlayables.Inventroy.WeaponIndex == 2)
            {
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SwordRunPlayable);
                else if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SpearRun);
            }
        }
        else
        {
            botController.Move(Vector3.zero, 0f);

            if (botMovement.IdleBeforeWanderTimer.Expired(botMovement.Runner))
            {
                botMovement.WanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
                if (botPlayables.Inventroy.WeaponIndex == 1)
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.RunPlayable);
                else if (botPlayables.Inventroy.WeaponIndex == 2)
                {
                    if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                        botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SwordRunPlayable);
                    else if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                        botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SpearRun);
                }
            }
        }
    }
}
