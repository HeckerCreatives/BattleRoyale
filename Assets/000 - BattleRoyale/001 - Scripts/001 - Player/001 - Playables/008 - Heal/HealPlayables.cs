using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UI;

public class HealPlayables : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private DeathMovement deathMovement;
    [SerializeField] private RepairArmorPlayables repairArmorPlayables;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private Image progressCircle;
    [SerializeField] private TextMeshProUGUI statusTMP;
    [SerializeField] private TextMeshProUGUI warningTMP;

    [Space]
    [SerializeField] private AnimationClip healClip;

    [Header("DEBUGGER")]
    [SerializeField] private float healWeight;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][SerializeField] public bool Healing { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  =======================

    MyInput controllerInput;

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    //  =======================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 1);

        var healingPlayable = AnimationClipPlayable.Create(graph, healClip);
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
                    case nameof(Healing):
                        if (!Healing) return;

                        var healingPlayable = clipPlayables[0];
                        healingPlayable.SetTime(0); // Reset time
                        healingPlayable.Play();    // Start playing

                        break;
                }
            }

            AnimationBlend();

            if (HasInputAuthority)
            {
                if (Healing)
                {
                    progressCircle.gameObject.SetActive(true);
                    progressCircle.fillAmount = (float)((clipPlayables[0].GetDuration() - clipPlayables[0].GetTime()) / clipPlayables[0].GetDuration());
                    statusTMP.text = "Healing...";
                    statusTMP.gameObject.SetActive(true);
                }
                else if (!Healing && !repairArmorPlayables.Repairing)
                {
                    progressCircle.gameObject.SetActive(false);
                    statusTMP.gameObject.SetActive(false);
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
                healWeight = Mathf.Lerp(healWeight, 0f, Runner.DeltaTime * 4);
                clipPlayables[0].Pause();
            }
            else if (!Healing)
            {
                healWeight = Mathf.Lerp(healWeight, 0f, Runner.DeltaTime * 4);
            }
            else
            {
                healWeight = Mathf.Lerp(healWeight, 1f, Runner.DeltaTime * 4);
            }


            movementMixer.SetInputWeight(0, healWeight); // Idle animation active
        }
    }

    private void DoHealing()
    {

        if (health.CurrentHealth >= 100f) return;

        if (inventory.HealCount <= 0) return;

        if (deathMovement.IsDead) return;

        if (repairArmorPlayables.Repairing) return;

        if (Healing) return;

        if (playerController.IsProne)
        {
            warningTMP.text = "Can't heal while prone";
            warningTMP.gameObject.SetActive(true);
            Invoke(nameof(TurnOffWarning), 3f);
            return;
        }
        else
        {
            if (controllerInput.Buttons.IsSet(InputButton.Heal) && characterController.IsGrounded)
            {
                Healing = true;

                var healingPlayable = clipPlayables[0];
                healingPlayable.SetTime(0); // Reset time
                healingPlayable.Play();    // Start playing

                Invoke(nameof(HealPlayer), healClip.length * 0.6f); // Allow next combo towards the end of Punch1
                Invoke(nameof(ResetHeal), healClip.length);
            }
        }
    }


    private void TurnOffWarning()
    {
        warningTMP.gameObject.SetActive(false);
        warningTMP.text = "";
    }

    private void HealPlayer()
    {
        inventory.HealCount -= 1;
        health.CurrentHealth += 25f;
    }

    private void ResetHeal()
    {
        Healing = false;
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
