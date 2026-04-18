using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static Unity.Collections.Unicode;

public class PlayerUpperRoll : UpperNoAimState
{
    public PlayerUpperRoll(SimpleKCC characterController, UpperBodyChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool canAnimateUpper) : base(characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, canAnimateUpper)
    {
    }

    public override void NetworkUpdate()
    {
        base.NetworkUpdate();

        TryExitRoll();
    }

    private void TryExitRoll()
    {
        var nextState = GetPostRollUpperState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private UpperBodyAnimations GetPostRollUpperState()
    {
        var upper = playerPlayables.upperBodyMovement;
        var inventory = playerPlayables.inventory;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.RollStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);
        int cancelStartTick = Mathf.CeilToInt(totalPunchTicks * 0.25f);

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina >= 10f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (elapsedTicks < cancelStartTick) return null;

                    if (playerMovement.IsJumping) return upper.JumpPlayable;

                    if (elapsedTicks < finishStartTick) return null;

                    if (isMoving)
                    {
                        if (canSprint) return upper.SprintPlayables;

                        return upper.RunPlayables;
                    }

                    return upper.IdlePlayables;
                }

            case 2:
                {
                    if (elapsedTicks < cancelStartTick) return null;

                    string primaryId = inventory.PrimaryWeaponID();

                    if (playerMovement.IsJumping) return upper.JumpPlayable;

                    if (elapsedTicks < finishStartTick) return null;

                    if (primaryId == "001")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return upper.SwordSprint;

                            return upper.SwordRunPlayable;
                        }
                        return upper.SwordIdlePlayable;
                    }
                    if (primaryId == "002")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return upper.SpearSprintPlayable;

                            return upper.SpearRunPlayable;
                        }
                        return upper.SpearIdle;
                    }

                    break;
                }

            case 3:
                {
                    if (elapsedTicks < cancelStartTick) return null;

                    string secondaryId = inventory.SecondaryWeaponID();


                    if (playerMovement.IsJumping) return upper.JumpPlayable;

                    if (elapsedTicks < finishStartTick) return null;

                    if (secondaryId == "003")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return upper.RifleSprintPlayable;

                            return upper.RifleRunPlayable;
                        }

                        return upper.RifleIdle;
                    }
                    if (secondaryId == "004")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return upper.BowSprintPlayable;

                            return upper.BowRunPlayable;
                        }

                        return upper.BowIdlePlayable;
                    }

                    break;
                }
        }

        if (!characterController.IsGrounded)
            return upper.FallingPlayables;

        return upper.IdlePlayables;
    }
}
