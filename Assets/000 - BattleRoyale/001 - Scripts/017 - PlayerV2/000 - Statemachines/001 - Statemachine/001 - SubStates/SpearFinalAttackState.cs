using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class SpearFinalAttackState : PlayerOnGround
{
    public SpearFinalAttackState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        if (playerPlayables.HasInputAuthority) playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.inventory.PrimaryWeapon.SoundController.PlayAttackOne();
            playerPlayables.SlashSpearParticles(0);
        }

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.RotatePlayer();

        playerMovement.AttackMoveSpeed = playerMovement.SpearAttackMoveSpeedFinal;
        playerMovement.SwordStartTick = playerPlayables.Runner.Tick;
        playerMovement.Swording = true;
        //playerMovement.CannotJump = true;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.SlashSpearParticlesStop(0);

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.Swording = false;
        playerMovement.SwordingMove = false;
        playerMovement.Attacking = false;
        //playerMovement.CannotJump = false;
    }

    public override void NetworkUpdate()
    {
        HandleMoveWindow();
        playerMovement.MoveCharacter();

        var nextState = GetNextState();

        if (nextState != null && playablesChanger.CurrentState != nextState)
        {
            playablesChanger.ChangeState(nextState);
        }

        if (playerPlayables.HasStateAuthority)
            playerPlayables.stamina.RecoverStamina(5f);
    }

    private void HandleMoveWindow()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.SwordStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int moveStartTick = Mathf.CeilToInt(totalPunchTicks * 0.2f);
        int moveEndTick = Mathf.CeilToInt(totalPunchTicks * 0.5f);

        playerMovement.SwordingMove = elapsedTicks >= moveStartTick && elapsedTicks <= moveEndTick;
    }

    private AnimationPlayable GetNextState()
    {
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        return GetRecoveryState();
    }

    private AnimationPlayable GetInterruptState()
    {
        if (playerPlayables.healthV2.IsDead)
            return playerPlayables.lowerBodyMovement.DeathPlayable;

        if (!characterController.IsGrounded)
            return playerPlayables.lowerBodyMovement.FallingPlayable;

        return null;
    }

    private AnimationPlayable GetRecoveryState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.SwordStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        bool finishedPunch = elapsedTicks >= finishStartTick;
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;


        //if (playerMovement.IsBlocking)
        //    return playerPlayables.lowerBodyMovement.BlockPlayable;

        if (canRoll)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (isMoving && finishedPunch)
        {
            return playerMovement.IsSprint
                ? playerPlayables.lowerBodyMovement.SpearSprintPlayable
                : playerPlayables.lowerBodyMovement.SpearRunPlayable;
        }

        if (!finishedPunch)
            return null;

        return playerPlayables.lowerBodyMovement.SpearIdlePlayable;
    }
}
