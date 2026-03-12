using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class RollState : PlayerOnGround
{
    bool canReduce;

    public RollState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority)
            playerPlayables.CancelInvoke();

        playerPlayables.PlayRollSoundEffect();

        canReduce = true;
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.HasInputAuthority)
            playerPlayables.CancelInvoke();

        canReduce = false;
    }

    public override void NetworkUpdate()
    {
        float currentTime = (float)animationClipPlayable.GetTime();
        float remaining = animationLength - currentTime;

        float rollSpeed = 400f;

        if (remaining < 0.1f)
        {
            float t = remaining / 0.1f;
            rollSpeed *= Mathf.Clamp01(t);
        }

        characterController.Move(
            playerMovement.MainCharObj.forward * rollSpeed * playerMovement.Runner.DeltaTime,
            0f
        );

        TryExitRoll(currentTime);

        if (canReduce)
        {
            playerPlayables.stamina.ReduceStamina(35f);
            canReduce = false;
        }
    }

    private void TryExitRoll(float currentTime)
    {
        if (currentTime < animationLength - 0.025f)
            return;


        if (playerPlayables.HasInputAuthority)
        {
            var predictedState = GetPostRollState();

            if (predictedState != null && playablesChanger.CurrentState != predictedState)
            {
                playablesChanger.ChangeState(predictedState);
            }
        }

        if (playerPlayables.HasStateAuthority)
        {
            var nextState = GetPostRollState();

            if (nextState != null && playablesChanger.CurrentState != nextState)
            {
                playablesChanger.ChangeState(nextState);
            }
        }
    }

    private AnimationPlayable GetPostRollState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        if (!characterController.IsGrounded)
            return lower.FallingPlayable;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina > 0f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    //if (!isMoving)
                    //    return lower.IdlePlayable;

                    if (canSprint)
                        return lower.SprintPlayable;

                    return lower.RunPlayable;
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
