using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class MainCorePlayable : NetworkBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerController controller;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private Animator animator;
    [SerializeField] private AvatarMask upperBodyMask;
    [SerializeField] private AvatarMask LowerBodyMask;

    [Space]
    [SerializeField] private BasicMovement basicMovement;
    [SerializeField] private BareHandsMovement bareHandsMovement;
    [SerializeField] private JumpMovement jumpMovement;
    [SerializeField] private FallingAnimation fallingAnimation;
    [SerializeField] private CrouchMovement crouchMovement;
    [SerializeField] private ProneMovement proneMovement;
    [SerializeField] private DeathMovement deathMovement;

    [Header("DEBUGGER")]
    [SerializeField] private float basicMovementWeight;
    [SerializeField] private float bareHandWeight;
    [SerializeField] private float jumpMovementWeight;
    [SerializeField] private float fallingWeight;
    [SerializeField] private float crouchWeight;
    [SerializeField] private float proneWeight;
    [SerializeField] private float deadWeight;

    [field: Space]
    [Networked][field: SerializeField] public float TickRateAnimation { get; set; }

    //  =========================

    private PlayableGraph playableGraph;
    private AnimationLayerMixerPlayable mainPlayable;

    private void Start()
    {
        playableGraph = PlayableGraph.Create("CharacterAnimationGraph");

        // Create the Animation Playable Output
        var output = AnimationPlayableOutput.Create(playableGraph, "AnimationOutput", animator);

        // Create the main playable (AnimationLayerMixerPlayable)
        mainPlayable = AnimationLayerMixerPlayable.Create(playableGraph, 7);
        output.SetSourcePlayable(mainPlayable);

        if (basicMovement != null)
        {
            basicMovement.Initialize(playableGraph);
            playableGraph.Connect(basicMovement.GetPlayable(), 0, mainPlayable, 0);
            mainPlayable.SetInputWeight(0, 1f); // Full-body layer weight
        }

        if (bareHandsMovement != null)
        {
            bareHandsMovement.Initialize(playableGraph);
            playableGraph.Connect(bareHandsMovement.GetPlayable(), 0, mainPlayable, 1);
            mainPlayable.SetInputWeight(1, 0f);
            mainPlayable.SetLayerMaskFromAvatarMask(1, upperBodyMask);
        }

        if (jumpMovement != null)
        {
            jumpMovement.Initialize(playableGraph);
            playableGraph.Connect(jumpMovement.GetPlayable(), 0, mainPlayable, 2);
            mainPlayable.SetInputWeight(2, 0f);
        }

        if (fallingAnimation != null)
        {
            fallingAnimation.Initialize(playableGraph);
            playableGraph.Connect(fallingAnimation.GetPlayable(), 0, mainPlayable, 3);
            mainPlayable.SetInputWeight(3, 0f);
        }

        if (crouchMovement != null)
        {
            crouchMovement.Initialize(playableGraph);
            playableGraph.Connect(crouchMovement.GetPlayable(), 0, mainPlayable, 4);
            mainPlayable.SetInputWeight(4, 0f);
            mainPlayable.SetLayerMaskFromAvatarMask(4, LowerBodyMask);
        }

        if (proneMovement != null)
        {
            proneMovement.Initialize(playableGraph);
            playableGraph.Connect(proneMovement.GetPlayable(), 0, mainPlayable, 5);
            mainPlayable.SetInputWeight(5, 0f);
        }

        if (deathMovement != null)
        {
            deathMovement.Initialize(playableGraph);
            playableGraph.Connect(deathMovement.GetPlayable(), 0, mainPlayable, 6);
            mainPlayable.SetInputWeight(6, 0f);
        }

        playableGraph.Play();
    }

    public override void Render()
    {
        if (playableGraph.IsValid())
        {
            //Debug.Log($"Player Grounded: {characterController.IsGrounded}");

            if (!deathMovement.IsDead)
            {

                deadWeight = 0;

                if (characterController.IsGrounded)
                {
                    if (!controller.IsCrouch && !controller.IsProne)
                    {
                        basicMovementWeight = Mathf.Lerp(basicMovementWeight, 1f, Time.deltaTime * 5f);
                        crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                        proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                    }

                    if (!controller.IsProne)
                    {
                        //  Bare Hands
                        if (inventory.WeaponIndex == 1)
                        {
                            bareHandWeight = Mathf.Lerp(bareHandWeight, 1f, Time.deltaTime * 5f);
                        }
                        else
                        {
                            bareHandWeight = Mathf.Lerp(bareHandWeight, 0f, Time.deltaTime * 5f);
                        }

                        if (controller.IsCrouch)
                        {
                            crouchWeight = Mathf.Lerp(crouchWeight, 1f, Time.deltaTime * 5f);
                            proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                            basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                        }
                        else
                        {
                            crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                        }

                        proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                    }
                    else
                    {
                        proneWeight = Mathf.Lerp(proneWeight, 1f, Time.deltaTime * 5f);
                        crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                        basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                        bareHandWeight = Mathf.Lerp(bareHandWeight, 0f, Time.deltaTime * 5f);
                    }

                    jumpMovementWeight = 0;
                    fallingWeight = 0;
                }
                else
                {
                    if (jumpMovement.IsJumping)
                    {
                        jumpMovementWeight = Mathf.Lerp(jumpMovementWeight, 1f, Time.deltaTime * 5f);
                        fallingWeight = Mathf.Lerp(fallingWeight, 0f, Time.deltaTime * 5f);
                    }
                    else
                    {
                        fallingWeight = Mathf.Lerp(fallingWeight, 1f, Time.deltaTime * 5f);
                        jumpMovementWeight = Mathf.Lerp(jumpMovementWeight, 0f, Time.deltaTime * 5f);
                    }
                    basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                    bareHandWeight = Mathf.Lerp(bareHandWeight, 0f, Time.deltaTime * 5f);
                    crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                    proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                }
            }
            else
            {
                basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                bareHandWeight = Mathf.Lerp(bareHandWeight, 0f, Time.deltaTime * 5f);
                crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                jumpMovementWeight = Mathf.Lerp(jumpMovementWeight, 0f, Time.deltaTime * 5f);
                fallingWeight = Mathf.Lerp(fallingWeight, 0f, Time.deltaTime * 5f);
                deadWeight = Mathf.Lerp(deadWeight, 1f, Time.deltaTime * 5f);
            }


            mainPlayable.SetInputWeight(0, basicMovementWeight);
            mainPlayable.SetInputWeight(1, bareHandWeight);
            mainPlayable.SetInputWeight(2, jumpMovementWeight);
            mainPlayable.SetInputWeight(3, fallingWeight);
            mainPlayable.SetInputWeight(4, crouchWeight);
            mainPlayable.SetInputWeight(5, proneWeight);
            mainPlayable.SetInputWeight(6, deadWeight);
        }
    }

    private void OnDisable()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            TickRateAnimation = Runner.Tick * Runner.DeltaTime;
        }
    }
}
