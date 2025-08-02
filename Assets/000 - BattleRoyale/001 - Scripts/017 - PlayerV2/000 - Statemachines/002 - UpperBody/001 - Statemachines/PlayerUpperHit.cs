using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperHit : UpperBodyAnimations
{
    float timer;
    bool canAction;

    public PlayerUpperHit(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
    }

    private void Animation()
    {

        //if (playerPlayables.healthV2.IsSecondHit)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.MiddleHitPlayable);
        //    return;
        //}

        //if (playerPlayables.healthV2.IsStagger)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.StaggerHitPlayable);
        //    return;
        //}

        //if (playerPlayables.healthV2.IsDead)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);
        //    return;
        //}

        //if (!characterController.IsGrounded)
        //{
        //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);
        //    return;
        //}

        //if (playerPlayables.healthV2.IsHit)
        //{
        //    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.HitPlayable);
        //    return;
        //}

        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            CheckWeapon();
        }
    }

    private void CheckWeapon()
    {
        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RollPlayables);

        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.Attacking)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FirstPunch);

            //if (playerMovement.IsBlocking)
            //    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BlockPlayable);

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
            }
            else
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
        }
        //else if (playerPlayables.inventory.WeaponIndex == 2)
        //{
        //    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
        //    {
        //        if (playerMovement.Attacking)
        //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable);

        //        if (playerMovement.IsBlocking)
        //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordBlockPlayable);

        //        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        //        {
        //            if (playerMovement.IsSprint)
        //                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);
        //            else
        //                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
        //        }
        //        else
        //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
        //    }
        //    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
        //    {
        //        if (playerMovement.Attacking)
        //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearFirstAttackPlayable);

        //        if (playerMovement.IsBlocking)
        //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearBlockPlayable);

        //        if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
        //        {
        //            if (playerMovement.IsSprint)
        //                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);
        //            else
        //                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
        //        }
        //        else
        //            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearIdlePlayable);
        //    }
        //}
    }
}
