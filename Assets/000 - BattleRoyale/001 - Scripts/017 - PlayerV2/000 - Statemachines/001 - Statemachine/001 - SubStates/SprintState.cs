using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class SprintState : PlayerOnGround
{
    public SprintState(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername)
    {
    }

    public override void LogicUpdate()
    {
        

    }

    public override void NetworkUpdate()
    {
        if (playerPlayables.stamina.Stamina > 0f)
        {
            if (playerMovement.MoveDirection == Vector3.zero)
            {
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
            }
            else if (!playerMovement.IsSprint)
            {
                if (playerMovement.MoveDirection == Vector3.zero)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
        }
        else
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
        }
        
        if (playerMovement.IsRoll)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

        playerPlayables.stamina.DecreaseStamina(20f);
    }
}
