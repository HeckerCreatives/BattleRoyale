using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PlayerBlockPlayable : UpperBodyAnimations
{
    float timer;
    bool canAction;
    public PlayerBlockPlayable(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void Enter()
    {
        base.Enter();

        //blockwindowend = playerPlayables.TickRateAnimation + 0.25f;
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
        playerPlayables.healthV2.IsStagger = false;

        Animation();
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
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SprintPlayables);

                    else
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.RunPlayables);
                }
                else if (playerPlayables.inventory.WeaponIndex == 2)
                {
                    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordSprint);

                        else
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordRunPlayable);
                    }
                    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                    {
                        if (playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f)
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearSprintPlayable);

                        else
                            playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearRunPlayable);
                    }
                }
            }
            else
            {
                if (playerPlayables.inventory.WeaponIndex == 1)
                    playablesChanger.ChangeState(playerPlayables.upperBodyMovement.IdlePlayables);
                else if (playerPlayables.inventory.WeaponIndex == 2)
                {
                    if (playerPlayables.inventory.PrimaryWeaponID() == "001")
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SwordIdlePlayable);
                    else if (playerPlayables.inventory.PrimaryWeaponID() == "002")
                        playablesChanger.ChangeState(playerPlayables.upperBodyMovement.SpearIdle);
                }
            }
        }
    }
}
