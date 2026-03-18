using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

public class BlockState : PlayerOnGround
{
    public BlockState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority || playerPlayables.HasStateAuthority) playerMovement.IsBlocking = false;
    }


    public override void NetworkUpdate()
    {
        characterController.Move(Vector3.zero, 0f);

        TryExitBlock();
    }

    private void TryExitBlock()
    {
        if (animationClipPlayable.GetTime() < animationLength - 0.02f)
            return;

        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetPostBlockLowerState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }

        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetPostBlockLowerState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }

            playerPlayables.stamina.RecoverStamina(5f);
        }
    }

    private AnimationPlayable GetPostBlockLowerState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    //if (!isMoving)
                    //    return lower.IdlePlayable;

                    if (canSprint)
                        return lower.SprintPlayable;

                    //return lower.RunPlayable;
                    return lower.IdlePlayable;
                }

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (!isMoving)
                    {
                        if (primaryId == "001") return lower.SwordIdlePlayable;
                        if (primaryId == "002") return lower.SpearIdlePlayable;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (primaryId == "001") return lower.SwordSprintPlayable;
                            if (primaryId == "002") return lower.SpearSprintPlayable;
                        }

                        if (primaryId == "001") return lower.SwordRunPlayable;
                        if (primaryId == "002") return lower.SpearRunPlayable;
                    }

                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (!isMoving)
                    {
                        if (secondaryId == "003") return lower.RifleIdlePlayable;
                        if (secondaryId == "004") return lower.BowIdlePlayable;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (secondaryId == "003") return lower.RifleSprintPlayable;
                            if (secondaryId == "004") return lower.BowSprintPlayable;
                        }

                        if (secondaryId == "003") return lower.RifleRunPlayable;
                        if (secondaryId == "004") return lower.BowRunPlayable;
                    }

                    break;
                }
        }

        return lower.IdlePlayable;
    }
}
