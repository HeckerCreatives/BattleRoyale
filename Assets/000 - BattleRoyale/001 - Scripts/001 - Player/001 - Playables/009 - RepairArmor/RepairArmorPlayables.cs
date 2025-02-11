using Fusion.Addons.SimpleKCC;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Fusion.NetworkBehaviour;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

public class RepairArmorPlayables : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private DeathMovement deathMovement;
    [SerializeField] private HealPlayables healPlayables;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private Image progressCircle;
    [SerializeField] private TextMeshProUGUI statusTMP;
    [SerializeField] private TextMeshProUGUI warningTMP;

    [Space]
    [SerializeField] private AnimationClip repairClip;

    [Header("DEBUGGER")]
    [SerializeField] private float repairWeight;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][SerializeField] public bool Repairing { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  =======================

    MyInput controllerInput;

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    Coroutine warningCoroutine;

    //  =======================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 1);

        var healingPlayable = AnimationClipPlayable.Create(graph, repairClip);
        clipPlayables.Add(healingPlayable);

        graph.Connect(healingPlayable, 0, movementMixer, 0);
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public override void Render()
    {
        if (movementMixer.IsValid() && !HasStateAuthority)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Repairing):
                        if (!Repairing) return;

                        var healingPlayable = clipPlayables[0];
                        healingPlayable.SetTime(0); // Reset time
                        healingPlayable.Play();    // Start playing

                        break;
                }
            }

            AnimationBlend();

            if (HasInputAuthority)
            {
                if (Repairing)
                {
                    progressCircle.gameObject.SetActive(true);
                    progressCircle.fillAmount = (float)((clipPlayables[0].GetDuration() - clipPlayables[0].GetTime()) / clipPlayables[0].GetDuration());
                    statusTMP.text = "Repairing...";
                    statusTMP.gameObject.SetActive(true);
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
        AnimationBlend();
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        DoHealing();

        PreviousButtons = input.Buttons;
    }


    private void AnimationBlend()
    {
        if (movementMixer.IsValid())
        {
            if (deathMovement.IsDead)
            {
                repairWeight = Mathf.Lerp(repairWeight, 0f, Runner.DeltaTime * 4);
                clipPlayables[0].Pause();
            }
            else if (!Repairing)
            {
                repairWeight = Mathf.Lerp(repairWeight, 0f, Runner.DeltaTime * 4);
            }
            else
            {
                repairWeight = Mathf.Lerp(repairWeight, 1f, Runner.DeltaTime * 4);
            }


            movementMixer.SetInputWeight(0, repairWeight); // Idle animation active
        }
    }

    private void DoHealing()
    {

        if (inventory.ArmorRepairCount <= 0) return;

        if (inventory.Shield == null) return;

        if (inventory.Shield.Ammo >= 100f) return;

        if (deathMovement.IsDead) return;

        if (healPlayables.Healing) return;

        if (Repairing) return;

        if (controllerInput.Buttons.IsSet(InputButton.ArmorRepair) && characterController.IsGrounded)
        {
            if (HasInputAuthority && playerController.IsProne)
            {
                warningTMP.text = "Can't repair armor while prone";
                warningTMP.gameObject.SetActive(true);

                if (warningCoroutine != null) StopCoroutine(warningCoroutine);

                warningCoroutine = StartCoroutine(TurnOffWarning());

                return;
            }

            if (!HasStateAuthority) return;

            Repairing = true;

            var healingPlayable = clipPlayables[0];
            healingPlayable.SetTime(0); // Reset time
            healingPlayable.Play();    // Start playing

            Invoke(nameof(RepairPlayer), repairClip.length * 0.6f); // Allow next combo towards the end of Punch1
            Invoke(nameof(ResetRepair), repairClip.length);
        }
    }

    IEnumerator TurnOffWarning()
    {
        yield return new WaitForSeconds(3f);

        warningTMP.gameObject.SetActive(false);
        warningTMP.text = "";
    }

    private void RepairPlayer()
    {
        inventory.ArmorRepairCount -= 1;
        inventory.Shield.Ammo += 25;
    }

    private void ResetRepair()
    {
        Repairing = false;
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
