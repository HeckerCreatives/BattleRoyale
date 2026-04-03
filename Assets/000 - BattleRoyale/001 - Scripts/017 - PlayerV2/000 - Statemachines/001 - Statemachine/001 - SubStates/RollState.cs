using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static Unity.Collections.Unicode;

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

        if (!playerPlayables.HasStateAuthority) return;

        canReduce = true;
    }

    public override void Exit()
    {
        base.Exit();

        playerMovement.IsRoll = false;

        if (playerPlayables.HasInputAuthority)
            playerPlayables.CancelInvoke();

        if (!playerPlayables.HasStateAuthority) return;

        canReduce = false;
    }

    public override void NetworkUpdate()
    {
        float currentTime = (float)animationClipPlayable.GetTime();

        //if (currentTime <= animationLength * 0.8f)
        //    characterController.Move(
        //        playerMovement.MainCharObj.forward * 400f * playerPlayables.Runner.DeltaTime,
        //        0f
        //    );


        TryExitRoll(currentTime);

        if (canReduce && playerPlayables.HasStateAuthority)
        {
            playerPlayables.stamina.ReduceStamina(35f);
            canReduce = false;
        }
    }

    private void TryExitRoll(float currentTime)
    {
        var nextState = GetPostRollState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }
    }

    private AnimationPlayable GetPostRollState()
    {
        var lower = playerPlayables.lowerBodyMovement;
        var inventory = playerPlayables.inventory;

        bool isMoving = playerMovement.XMovement != 0f || playerMovement.YMovement != 0f;
        bool canSprint = playerMovement.IsSprint && playerPlayables.stamina.Stamina > 0f;

        switch (inventory.WeaponIndex)
        {
            case 1:
                {
                    if (animationClipPlayable.GetTime() < animationLength * 0.25f) return null;

                    if (playerMovement.IsJumping) return lower.JumpPlayable;

                    if (animationClipPlayable.GetTime() < animationLength * 0.85f) return null;

                    return lower.IdlePlayable;
                }

            case 2:
                {
                    if (animationClipPlayable.GetTime() < animationLength * 0.25f) return null;

                    string primaryId = inventory.PrimaryWeaponID();


                    if (playerMovement.IsJumping) return lower.JumpPlayable;

                    if (animationClipPlayable.GetTime() < animationLength * 0.85f) return null;

                    if (primaryId == "001") return lower.SwordIdlePlayable;
                    if (primaryId == "002") return lower.SpearIdlePlayable;

                    break;
                }

            case 3:
                {
                    if (animationClipPlayable.GetTime() < animationLength * 0.25f) return null;

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

        if (!characterController.IsGrounded)
            return lower.FallingPlayable;

        return lower.IdlePlayable;
    }
}
