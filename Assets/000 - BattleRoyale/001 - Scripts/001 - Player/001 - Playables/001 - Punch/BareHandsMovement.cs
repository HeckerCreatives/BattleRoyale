using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BareHandsMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private FistWeaponHandler fistWeaponHandler;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip punchOneClip;
    [SerializeField] private AnimationClip punchTwoClip;

    [Space]
    [SerializeField] private float comboResetTime = 1f;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] private bool Attacking { get; set; }
    [Networked][field: SerializeField] private int AttackStep { get; set; } // Current combo step
    [Networked][field: SerializeField] private float LastAttackTime { get; set; } // Time when the last attack was performed
    [Networked][field: SerializeField] private bool CanCombo { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    MyInput controllerInput;

    //  ============================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        if (!HasInputAuthority && !HasStateAuthority)
        {
            if (movementMixer.IsValid())
            {

                foreach (var change in _changeDetector.DetectChanges(this))
                {
                    switch (change)
                    {
                        case nameof(AttackStep):
                            movementMixer.SetInputWeight(0, 0f);
                            if (AttackStep == 1)
                            {
                                var punchOnePlayable = clipPlayables[1];
                                SetAttackTickRate(punchOnePlayable); // Reset time
                                punchOnePlayable.Play();    // Start playing
                                movementMixer.SetInputWeight(2, 0f);
                                movementMixer.SetInputWeight(1, 1f);
                            }
                            else if (AttackStep == 2)
                            {
                                var punchTwoPlayable = clipPlayables[2];
                                SetAttackTickRate(punchTwoPlayable); // Reset time
                                punchTwoPlayable.Play();    // Start playing
                                movementMixer.SetInputWeight(1, 0f);
                                movementMixer.SetInputWeight(2, 1f);
                            }
                            break;
                    }
                }

                if (mainCorePlayable.TickRateAnimation - LastAttackTime > comboResetTime && Attacking)
                {
                    movementMixer.SetInputWeight(0, 1f); // Idle animation active
                    movementMixer.SetInputWeight(1, 0f);
                    movementMixer.SetInputWeight(2, 0f);

                    var idlePlayable = clipPlayables[0];
                    idlePlayable.SetTime(0); // Reset idle animation
                    idlePlayable.Play();
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
        ResetToIdle();
    }


    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 3);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var punchOnePlayable = AnimationClipPlayable.Create(graph, punchOneClip);
        clipPlayables.Add(punchOnePlayable);

        var punchTwoPlayable = AnimationClipPlayable.Create(graph, punchTwoClip);
        clipPlayables.Add(punchTwoPlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(punchOnePlayable, 0, movementMixer, 1);
        graph.Connect(punchTwoPlayable, 0, movementMixer, 2);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Attack();

        PreviousButtons = input.Buttons;
    }

    private void Attack()
    {
        if (playerInventory.WeaponIndex != 1)
        {
            movementMixer.SetInputWeight(0, 0f);
            movementMixer.SetInputWeight(1, 0f);
            movementMixer.SetInputWeight(2, 0f);
            return;
        }

        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Melee))
        {
            if (AttackStep < 2)
            {
                Attacking = true;
                LastAttackTime = Runner.Tick * Runner.DeltaTime;
                HandleComboInput();
            }
        }
    }

    private void HandleComboInput()
    {
        movementMixer.SetInputWeight(0, 0f);

        if (AttackStep == 0 || CanCombo)
        {
            AttackStep++;

            if (AttackStep > 2)
                AttackStep = 1;

            CanCombo = false; // Prevent early input for the next combo

            if (AttackStep == 1)
            {
                var punchOnePlayable = clipPlayables[1];
                SetAttackTickRate(punchOnePlayable); // Reset time
                punchOnePlayable.Play();    // Start playing
                movementMixer.SetInputWeight(1, 1f);
                movementMixer.SetInputWeight(2, 0f);

                Invoke(nameof(AllowNextCombo), punchOneClip.length * 0.8f); // Allow next combo towards the end of Punch1
                Invoke(nameof(PerformFirstAttack), punchOneClip.length * 0.3f);
                Invoke(nameof(ResetFirstAttack), punchOneClip.length);
            }
            else if (AttackStep == 2)
            {
                var punchTwoPlayable = clipPlayables[2];
                SetAttackTickRate(punchTwoPlayable); // Reset time
                punchTwoPlayable.Play();    // Start playing
                movementMixer.SetInputWeight(2, 1f);
                movementMixer.SetInputWeight(1, 0f);

                Invoke(nameof(PerformSecondAttack), punchTwoClip.length * 0.3f);
                Invoke(nameof(ResetSecondAttack), punchTwoClip.length); // Reset to Idle after Punch2
            }
        }
    }

    private void SetAttackTickRate(AnimationClipPlayable playables)
    {
        double syncedTime = mainCorePlayable.TickRateAnimation % playables.GetAnimationClip().length;

        // Get the current playable time
        double currentPlayableTime = playables.GetTime();

        // Check if the time difference exceeds the threshold
        if (Mathf.Abs((float)(currentPlayableTime - syncedTime)) > 5f)
        {
            // If the animation is looping or still playing, set the time
            if (currentPlayableTime < playables.GetAnimationClip().length)
            {
                playables.SetTime(syncedTime);
            }
            else
            {
                playables.SetTime(0);
            }
        }
        else
        {
            playables.SetTime(0);
        }
    }

    private void PerformFirstAttack() => fistWeaponHandler.PerformFirstAttack();

    private void ResetFirstAttack() => fistWeaponHandler.ResetFirstAttack();

    private void PerformSecondAttack() => fistWeaponHandler.PerformSecondAttack();

    private void ResetSecondAttack() => fistWeaponHandler.ResetSecondAttack();

    private void AllowNextCombo()
    {
        CanCombo = true;
    }

    private void ResetToIdle()
    {
        if (!movementMixer.IsValid()) return;

        if (!Attacking)
        {
            movementMixer.SetInputWeight(0, 1f); // Idle animation active
            movementMixer.SetInputWeight(1, 0f);
            movementMixer.SetInputWeight(2, 0f);
        }

        if (mainCorePlayable.TickRateAnimation - LastAttackTime > comboResetTime && Attacking)
        {
            Attacking = false;
            AttackStep = 0;
            CanCombo = false;

            var idlePlayable = clipPlayables[0];
            idlePlayable.SetTime(0); // Reset idle animation
            idlePlayable.Play();

            movementMixer.SetInputWeight(0, 1f); // Idle animation active
            movementMixer.SetInputWeight(1, 0f);
            movementMixer.SetInputWeight(2, 0f);
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
