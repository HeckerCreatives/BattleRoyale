using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RepairState : AnimationPlayable
{
    float healtimer;
    float timer;
    bool canAction;
    bool doneHeal;

    public RepairState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        healtimer = playerPlayables.TickRateAnimation + (animationLength * 0.5f);
        timer = playerPlayables.TickRateAnimation + animationLength;
        doneHeal = false;
        canAction = true;
    }

    public override void Exit()
    {
        base.Exit();

        doneHeal = false;
        canAction = false;
    }

    public override void NetworkUpdate()
    {
        if (!doneHeal && playerPlayables.TickRateAnimation > healtimer)
        {
            playerPlayables.healthV2.RepairArmor();
            doneHeal = true;
        }

        if (playerPlayables.TickRateAnimation < healtimer)
        {
            Animations();
        }

        if (canAction && playerPlayables.TickRateAnimation >= timer)
        {

            Animations();
        }
    }

    private void Animations()
    {
        if (playerPlayables.healthV2.IsDead)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);
        }

        if (!characterController.IsGrounded)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);
            return;
        }

        if (playerMovement.IsJumping)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPlayable);
            return;
        }

        if (playerMovement.IsBlocking)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsHit)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
            return;
        }

        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        {


            if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);

            return;
        }

        if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
        {
            if (canAction && playerPlayables.TickRateAnimation >= timer)
            {
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);
                canAction = false;
                return;
            }
        }

        if (playerMovement.Attacking)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.Punch1Playable);
            return;
        }

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
        {

            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);
            return;
        }
    }
}
