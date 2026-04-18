using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class PunchState : PlayerOnGround
{
    public PunchState(MonoBehaviour host, SimpleKCC characterController, PlayablesChanger playablesChanger, PlayerMovementV2 playerMovement, PlayerPlayables playerPlayables, AnimationMixerPlayable mixerAnimations, List<string> animations, List<string> mixers, string animationname, string mixername, float animationLength, AnimationClipPlayable animationClipPlayable, bool oncePlay, bool isLower) : base(host, characterController, playablesChanger, playerMovement, playerPlayables, mixerAnimations, animations, mixers, animationname, mixername, animationLength, animationClipPlayable, oncePlay, isLower)
    {
    }

    public override void Enter()
    {
        base.Enter();

        playerMovement.AttackMoveSpeed = playerMovement.AttackMoveSpeedOne;

        if (playerPlayables.HasInputAuthority) playerMovement.AnimationTick = playerPlayables.Runner.Tick;

        if (!playerPlayables.HasStateAuthority)
        {
            playerPlayables.fistSoundController.PlayAttackOne();
            playerPlayables.SlashPunchParticles(0);
        }

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.RotatePlayer();

        playerMovement.PunchStartTick = playerPlayables.Runner.Tick;
        playerMovement.Punching = true;
        //playerMovement.CannotJump = true;
    }

    public override void Exit()
    {
        base.Exit();

        playerPlayables.SlashPunchParticlesStop(0);

        if (!playerPlayables.HasStateAuthority) return;

        playerMovement.Punching = false;
        playerMovement.PunchingMove = false;
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
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.PunchStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int moveStartTick = Mathf.CeilToInt(totalPunchTicks * 0.3f);
        int moveEndTick = Mathf.CeilToInt(totalPunchTicks * 0.6f);

        playerMovement.PunchingMove = elapsedTicks >= moveStartTick && elapsedTicks <= moveEndTick;
    }

    private AnimationPlayable GetNextState()
    {
        var interruptState = GetInterruptState();
        if (interruptState != null)
            return interruptState;

        var comboState = GetComboState();
        if (comboState != null)
            return comboState;

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

    private AnimationPlayable GetComboState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.PunchStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int comboWindowStartTick = Mathf.CeilToInt(totalPunchTicks * 0.8f);
        int comboWindowEndTick = Mathf.CeilToInt(totalPunchTicks * 0.95f);

        bool comboWindow = elapsedTicks >= comboWindowStartTick && elapsedTicks <= comboWindowEndTick;

        if (comboWindow && playerMovement.Attacking)
            return playerPlayables.lowerBodyMovement.Punch2Playable;

        return null;
    }

    private AnimationPlayable GetRecoveryState()
    {
        int currentTick = playerPlayables.Runner.Tick;
        int elapsedTicks = currentTick - (playerPlayables.HasStateAuthority ? playerMovement.PunchStartTick : playerMovement.AnimationTick);

        int totalPunchTicks = Mathf.CeilToInt((float)(animationLength / playerPlayables.Runner.DeltaTime));
        int finishStartTick = Mathf.CeilToInt(totalPunchTicks * 0.9f);

        bool finishedPunch = elapsedTicks >= finishStartTick;
        bool isMoving = playerMovement.XMovement != 0 || playerMovement.YMovement != 0;
        bool canRoll = playerMovement.IsRoll && playerPlayables.stamina.Stamina >= 35f;

        if (canRoll)
            return playerPlayables.lowerBodyMovement.RollPlayable;

        if (isMoving && finishedPunch)
        {
            // ? Wait until punch momentum blend is fully done before switching to run
            if (playerMovement.WasPunchingMoveLastTick)
                return null; // hold on idle/punch end until movement catches up

            return playerMovement.IsSprint
                ? playerPlayables.lowerBodyMovement.SprintPlayable
                : playerPlayables.lowerBodyMovement.RunPlayable;
        }

        if (!finishedPunch)
            return null;

        return playerPlayables.lowerBodyMovement.IdlePlayable;
    }
}
