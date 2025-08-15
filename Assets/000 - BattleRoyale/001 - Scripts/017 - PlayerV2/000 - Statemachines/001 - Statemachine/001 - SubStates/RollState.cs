using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RollState : PlayerOnGround
{
    float timer;
    bool canAction;
    bool canReduce;

    public RollState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerPlayables.CancelInvoke();

        playerPlayables.PlayRollSoundEffect();

        timer = playerPlayables.TickRateAnimation + animationLength;
        canAction = true;
        canReduce = true;

    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.CancelInvoke();

        canAction = false;
        canReduce = false;
    }


    public override void NetworkUpdate()
    {
        characterController.Move(characterController.TransformDirection * 8f, 0f);

        Animation();


        if (canReduce)
        {
            playerPlayables.stamina.ReduceStamina(35f);
            canReduce = false;
        }
    }

    private void Animation()
    {
        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (!characterController.IsGrounded)
                playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.FallingPlayable);

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
                if (playerMovement.MoveDirection != Vector3.zero)
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
                if (playerMovement.MoveDirection != Vector3.zero)
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
                if (playerMovement.MoveDirection != Vector3.zero)
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
                if (playerMovement.MoveDirection != Vector3.zero)
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
