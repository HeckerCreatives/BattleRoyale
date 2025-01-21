using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BareHandsMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private FistWeaponHandler fistWeaponHandler;
    [SerializeField] private DeathMovement deathMovement;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip punchOneClip;
    [SerializeField] private AnimationClip punchTwoClip;

    [Space]
    [SerializeField] private AnimationClip crouchPunchOneClip;
    [SerializeField] private AnimationClip crouchPunchTwoClip;

    [Space]
    [SerializeField] private List<float> comboResetTime;

    [Header("DEBUGGER LOCAL")]
    [SerializeField] private float idleWeight;
    [SerializeField] private float attackOneWeight;
    [SerializeField] private float attackTwoWeight;
    [SerializeField] private float crouchAttackOneWeight;
    [SerializeField] private float crouchAttackTwoWeight;

    [field: Header("DEBUGGER NETWORK")]
    [Networked][field: SerializeField] public bool Attacking { get; set; }
    [Networked][field: SerializeField] public int AttackStep { get; set; } // Current combo step
    [Networked][field: SerializeField] public float LastAttackTime { get; set; } // Time when the last attack was performed
    [Networked][field: SerializeField] public bool CanCombo { get; set; }
    [field: SerializeField] public bool CanAttack { get; set; }
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
        if (movementMixer.IsValid() && !HasStateAuthority && characterController.IsGrounded && playerInventory.WeaponIndex == 1)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(AttackStep):
                        if (!playerController.IsCrouch)
                        {
                            if (AttackStep == 1)
                            {
                                var punchOnePlayable = clipPlayables[1];
                                SetAttackTickRate(punchOnePlayable); // Reset time
                                punchOnePlayable.Play();    // Start playing
                            }
                            else if (AttackStep == 2)
                            {
                                var punchTwoPlayable = clipPlayables[2];
                                SetAttackTickRate(punchTwoPlayable); // Reset time
                                punchTwoPlayable.Play();    // Start playing
                            }
                        }
                        else
                        {
                            if (AttackStep == 1)
                            {
                                var punchOnePlayable = clipPlayables[3];
                                SetAttackTickRate(punchOnePlayable); // Reset time
                                punchOnePlayable.Play();    // Start playing
                            }
                            else if (AttackStep == 2)
                            {
                                var punchTwoPlayable = clipPlayables[4];
                                SetAttackTickRate(punchTwoPlayable); // Reset time
                                punchTwoPlayable.Play();    // Start playing
                            }
                        }
                        break;
                }
            }

            if (mainCorePlayable.TickRateAnimation - LastAttackTime > comboResetTime[AttackStep <= 0 ? 0 : AttackStep - 1] && Attacking)
            {
                var idlePlayable = clipPlayables[0];
                idlePlayable.SetTime(0); // Reset idle animation
                idlePlayable.Play();
            }

            AnimationBlend();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (playerInventory.WeaponIndex == 1)
        {
            if (characterController.IsGrounded)
            {
                InputControlls();
                ResetToIdle();
                AnimationBlend();
            }

            ResetAttackAnimation();
        }
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 5);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var punchOnePlayable = AnimationClipPlayable.Create(graph, punchOneClip);
        clipPlayables.Add(punchOnePlayable);

        var punchTwoPlayable = AnimationClipPlayable.Create(graph, punchTwoClip);
        clipPlayables.Add(punchTwoPlayable);

        var punchOneCrouchPlayable = AnimationClipPlayable.Create(graph, crouchPunchOneClip);
        clipPlayables.Add(punchOneCrouchPlayable);

        var punchCrouchTwoPlayable = AnimationClipPlayable.Create(graph, crouchPunchTwoClip);
        clipPlayables.Add(punchCrouchTwoPlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(punchOnePlayable, 0, movementMixer, 1);
        graph.Connect(punchTwoPlayable, 0, movementMixer, 2);
        graph.Connect(punchOneCrouchPlayable, 0, movementMixer, 3);
        graph.Connect(punchCrouchTwoPlayable, 0, movementMixer, 4);

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
        AttackDamage();

        PreviousButtons = input.Buttons;
    }

    private void AnimationBlend()
    {
        if (movementMixer.IsValid())
        {
            if (!playerController.IsCrouch)
            {
                crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 0f, Runner.DeltaTime * 4);
                crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 0f, Runner.DeltaTime * 4);

                if (AttackStep == 1)
                {
                    attackOneWeight = Mathf.Lerp(attackOneWeight, 1f, Runner.DeltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Runner.DeltaTime * 4f);
                    attackTwoWeight = Mathf.Lerp(attackTwoWeight, 0f, Runner.DeltaTime * 4);
                }
                else if (AttackStep == 2)
                {
                    attackTwoWeight = Mathf.Lerp(attackTwoWeight, 1f, Runner.DeltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Runner.DeltaTime * 4);
                    attackOneWeight = Mathf.Lerp(attackOneWeight, 0f, Runner.DeltaTime * 4);
                }
                else
                {
                    idleWeight = Mathf.Lerp(idleWeight, 1f, Runner.DeltaTime * 4f);
                    attackOneWeight = Mathf.Lerp(attackOneWeight, 0f, Runner.DeltaTime * 4f);
                    attackTwoWeight = Mathf.Lerp(attackTwoWeight, 0f, Runner.DeltaTime * 4f);
                }
            }
            else
            {
                attackOneWeight = Mathf.Lerp(attackOneWeight, 0f, Runner.DeltaTime * 4);
                attackTwoWeight = Mathf.Lerp(attackTwoWeight, 0f, Runner.DeltaTime * 4);

                if (AttackStep == 1)
                {
                    crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 1f, Runner.DeltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Runner.DeltaTime * 4);
                    crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 0f, Runner.DeltaTime * 4);
                }
                else if (AttackStep == 2)
                {
                    crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 1f, Runner.DeltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Runner.DeltaTime * 4);
                    crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 0f, Runner.DeltaTime * 4);
                }
                else
                {
                    idleWeight = Mathf.Lerp(idleWeight, 1f, Runner.DeltaTime * 4);
                    crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 0f, Runner.DeltaTime * 4);
                    crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 0f, Runner.DeltaTime * 4);
                }
            }


            movementMixer.SetInputWeight(0, idleWeight); // Idle animation active
            movementMixer.SetInputWeight(1, attackOneWeight);
            movementMixer.SetInputWeight(2, attackTwoWeight);
            movementMixer.SetInputWeight(3, crouchAttackOneWeight);
            movementMixer.SetInputWeight(4, crouchAttackTwoWeight);
        }
    }

    private void Attack()
    {
        if (playerInventory.WeaponIndex != 1)
            return;

        if (controllerInput.Buttons.IsSet(InputButton.Melee) && characterController.IsGrounded)
        {
            if (AttackStep <= 2)
            {
                Attacking = true;
                LastAttackTime = Runner.Tick * Runner.DeltaTime;
                HandleComboInput();
            }
        }
    }

    private void AttackDamage()
    {
        if (!HasStateAuthority) return;

        if (!CanAttack) return;

        if (deathMovement.IsDead) return;

        if (AttackStep == 1) fistWeaponHandler.PerformFirstAttack();
        else if (AttackStep == 2) fistWeaponHandler.PerformSecondAttack();
    }

    private void HandleComboInput()
    {
        if (!HasStateAuthority) return;

        if (AttackStep == 0 || CanCombo)
        {
            AttackStep++;

            if (AttackStep > 2)
                AttackStep = 1;

            CanCombo = false;

            CanAttack = false;

            if (!playerController.IsCrouch)
            {
                if (AttackStep == 1)
                {
                    var punchOnePlayable = clipPlayables[1];
                    SetAttackTickRate(punchOnePlayable); // Reset time
                    punchOnePlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), punchOneClip.length * 0.8f); // Allow next combo towards the end of Punch1
                    Invoke(nameof(CanNowAttack), punchOneClip.length * 0.3f);
                    Invoke(nameof(ResetFirstAttack), punchOneClip.length);
                }
                else if (AttackStep == 2)
                {
                    var punchTwoPlayable = clipPlayables[2];
                    SetAttackTickRate(punchTwoPlayable); // Reset time
                    punchTwoPlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), punchTwoClip.length * 0.6f); // Allow next combo towards the end of Punch2
                    Invoke(nameof(CanNowAttack), punchTwoClip.length * 0.3f);
                    Invoke(nameof(ResetSecondAttack), punchTwoClip.length); // Reset to Idle after Punch2
                }
            }
            else
            {
                if (AttackStep == 1)
                {
                    var punchOnePlayable = clipPlayables[3];
                    SetAttackTickRate(punchOnePlayable); // Reset time
                    punchOnePlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), crouchPunchOneClip.length * 0.8f); // Allow next combo towards the end of Punch1
                    Invoke(nameof(CanNowAttack), crouchPunchOneClip.length * 0.54f);
                    Invoke(nameof(ResetFirstAttack), crouchPunchOneClip.length);
                }
                else if (AttackStep == 2)
                {
                    var punchTwoPlayable = clipPlayables[4];
                    SetAttackTickRate(punchTwoPlayable); // Reset time
                    punchTwoPlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), crouchPunchTwoClip.length * 0.8f); // Allow next combo towards the end of Punch2
                    Invoke(nameof(CanNowAttack), crouchPunchTwoClip.length * 0.5f);
                    Invoke(nameof(ResetSecondAttack), crouchPunchTwoClip.length); // Reset to Idle after Punch2
                }
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

    private IEnumerator PerformCanAttack(int delayInTicks)
    {
        int targetTick = Runner.Tick + delayInTicks;

        while (Runner.Tick < targetTick)
        {
            yield return null;
        }

        CanAttack = true;
    }

    private void ResetFirstAttack() => fistWeaponHandler.ResetFirstAttack();

    private void ResetSecondAttack() => fistWeaponHandler.ResetSecondAttack();

    private void CanNowAttack() => CanAttack = true;

    private void ResetAttackAnimation()
    {
        if (characterController.IsGrounded) return;

        AttackStep = 0;
        CanAttack = false;
        CanCombo = false;
        Attacking = false;
    }

    private void AllowNextCombo()
    {
        CanCombo = true;
    }

    private void ResetToIdle()
    {
        if (!HasStateAuthority) return;

        if (!movementMixer.IsValid()) return;

        if (mainCorePlayable.TickRateAnimation - LastAttackTime > comboResetTime[AttackStep <= 0 ? 0 : AttackStep - 1] && Attacking)
        {
            Attacking = false;
            AttackStep = 0;
            CanCombo = false;
            CanAttack = false;

            var idlePlayable = clipPlayables[0];
            idlePlayable.SetTime(0); // Reset idle animation
            idlePlayable.Play();
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
