using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class BlockState : PlayerOnGround
{
    float blockwindowend;
    float timer;
    bool canAction;

    public BlockState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        blockwindowend = playerPlayables.TickRateAnimation + 0.25f;
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
        if (playerPlayables.TickRateAnimation >= timer && canAction)
        {
            if (playerMovement.MoveDirection != Vector3.zero)
            {
                if (playerPlayables.inventory.WeaponIndex == 1)
                {
                    if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.RunPlayable);
                }
                else if (playerPlayables.inventory.WeaponIndex == 2)
                {
                    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordRunPlayable);
                    }
                    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearRunPlayable);
                    }
                }
            }
            else
            {
                if (playerPlayables.inventory.WeaponIndex == 1)
                    playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.IdlePlayable);
                else if (playerPlayables.inventory.WeaponIndex == 2)
                {
                    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SwordIdlePlayable);
                    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                        playablesChanger.ChangeState(playerPlayables.lowerBodyMovement.SpearIdlePlayable);
                }
            }
        }
    }
}
