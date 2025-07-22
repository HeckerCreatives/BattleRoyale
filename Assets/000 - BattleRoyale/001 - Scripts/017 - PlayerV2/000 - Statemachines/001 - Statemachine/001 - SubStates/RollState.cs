using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RollState : PlayerOnGround
{
    float timer;
    bool canAction;
    bool canReduce;

    public RollState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        canAction = true;
        canReduce = true;

    }

    public override void Exit()
    {
        base.Exit();
        canAction = false;
        canReduce = false;
    }


    public override void NetworkUpdate()
    {
        if (canReduce)
        {
            playerPlayables.stamina.ReduceStamina(35f);
            canReduce = false;
        }

        Animation();

        characterController.Move(characterController.TransformDirection * 8f, 0f);
    }

    private void Animation()
    {
        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);

            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
        }
    }
}
