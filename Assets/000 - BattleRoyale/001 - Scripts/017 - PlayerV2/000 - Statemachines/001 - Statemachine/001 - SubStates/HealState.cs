using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class HealState : PlayerOnGround
{

    bool doneHeal;

    public HealState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) playerMovement.IsHealing = false;

        if (!playerPlayables.HasStateAuthority) return;

        doneHeal = true;
    }

    public override void Exit()
    {
        base.Exit();

        if (!playerPlayables.HasStateAuthority) return;

        doneHeal = false;
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        double animTime = animationClipPlayable.GetTime();

        double healTime = animationLength * 0.5f;
        bool beforeHeal = animTime < healTime;
        bool finished = animTime >= animationLength - 0.02f;

        if (animTime >= healTime && !doneHeal)
        {
            if (playerPlayables.HasStateAuthority)
            {
                playerPlayables.healthV2.HealHealth();
                doneHeal = true;
            }
        }

        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetNextState(animTime);

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }
        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetNextState(animTime);

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }

    }
    private AnimationPlayable GetNextState(double clipTime)
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        if (playerMovement.IsJumping)
            return playerPlayables.lowerBodyMovement.JumpPlayable;

        if (playerMovement.IsBlocking)
            return GetBlockState();

        if (playerPlayables.healthV2.IsStagger)
            return playerPlayables.lowerBodyMovement.StaggerHitPlayable;

        if (playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (clipTime < animationLength - 0.02f)
            return null;

        return GetWeaponGroundedStateAfterHeal();
    }

    private AnimationPlayable GetWeaponGroundedStateAfterHeal()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.Attacking)
                return playerPlayables.lowerBodyMovement.Punch1Playable;

            if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
            {
                if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                    return playerPlayables.lowerBodyMovement.SprintPlayable;

                return playerPlayables.lowerBodyMovement.RunPlayable;
            }

            return playerPlayables.lowerBodyMovement.IdlePlayable;
        }
        else if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
            {
                if (playerMovement.Attacking)
                    return playerPlayables.lowerBodyMovement.SwordAttackFirstPlayable;

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return playerPlayables.lowerBodyMovement.SwordSprintPlayable;

                    return playerPlayables.lowerBodyMovement.SwordRunPlayable;
                }

                return playerPlayables.lowerBodyMovement.SwordIdlePlayable;
            }
            else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
            {
                if (playerMovement.Attacking)
                    return playerPlayables.lowerBodyMovement.SpearFirstAttackPlayable;

                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return playerPlayables.lowerBodyMovement.SpearSprintPlayable;

                    return playerPlayables.lowerBodyMovement.SpearRunPlayable;
                }

                return playerPlayables.lowerBodyMovement.SpearIdlePlayable;
            }
        }
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return playerPlayables.lowerBodyMovement.RifleSprintPlayable;

                    return playerPlayables.lowerBodyMovement.RifleRunPlayable;
                }

                return playerPlayables.lowerBodyMovement.RifleIdlePlayable;
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.XMovement != 0 || playerMovement.YMovement != 0)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        return playerPlayables.lowerBodyMovement.BowSprintPlayable;

                    return playerPlayables.lowerBodyMovement.BowRunPlayable;
                }

                return playerPlayables.lowerBodyMovement.BowIdlePlayable;
            }
        }

        return null;
    }

    private AnimationPlayable GetBlockState()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
            return playerPlayables.lowerBodyMovement.BlockPlayable;

        if (playerPlayables.inventory.WeaponIndex == 2)
        {
            if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                return playerPlayables.lowerBodyMovement.SwordBlockPlayable;

            if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                return playerPlayables.lowerBodyMovement.SpearBlockPlayable;
        }

        return playerPlayables.lowerBodyMovement.BlockPlayable;
    }
}
