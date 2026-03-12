using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static Unity.Collections.Unicode;

public class PlayerUpperRoll : UpperNoAimState
{
    public PlayerUpperRoll(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        TryExitRoll();
    }

    private void TryExitRoll()
    {
        if (animationClipPlayable.GetTime() < animationLength - 0.025f)
            return;

        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetPostRollUpperState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }

        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetPostRollUpperState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }
    }

    private UpperBodyAnimations GetPostRollUpperState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina > 0f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    //if (!isMoving)
                    //    return upper.IdlePlayables;

                    if (canSprint)
                        return upper.SprintPlayables;

                    //return upper.RunPlayables;
                    return upper.IdlePlayables;
                }

            case 2:
                {
                    string primaryId = inventory.PrimaryWeaponID();

                    if (!isMoving)
                    {
                        if (primaryId == "001") return upper.SwordIdlePlayable;
                        if (primaryId == "002") return upper.SpearIdle;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (primaryId == "001") return upper.SwordSprint;
                            if (primaryId == "002") return upper.SpearSprintPlayable;
                        }

                        if (primaryId == "001") return upper.SwordRunPlayable;
                        if (primaryId == "002") return upper.SpearRunPlayable;
                    }

                    break;
                }

            case 3:
                {
                    string secondaryId = inventory.SecondaryWeaponID();

                    if (!isMoving)
                    {
                        if (secondaryId == "003") return upper.RifleIdle;
                        if (secondaryId == "004") return upper.BowIdlePlayable;
                    }
                    else
                    {
                        if (canSprint)
                        {
                            if (secondaryId == "003") return upper.RifleSprintPlayable;
                            if (secondaryId == "004") return upper.BowSprintPlayable;
                        }

                        if (secondaryId == "003") return upper.RifleRunPlayable;
                        if (secondaryId == "004") return upper.BowRunPlayable;
                    }

                    break;
                }
        }

        return upper.IdlePlayables;
    }
}
