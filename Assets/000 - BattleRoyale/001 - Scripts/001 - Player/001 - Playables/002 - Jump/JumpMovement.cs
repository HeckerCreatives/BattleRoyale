using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using static Fusion.NetworkBehaviour;

public class JumpMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController controller;
    [SerializeField] private HealPlayables heal;

    [Space]
    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private LayerMask groundLayerMask;

    [Space]
    [SerializeField] private float jumpHeight;
    [SerializeField] private float maxSlopeAngle = 45f;
    [SerializeField] private float raycastLengthSlope;

    [Space]
    [SerializeField] private AnimationClip jumpClip;

    [Header("DEBUGGER LOCAL")]
    [SerializeField] private Vector3 groundNormal;

    [field: Header("DEBUGGER")]
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public float JumpImpulse { get; set; }
    [field: MyBox.ReadOnly][field: SerializeField][Networked] public NetworkBool IsJumping { get; set; }

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    //  ============================

    [Networked] public NetworkButtons PreviousButtons { get; set; }

    MyInput controllerInput;

    private ChangeDetector _changeDetector;


    //  ============================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 3);

        var jumpPlayable = AnimationClipPlayable.Create(graph, jumpClip);
        clipPlayables.Add(jumpPlayable);

        graph.Connect(jumpPlayable, 0, movementMixer, 0);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }
    }

    public override void Render()
    {
        if (!HasStateAuthority && movementMixer.IsValid() && !characterController.IsGrounded)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(IsJumping):
                        if (IsJumping)
                        {
                            var jumpPlayable = clipPlayables[0];
                            jumpPlayable.SetTime(0);
                            jumpPlayable.Play();    // Start playing

                            movementMixer.SetInputWeight(0, 1f); // Idle animation active
                        }
                    break;
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        GroundDetector();
        Jump();
    }

    private void GroundDetector()
    {
        groundNormal = Vector3.up;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastLengthSlope, groundLayerMask)) // Adjust length based on character
        {
            groundNormal = hit.normal;
        }
    }

    private bool IsSlopeWalkable(Vector3 normal)
    {
        return Vector3.Dot(Vector3.up, normal) >= Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);
    }

    private void Jump()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        if (HasStateAuthority && controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Jump) && characterController.IsGrounded && !IsJumping && IsSlopeWalkable(groundNormal) && !heal.Healing)
        {
            if (controller.IsCrouch || controller.IsProne)
            {
                controller.IsCrouch = false;
                controller.IsProne = false;
                return;
            }
            IsJumping = true;
            JumpImpulse = jumpHeight;

            var jumpPlayable = clipPlayables[0];
            jumpPlayable.SetTime(0);
            jumpPlayable.Play();    // Start playing

            movementMixer.SetInputWeight(0, 1f); // Idle animation active

            Invoke(nameof(ResetJumping), jumpClip.length);
        }

        PreviousButtons = input.Buttons;
    }

    private void ResetJumping()
    {
        IsJumping = false;
        JumpImpulse = 0f;
    }

    private async void ResetJumpImpulse()
    {
        while (JumpImpulse > 0f)
        {
            JumpImpulse -= Runner.DeltaTime * 5f;
            await Task.Yield();
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

    public Playable GetPlayable()
    {
        return movementMixer;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, new Vector3(0f, -1 + raycastLengthSlope));
    }
}
