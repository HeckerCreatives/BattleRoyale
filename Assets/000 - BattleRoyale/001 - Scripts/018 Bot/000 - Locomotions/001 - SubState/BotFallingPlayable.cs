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

            if (botPlayables.Inventroy.WeaponIndex == 1)
                botPlayablesChanger.ChangeState(botPlayables.BasicMovement.IdlePlayable);
            else if (botPlayables.Inventroy.WeaponIndex == 2)
            {
                if (botPlayables.Inventroy.GetPrimaryWeaponID() == "001")
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SwordIdlePlayable);
                else if (botPlayables.Inventroy.GetPrimaryWeaponID() == "002")
                    botPlayablesChanger.ChangeState(botPlayables.BasicMovement.SpearIdle);
            }
        }
    }

    private void MoveBot()
    {
        botMovement.MoveInDirection();
    }
}
