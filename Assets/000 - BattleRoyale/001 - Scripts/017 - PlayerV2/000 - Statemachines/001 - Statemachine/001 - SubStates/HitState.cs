using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class HitState : PlayerOnGround
{
    float timer;
    bool canAction;

    public HitState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
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

        playerPlayables.healthV2.IsHit = false;
        canAction = false;
    }


    public override void NetworkUpdate()
    {
        Animation();

        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {

        if (playerPlayables.healthV2.IsStagger)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsDead)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);
            return;
        }

        if (!characterController.IsGrounded)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);
            return;
        }

        if (playerPlayables.healthV2.IsHit)
        {
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.HitPlayable);
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
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RollPlayable);

        if (playerPlayables.inventory.WeaponIndex == 1)
        {

            if (playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.Punch1Playable);

            if (playerMovement.IsBlocking)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
            }
            else
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.Attacking)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable);

                if (playerMovement.IsBlocking)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordBlockPlayable);

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);
                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.Attacking)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearFirstAttackPlayable);

                if (playerMovement.IsBlocking)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearBlockPlayable);

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);
                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearIdlePlayable);
            }
        }
    }
}
