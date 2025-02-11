using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class TrapPlayables : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerNetworkLoader playerNetworkLoader;
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private DeathMovement deathMovement;
    [SerializeField] private RepairArmorPlayables repairArmorPlayables;
    [SerializeField] private HealPlayables healPlayables;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private LayerMask raycastLayerMask;
    [SerializeField] private NetworkObject trapObject;
    [SerializeField] private float detectorLength;

    [Space]
    [SerializeField] private Transform trapDetectorTF;

    [Space]
    [SerializeField] private AnimationClip trapClip;

    [Header("DEBUGGER")]
    [SerializeField] private float trapWeight;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][SerializeField] public bool SetTrap { get; set; }
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  =======================

    MyInput controllerInput;

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    LagCompensatedHit hit;

    //  =======================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 1);

        var trapPlayable = AnimationClipPlayable.Create(graph, trapClip);
        clipPlayables.Add(trapPlayable);

        graph.Connect(trapPlayable, 0, movementMixer, 0);
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
                    case nameof(SetTrap):
                        if (!SetTrap) return;

                        var healingPlayable = clipPlayables[0];
                        healingPlayable.SetTime(0); // Reset time
                        healingPlayable.Play();    // Start playing

                        break;
                }
            }

            AnimationBlend();
        }
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    private void Update()
    {
        AnimationBlend();
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        DoSetupTrap();

        PreviousButtons = input.Buttons;
    }


    private void AnimationBlend()
    {
        if (Object == null) return;

        if (movementMixer.IsValid())
        {
            if (deathMovement.IsDead)
            {
                trapWeight = Mathf.MoveTowards(trapWeight, 0f, Time.deltaTime * 4);
                clipPlayables[0].Pause();
            }
            else if (!SetTrap)
            {
                trapWeight = Mathf.MoveTowards(trapWeight, 0f, Time.deltaTime * 4);
            }
            else
            {
                trapWeight = Mathf.MoveTowards(trapWeight, 1f, Time.deltaTime * 4);
            }


            movementMixer.SetInputWeight(0, trapWeight); // Idle animation active
        }
    }

    private void DoSetupTrap()
    {
        if (health.CurrentHealth <= 0f) return;

        if (inventory.TrapCount <= 0) return;

        if (deathMovement.IsDead) return;

        if (repairArmorPlayables.Repairing) return;

        if (healPlayables.Healing) return;

        if (SetTrap) return;

        if (controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.SwitchTrap) && characterController.IsGrounded)
        {
            if (!HasStateAuthority) return;

            if (Runner.LagCompensation.Raycast(trapDetectorTF.transform.position, Vector3.down, detectorLength, Object.InputAuthority, out hit, raycastLayerMask, HitOptions.IncludePhysX))
            {
                Debug.Log($"Trap detected? {hit.GameObject == null}");

                if (hit.GameObject == null) return;

                Debug.Log($"Trap placed by {playerNetworkLoader.Username}");

                Runner.Spawn(trapObject, hit.Point, Quaternion.Euler(hit.Normal), Object.InputAuthority, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) => {
                    obj.GetComponent<TrapWeaponController>().Initialize(playerNetworkLoader.Username, hit.Normal);
                });

                SetTrap = true;

                var trapPlayable = clipPlayables[0];
                trapPlayable.SetTime(0); // Reset time
                trapPlayable.Play();    // Start playing


                inventory.TrapCount -= 1;

                Invoke(nameof(ResetHeal), trapClip.length);
            }

        }
    }

    private void PutTrap()
    {
    }

    private void ResetHeal()
    {
        SetTrap = false;
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Vector3 start = trapDetectorTF.transform.position;
        Vector3 end = start + Vector3.down * detectorLength; // Offset correctly
        Gizmos.DrawLine(start, end);
    }
}
