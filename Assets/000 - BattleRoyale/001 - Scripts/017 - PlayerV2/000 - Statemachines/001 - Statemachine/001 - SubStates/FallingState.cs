using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class FallingState : AnimationPlayable
{
    float fallDamage;

    public FallingState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        fallDamage = 0;
    }

    public override void Exit()
    {
        base.Exit();

    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();

        playerMovement.WeaponSwitcher();

        FallDamage();
        Animation();
        WeaponsChecker(); //    NEXT FUNCTION AFTER DAMAGE IS APPLIED
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void FallDamage()
    {
        if (characterController.RealVelocity.y <= -20f)
        {
            playerPlayables.healthV2.FallDamageValue = Mathf.Abs(characterController.RealVelocity.y) - 5f;
        }
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.basicMovement.DeathPlayable);

        if (characterController.IsGrounded)
        {
            playerMovement.JumpImpulse = 0;

            if (playerPlayables.healthV2.FallDamageValue > 0)
                playerPlayables.healthV2.FallDamae();
        }
    }

    private void WeaponsChecker()
    {
        if (playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                playablesChanger.ChangeState(playerPlayables.basicMovement.JumpPunchPlayable);
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SwordJumpAttackPlayable);
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    playablesChanger.ChangeState(playerPlayables.basicMovement.SpearJumpAttackPlayable);
            }
        }

        if (characterController.IsGrounded)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
                else if (playerMovement.IsSprint)
                {
                    if (playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
            }
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);
                    else if (playerMovement.IsSprint)
                    {
                        if (playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordSprintPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);
                }
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SpearIdlePlayable);
                    else if (playerMovement.IsSprint)
                    {
                        if (playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.basicMovement.SpearSprintPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SpearRunPlayable);
                }
            }
        }
    }
}
