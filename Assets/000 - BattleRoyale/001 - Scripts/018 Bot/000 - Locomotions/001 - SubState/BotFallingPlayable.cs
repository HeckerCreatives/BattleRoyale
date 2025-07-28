using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BotFallingPlayable : BotAnimationPlayable
{
    float fallDamage;

    public BotFallingPlayable(MonoBehaviour host, SimpleKCC botController, BotPlayableChanger botPlayablesChanger, BotMovementController botMovement, BotPlayables botPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, botController, botPlayablesChanger, botMovement, botPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        fallDamage = 0;
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void NetworkUpdate()
    {
        FallDamage();
        Animation();
    }

    private void FallDamage()
    {
        if (botController.RealVelocity.y <= -20f)
        {
            fallDamage = Mathf.Abs(botController.RealVelocity.y) - 5f;
        }
    }

    private void Animation()
    {
        MoveBot();

        if (botPlayables.GetBotData.IsDead)
        {
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.DeathPlayable);
            return;
        }

        if (botController.IsGrounded)
        {
            //playerMovement.JumpImpulse = 0;

            if (fallDamage > 0)
                botPlayables.GetBotData.FallDamage(fallDamage);

            if (botPlayables.GetBotData.IsDead)
            {
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.DeathPlayable);
                return;
            }

            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.IdlePlayable);
        }
    }

    private void MoveBot()
    {
        botMovement.MoveInDirection();

        if (botMovement.WanderTimer.Expired(botMovement.Runner))
        {
            botMovement.IdleBeforeWanderTimer = TickTimer.CreateFromSeconds(botMovement.Runner, Random.Range(botMovement.MinWanderDelay, botMovement.MaxWanderDelay));
            botPlayablesChanger.ChangeState(botPlayables.BasicMovement.IdlePlayable);
        }
    }
}
