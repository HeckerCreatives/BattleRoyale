using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class IdleState : PlayerOnGround
{

    public IdleState(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void LogicUpdate()
    {

    }

    public override void NetworkUpdate()
    {
        if (playerMovement.MoveDirection != Vector3.zero)
        {
            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
        }

        if (playerMovement.IsRoll)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

        playerPlayables.stamina.RecoverStamina(5f);
    }
}
