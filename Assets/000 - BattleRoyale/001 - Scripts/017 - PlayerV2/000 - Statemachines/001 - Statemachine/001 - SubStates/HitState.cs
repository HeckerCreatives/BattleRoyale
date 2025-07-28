using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class HitState : PlayerOnGround
{
    float timer;
    bool canAction;

    public HitState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.healthV2.IsHit = false;
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
        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {

        if (playerPlayables.healthV2.IsSecondHit)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.MiddleHitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.StaggerHitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);
            return;
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.FallingPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsHit)
        {
            playablesChanger.ChangeState(playerPlayables.basicMovement.HitPlayable);
            return;
        }

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            CheckWeapon();
        }
    }

    private void CheckWeapon()
    {
        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.basicMovement.RollPlayable);

        if (playerPlayables.inventory.WeaponIndex == 1)
        {

            if (playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.Punch1Playable);

            if (playerMovement.IsBlocking)
                playablesChanger.ChangeState(playerPlayables.basicMovement.BlockPlayable);

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.Attacking)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordAttackFirstPlayable);

                if (playerMovement.IsBlocking)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordBlockPlayable);

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordSprintPlayable);
                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.Attacking)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SpearFirstAttackPlayable);

                if (playerMovement.IsBlocking)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SpearBlockPlayable);

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SpearSprintPlayable);
                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SpearRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SpearIdlePlayable);
            }
        }
    }
}
