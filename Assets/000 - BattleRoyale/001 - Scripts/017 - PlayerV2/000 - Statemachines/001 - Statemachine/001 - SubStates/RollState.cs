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
        {
            playerMovement.AnimationTick = playerPlayables.Runner.Tick;

            playerPlayables.ChangeFOV(70);
        }

        playerPlayables.PlayRollSoundEffect();

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.RollStartTick = playerPlayables.Runner.Tick;
        playerMovement.Rolling = true;
        playerPlayables.healthV2.IsStagger = false;
        canReduce = true;

        playerMovement.Swording = false;
        playerMovement.Punching = false;
        playerMovement.SwordingMove = false;
        playerMovement.PunchingMove = false;
        playerMovement.WasPunchingMoveLastTick = false;
        playerMovement.WasRollingMoveLastTick = false;
        playerMovement.WasSwordingMoveLastTick = false;

        playerPlayables.stamina.ReduceStamina(35f);
    }

    public override void Exit()
    {
        base.Exit();

        if (playerPlayables.HasInputAuthority) playerPlayables.ChangeFOV(60);

        if (playerPlayables.HasInputAuthority)
            playerPlayables.CancelInvoke();

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.Rolling = false;
        canReduce = false;
    }

    public override void NetworkUpdate()
    {
        float currentTime = (float)animationClipPlayable.GetTime();

        playerMovement.MoveCharacter();
        FOVChanger();
        TryExitRoll(currentTime);
    }

    private void FOVChanger()
    {
        if (!playerPlayables.HasInputAuthority) return;

        if (!canReduce) return;

        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - playerMovement.AnimationTick;

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.25f);

        if (elapsedTicks >= finishStartTick)
        {
            playerPlayables.ChangeFOV(60);
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

                    if (playerMovement.IsJumping) return lower.JumpPlayable;

                    if (elapsedTicks < finishStartTick) return null;

                    if (isMoving)
                    {
                        if (canSprint) return lower.SprintPlayable;

                        return lower.RunPlayable;
                    }

                    return lower.IdlePlayable;
                }

            case 2:
                {
                    if (elapsedTicks < cancelStartTick) return null;

                    string primaryId = inventory.PrimaryWeaponID();

                    if (playerMovement.IsJumping) return lower.JumpPlayable;

                    if (elapsedTicks < finishStartTick) return null;

                    if (primaryId == "001") 
                    {
                        if (isMoving)
                        {
                            if (canSprint) return lower.SwordSprintPlayable;

                            return lower.SwordRunPlayable;
                        }
                        return lower.SwordIdlePlayable;
                    } 
                    if (primaryId == "002")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return lower.SpearSprintPlayable;

                            return lower.SpearRunPlayable;
                        }
                        return lower.SpearIdlePlayable;
                    }

                    break;
                }

            case 3:
                {
                    if (elapsedTicks < cancelStartTick) return null;

                    string secondaryId = inventory.SecondaryWeaponID();


                    if (playerMovement.IsJumping) return lower.JumpPlayable;

                    if (elapsedTicks < finishStartTick) return null;

                    if (secondaryId == "003")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return lower.RifleSprintPlayable;

                            return lower.RifleRunPlayable;
                        }

                        return lower.RifleIdlePlayable;
                    }
                    if (secondaryId == "004")
                    {
                        if (isMoving)
                        {
                            if (canSprint) return lower.BowSprintPlayable;

                            return lower.BowRunPlayable;
                        }

                        return lower.BowIdlePlayable;
                    }

                    break;
                }
        }

        if (!characterController.IsGrounded)
            return lower.FallingPlayable;

        return lower.IdlePlayable;
    }
}
