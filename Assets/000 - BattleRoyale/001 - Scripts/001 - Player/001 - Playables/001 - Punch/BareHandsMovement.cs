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
    [SerializeField] private HealPlayables healPlayables;
    [SerializeField] private RepairArmorPlayables repairArmorPlayables;
    [SerializeField] private MeleeSoundController meleeSoundController;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip punchOneClip;
    [SerializeField] private AnimationClip punchTwoClip;

    [Space]
    [SerializeField] private AnimationClip crouchPunchOneClip;
    [SerializeField] private AnimationClip crouchPunchTwoClip;

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
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    MyInput controllerInput;

    //  ============================

    public override void Spawned()
    {
        if (HasStateAuthority) CanCombo = true;

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

                                meleeSoundController.PlayAttackOne();
                            }
                            else if (AttackStep == 2)
                            {
                                var punchTwoPlayable = clipPlayables[2];
                                SetAttackTickRate(punchTwoPlayable); // Reset time
                                punchTwoPlayable.Play();    // Start playing

                                meleeSoundController.PlayAttackTwo();
                            }
                        }
                        else
                        {
                            if (AttackStep == 1)
                            {
                                var punchOnePlayable = clipPlayables[3];
                                SetAttackTickRate(punchOnePlayable); // Reset time
                                punchOnePlayable.Play();    // Start playing
                                meleeSoundController.PlayAttackOne();
                            }
                            else if (AttackStep == 2)
                            {
                                var punchTwoPlayable = clipPlayables[4];
                                SetAttackTickRate(punchTwoPlayable); // Reset time
                                punchTwoPlayable.Play();    // Start playing
                                meleeSoundController.PlayAttackTwo();
                            }
                        }
                        break;
                }
            }

            float templength;

            if (playerController.IsCrouch)
            {
                templength = AttackStep == 1 ? crouchPunchOneClip.length : crouchPunchTwoClip.length;
            }
            else
            {
                templength = AttackStep == 1 ? punchOneClip.length : punchTwoClip.length;
            }

            if (mainCorePlayable.TickRateAnimation - LastAttackTime > templength && Attacking)
            {
                var idlePlayable = clipPlayables[0];
                idlePlayable.SetTime(0); // Reset idle animation
                idlePlayable.Play();
            }
        }
    }

    private void Update()
    {
        AnimationBlend();
    }

    public override void FixedUpdateNetwork()
    {
        if (healPlayables.Healing || repairArmorPlayables.Repairing)
        {
            ResetAttackAnimation();

            return;
        }

        if (playerInventory.WeaponIndex == 1)
        {
            if (characterController.IsGrounded)
            {
                InputControlls();
                ResetToIdle();
            }
            else
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

        PreviousButtons = input.Buttons;
    }

    private void AnimationBlend()
    {
        if (Object == null) return;

        if (movementMixer.IsValid())
        {
            if (!playerController.IsCrouch)
            {
                crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 0f, Time.deltaTime * 4);
                crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 0f, Time.deltaTime * 4);

                if (AttackStep == 1)
                {
                    attackOneWeight = Mathf.Lerp(attackOneWeight, 1f, Time.deltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4f);
                    attackTwoWeight = Mathf.Lerp(attackTwoWeight, 0f, Time.deltaTime * 4);
                }
                else if (AttackStep == 2)
                {
                    attackTwoWeight = Mathf.Lerp(attackTwoWeight, 1f, Time.deltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                    attackOneWeight = Mathf.Lerp(attackOneWeight, 0f, Time.deltaTime * 4);
                }
                else
                {
                    idleWeight = Mathf.Lerp(idleWeight, 1f, Time.deltaTime * 4f);
                    attackOneWeight = Mathf.Lerp(attackOneWeight, 0f, Time.deltaTime * 4f);
                    attackTwoWeight = Mathf.Lerp(attackTwoWeight, 0f, Time.deltaTime * 4f);
                }
            }
            else
            {
                attackOneWeight = Mathf.Lerp(attackOneWeight, 0f, Time.deltaTime * 4);
                attackTwoWeight = Mathf.Lerp(attackTwoWeight, 0f, Time.deltaTime * 4);

                if (AttackStep == 1)
                {
                    crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 1f, Time.deltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                    crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 0f, Time.deltaTime * 4);
                }
                else if (AttackStep == 2)
                {
                    crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 1f, Time.deltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                    crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 0f, Time.deltaTime * 4);
                }
                else
                {
                    idleWeight = Mathf.Lerp(idleWeight, 1f, Time.deltaTime * 4);
                    crouchAttackOneWeight = Mathf.Lerp(crouchAttackOneWeight, 0f, Time.deltaTime * 4);
                    crouchAttackTwoWeight = Mathf.Lerp(crouchAttackTwoWeight, 0f, Time.deltaTime * 4);
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

        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Melee) && CanCombo && characterController.IsGrounded && !healPlayables.Healing && !repairArmorPlayables.Repairing && !playerController.IsProne)
        {
            if (AttackStep <= 2)
            {
                Attacking = true;
                LastAttackTime = Runner.Tick * Runner.DeltaTime;
                HandleComboInput();
            }
        }
    }


    private void AttackFirstDamage()
    {
        if (!HasStateAuthority) return;

        if (deathMovement.IsDead) return;

        fistWeaponHandler.PerformFirstAttack();
    }

    private void AttackSecondDamage()
    {
        if (!HasStateAuthority) return;

        if (deathMovement.IsDead) return;

        fistWeaponHandler.PerformSecondAttack();
    }

    private void HandleComboInput()
    {
        if (!HasStateAuthority) return;

        if (healPlayables.Healing) return;

        if (repairArmorPlayables.Repairing) return;

        if (AttackStep == 0 || CanCombo)
        {
            AttackStep++;

            if (AttackStep > 2)
                AttackStep = 1;

            CanCombo = false;

            if (!playerController.IsCrouch)
            {
                if (AttackStep == 1)
                {
                    ResetFirstAttack();

                    var punchOnePlayable = clipPlayables[1];
                    SetAttackTickRate(punchOnePlayable); // Reset time
                    punchOnePlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), punchOneClip.length * 0.8f); // Allow next combo towards the end of Punch1
                    Invoke(nameof(AttackFirstDamage), punchOneClip.length * 0.25f);
                    Invoke(nameof(ResetFirstAttack), punchOneClip.length);
                }
                else if (AttackStep == 2)
                {
                    ResetSecondAttack();

                    var punchTwoPlayable = clipPlayables[2];
                    SetAttackTickRate(punchTwoPlayable); // Reset time
                    punchTwoPlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), punchTwoClip.length * 0.6f); // Allow next combo towards the end of Punch2
                    Invoke(nameof(AttackSecondDamage), punchTwoClip.length * 0.35f);
                    Invoke(nameof(ResetSecondAttack), punchTwoClip.length); // Reset to Idle after Punch2
                }
            }
            else
            {
                if (AttackStep == 1)
                {
                    ResetFirstAttack();

                    var punchOnePlayable = clipPlayables[3];
                    SetAttackTickRate(punchOnePlayable); // Reset time
                    punchOnePlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), crouchPunchOneClip.length * 0.8f); // Allow next combo towards the end of Punch1
                    Invoke(nameof(AttackFirstDamage), crouchPunchOneClip.length * 0.6f);
                    Invoke(nameof(ResetFirstAttack), crouchPunchOneClip.length);
                }
                else if (AttackStep == 2)
                {
                    ResetSecondAttack();

                    var punchTwoPlayable = clipPlayables[4];
                    SetAttackTickRate(punchTwoPlayable); // Reset time
                    punchTwoPlayable.Play();    // Start playing

                    Invoke(nameof(AllowNextCombo), crouchPunchTwoClip.length * 0.8f); // Allow next combo towards the end of Punch2
                    Invoke(nameof(AttackSecondDamage), crouchPunchTwoClip.length * 0.55f);
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

    private void ResetFirstAttack() => fistWeaponHandler.ResetFirstAttack();

    private void ResetSecondAttack() => fistWeaponHandler.ResetSecondAttack();

    private void ResetAttackAnimation()
    {
        if (characterController.IsGrounded) return;

        AttackStep = 0;
        CanCombo = true;
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

        float templength;

        if (playerController.IsCrouch)
        {
            templength = AttackStep == 1 ? crouchPunchOneClip.length : crouchPunchTwoClip.length;
        }
        else
        {
            templength = AttackStep == 1 ? punchOneClip.length : punchTwoClip.length;
        }

        if (mainCorePlayable.TickRateAnimation - LastAttackTime > templength && Attacking)
        {
            Attacking = false;
            AttackStep = 0;
            CanCombo = true;

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
