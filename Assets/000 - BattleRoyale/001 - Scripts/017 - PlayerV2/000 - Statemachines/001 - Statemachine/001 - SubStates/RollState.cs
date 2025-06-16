using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RollState : PlayerOnGround
{
    float timer;
    
    public RollState(SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = Time.time + 3f;
    }

    public override void NetworkUpdate()
    {
        if (Time.time >= timer)
        {
            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);

            playerMovement.IsRoll = false;
        }
    }
}
