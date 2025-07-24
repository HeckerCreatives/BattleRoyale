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

    public BlockState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
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
        playerPlayables.healthV2.IsHit = false;
        playerPlayables.healthV2.IsSecondHit = false;
        playerPlayables.healthV2.IsStagger = false;

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
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SprintPlayable);

                    else
                        playablesChanger.ChangeState(playerPlayables.basicMovement.RunPlayable);
                }
                else if (playerPlayables.inventory.WeaponIndex == 2)
                {
                    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.basicMovement.SwordRunPlayable);
                    }
                    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.basicMovement.SpearSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.basicMovement.SpearRunPlayable);
                    }
                }
            }
            else
            {
                if (playerPlayables.inventory.WeaponIndex == 1)
                    playablesChanger.ChangeState(playerPlayables.basicMovement.IdlePlayable);
                else if (playerPlayables.inventory.WeaponIndex == 2)
                {
                    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SwordIdlePlayable);
                    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                        playablesChanger.ChangeState(playerPlayables.basicMovement.SpearIdlePlayable);
                }
            }
        }
    }
}
