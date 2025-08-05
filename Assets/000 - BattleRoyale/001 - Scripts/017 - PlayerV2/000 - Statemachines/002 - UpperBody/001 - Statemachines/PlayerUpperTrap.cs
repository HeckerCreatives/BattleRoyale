using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperTrap : UpperBodyAnimations
{
    float timer;
    bool canAction;

    public PlayerUpperTrap(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        timer = playerPlayables.TickRateAnimation + animationLength;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        canAction = false;
    }

    public override void NetworkUpdate()
    {
        if (playerPlayables.healthV2.IsDead)
        {

            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (!characterController.IsGrounded)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
                return;
            }

            if (playerMovement.IsJumping)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPlayable);
                return;
            }

            if (playerMovement.IsBlocking)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BlockPlayable);
                return;
            }

            if (playerPlayables.healthV2.IsHitUpper)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
                return;
            }

            if (playerPlayables.healthV2.IsStagger)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.StaggerHitPlayable);
                return;
            }

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {


                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);

                return;
            }

            if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
                canAction = false;
                return;
            }

            if (playerMovement.Attacking)
            {
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);
                return;
            }

            if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            {

                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);
                return;
            }
        }
    }
}
