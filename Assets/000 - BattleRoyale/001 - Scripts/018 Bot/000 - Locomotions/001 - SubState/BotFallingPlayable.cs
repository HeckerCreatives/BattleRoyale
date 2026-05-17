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

    public override BotAnimationPlayable NetworkUpdate()
    {
        base.NetworkUpdate();

        FallDamage();
        return Animation();
}

    private void FallDamage()
    {
        if (botController.RealVelocity.y <= -20f)
        {
            fallDamage = Mathf.Abs(botController.RealVelocity.y) - 5f;
        }
    }

    private BotAnimationPlayable Animation()
    {
        MoveBot();

        if (botPlayables.GetBotData.IsDead)
        {
            return botPlayables.BasicMovement.DeathPlayable;
        }

        if (botController.IsGrounded)
        {
            if (fallDamage > 0)
                botPlayables.GetBotData.FallDamage(fallDamage);

            if (botPlayables.GetBotData.IsDead)
            {
                return botPlayables.BasicMovement.DeathPlayable;
            }

            int weaponIndex = botPlayables.Inventroy.WeaponIndex;

            if (weaponIndex == 2)
            {
                string priId = botPlayables.Inventroy.GetPrimaryWeaponID();
                if (priId == "001") return botPlayables.BasicMovement.SwordIdlePlayable;
                if (priId == "002") return botPlayables.BasicMovement.SpearIdle;
                return botPlayables.BasicMovement.IdlePlayable;
            }

            if (weaponIndex == 3)
            {
                string secId = botPlayables.Inventroy.GetSecondaryWeaponID();
                if (secId == "003") return botPlayables.BasicMovement.RifleIdlePlayable;
                if (secId == "004") return botPlayables.BasicMovement.BowIdlePlayable;
                return botPlayables.BasicMovement.IdlePlayable;
            }

            // WeaponIndex == 1 or unknown — fall back to bare-hand idle
            return botPlayables.BasicMovement.IdlePlayable;
        }

        return null;
}

    private void MoveBot()
    {
        // Don't run wander/navmesh logic mid-air — let gravity pull the bot down cleanly.
        botController.Move(Vector3.zero, 0f);
    }
}



