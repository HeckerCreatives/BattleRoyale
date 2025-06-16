using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class RunState : PlayerOnGround
{

    public RunState(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername)
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
        if (playerMovement.MoveDirection == Vector3.zero)
            playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);

        else if (playerMovement.IsSprint)
        {
            if (playerPlayables.stamina.Stamina >= 10f)
                playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);
        }

        if (playerMovement.IsRoll)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

        playerPlayables.stamina.RecoverStamina(5f);
    }
}
