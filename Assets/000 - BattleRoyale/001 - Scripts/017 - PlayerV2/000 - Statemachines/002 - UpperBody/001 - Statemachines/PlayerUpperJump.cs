using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperJump : UpperBodyAnimations
{
    public PlayerUpperJump(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        playerMovement.WeaponSwitcher();
        Animation();
        WeaponChecker();
    }

    private void Animation()
    {
        if (playerPlayables.healthV2.IsDead)
            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.DeathPlayable);

        if (!playerMovement.IsJumping)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);
        }
    }

    private void WeaponChecker()
    {

        if (playerMovement.Attacking)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.JumpPunchPlayable);
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordJumpAttackPlayable);
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearJumpAttackPlayable);
            }
        }

        if (characterController.IsGrounded)
        {
            if (playerPlayables.inventory.WeaponIndex == 1)
            {
                if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
            }
            else if (playerPlayables.inventory.WeaponIndex == 2)
            {
                if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    {
                        if (playerMovement.IsSprint)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);

                        else
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                }
                else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                {
                    if (playerMovement.XMovement == 0 && playerMovement.YMovement == 0)
                    {
                        if (playerMovement.IsSprint)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);
                    }
                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
                }
            }
        }
    }
}
