using Fusion.Addons.SimpleKCC;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class JumpState : PlayerOnGround
{
    float timer;

    public JumpState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.CancelInvoke();

        playerPlayables.PlayJumpSoundEffect();

        timer = playerPlayables.TickRateAnimation + 0.15f;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.CancelInvoke();
    }

    public override void NetworkUpdate()
    {
        playerMovement.MoveCharacter();
        Animation();
        WeaponChecker();    //  NEXT FUNCTION AFTER RESETTING JUMP STATE
        playerPlayables.stamina.RecoverStamina(5f);
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.DeathPlayable);

        if (!playerMovement.IsJumping)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);
        }

        if (playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPunchPlayable);
            else if (playerPlayables.inventory.WeaponIndex == 2)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordJumpAttackPlayable);
        }

        if (characterController.IsGrounded)
        {
            playerMovement.IsJumping = false;
            playerMovement.JumpImpulse = 0;
        }
        else
        {
            if (playerPlayables.TickRateAnimation >= timer)
            {
                playerMovement.IsJumping = false;
                playerMovement.JumpImpulse = 0;
            }
        }
    }

    private void WeaponChecker()
    {

        if (playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.JumpPunchPlayable);
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordJumpAttackPlayable);
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearJumpAttackPlayable);
            }
        }

        if (characterController.IsGrounded)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
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
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
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
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
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
            else if (playerPlayables.inventory.WeaponIndex == 3)
            {
                if (playerPlayables.inventory.SecondaryWeaponID() == "003")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    {
                        if (playerMovement.IsSprint)
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleRunPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RifleIdlePlayable);
                }
                else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    {
                        if (playerMovement.IsSprint)
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowRunPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.BowIdlePlayable);
                }
            }
        }
    }
}
