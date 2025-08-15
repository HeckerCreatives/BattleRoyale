using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerUpperRoll : UpperNoAimState
{
    float timer;
    bool canAction;

    public PlayerUpperRoll(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
        base.NetworkUpdate();

        playerPlayables.aimWeights.HipsOffset(0f, 0f, 0f);
        Animation();
    }

    private void Animation()
    {
        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.upperBodyMovement.FallingPlayables);

            WeaponsChecker();
        }
    }

    private void WeaponsChecker()
    {
        if (playerPlayables.inventory.WeaponIndex == 1)
        {
            if (playerMovement.MoveDirection != Vector3.zero)
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
                if (playerMovement.MoveDirection != Vector3.zero)
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
                if (playerMovement.MoveDirection != Vector3.zero)
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
        else if (playerPlayables.inventory.WeaponIndex == 3)
        {
            if (playerPlayables.inventory.SecondaryWeaponID() == "003")
            {
                if (playerMovement.MoveDirection != Vector3.zero)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RifleIdle);
            }
            else if (playerPlayables.inventory.SecondaryWeaponID() == "004")
            {
                if (playerMovement.MoveDirection != Vector3.zero)
                {
                    if (playerMovement.IsSprint)
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowSprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowRunPlayable);
                }
                else
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.BowIdlePlayable);
            }
        }
    }
}
