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
    [SerializeField] private SwordPlayable swordPlayable;
    [SerializeField] private SwordBasicMovement swordBasicMovement;
    [SerializeField] private SwordPlayable spearPlayable;
    [SerializeField] private SwordBasicMovement spearBasicMovement;
    [SerializeField] private HealPlayables healPlayables;
    [SerializeField] private RepairArmorPlayables repairArmorPlayables;
    [SerializeField] private BasicMovement rifleBasicMovement;
    [SerializeField] private RifleAttackPlayables rifleAttackPlayables;

    [Header("DEBUGGER")]
    [SerializeField] private float basicMovementWeight;
    [SerializeField] private float bareHandWeight;
    [SerializeField] private float jumpMovementWeight;
    [SerializeField] private float fallingWeight;
    [SerializeField] private float crouchWeight;
    [SerializeField] private float proneWeight;
    [SerializeField] private float deadWeight;
    [SerializeField] private float swordBasicMovementWeight;
    [SerializeField] private float swordWeight;
    [SerializeField] private float spearBasicMovementWeight;
    [SerializeField] private float spearWeight;
    [SerializeField] private float healWeight;
    [SerializeField] private float repairArmorWeight;
    [SerializeField] private float rifleBasicWeight;
    [SerializeField] private float rifleAttackWeight;

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
        mainPlayable = AnimationLayerMixerPlayable.Create(playableGraph, 15);
        output.SetSourcePlayable(mainPlayable);

        if (basicMovement != null)
        {
            basicMovement.Initialize(playableGraph);
            playableGraph.Connect(basicMovement.GetPlayable(), 0, mainPlayable, 0);
            mainPlayable.SetInputWeight(0, 1f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(0, LowerBodyMask);
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

        if (swordPlayable != null)
        {
            swordPlayable.Initialize(playableGraph);
            playableGraph.Connect(swordPlayable.GetPlayable(), 0, mainPlayable, 7);
            mainPlayable.SetInputWeight(7, 0f);
            mainPlayable.SetLayerMaskFromAvatarMask(7, upperBodyMask);
        }

        if (swordBasicMovement != null)
        {
            swordBasicMovement.Initialize(playableGraph);
            playableGraph.Connect(swordBasicMovement.GetPlayable(), 0, mainPlayable, 8);
            mainPlayable.SetInputWeight(8, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(8, LowerBodyMask);
        }


        if (spearPlayable != null)
        {
            spearPlayable.Initialize(playableGraph);
            playableGraph.Connect(spearPlayable.GetPlayable(), 0, mainPlayable, 9);
            mainPlayable.SetInputWeight(9, 0f);
            mainPlayable.SetLayerMaskFromAvatarMask(9, upperBodyMask);
        }

        if (spearBasicMovement != null)
        {
            spearBasicMovement.Initialize(playableGraph);
            playableGraph.Connect(spearBasicMovement.GetPlayable(), 0, mainPlayable, 10);
            mainPlayable.SetInputWeight(10, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(10, LowerBodyMask);
        }

        if (healPlayables != null)
        {
            healPlayables.Initialize(playableGraph);
            playableGraph.Connect(healPlayables.GetPlayable(), 0, mainPlayable, 11);
            mainPlayable.SetInputWeight(11, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(11, upperBodyMask);
        }

        if (repairArmorPlayables != null)
        {
            repairArmorPlayables.Initialize(playableGraph);
            playableGraph.Connect(repairArmorPlayables.GetPlayable(), 0, mainPlayable, 12);
            mainPlayable.SetInputWeight(12, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(12, upperBodyMask);
        }

        if (rifleBasicMovement != null)
        {
            rifleBasicMovement.Initialize(playableGraph);
            playableGraph.Connect(rifleBasicMovement.GetPlayable(), 0, mainPlayable, 13);
            mainPlayable.SetInputWeight(13, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(13, LowerBodyMask);
        }

        if (rifleAttackPlayables != null)
        {
            rifleAttackPlayables.Initialize(playableGraph);
            playableGraph.Connect(rifleAttackPlayables.GetPlayable(), 0, mainPlayable, 14);
            mainPlayable.SetInputWeight(14, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(14, upperBodyMask);
        }

        playableGraph.Play();
    }

    public void Update()
    {
        if (!playableGraph.IsValid()) return;

        if (!deathMovement.IsDead)
        {
            deadWeight = 0;

            if (characterController.IsGrounded)
            {
                // Healing & Repairing
                SetWeight(ref healWeight, healPlayables.Healing);
                SetWeight(ref repairArmorWeight, repairArmorPlayables.Repairing);

                // Lower Body Mask (Non-Crouch & Non-Prone)
                if (!controller.IsCrouch && !controller.IsProne)
                {
                    SetWeaponMovementWeight();
                    SetWeight(ref crouchWeight, false);
                    SetWeight(ref proneWeight, false);
                }

                // Prone Logic
                if (!controller.IsProne)
                {
                    SetWeaponAttackWeight();
                    SetWeight(ref crouchWeight, controller.IsCrouch);
                    SetWeight(ref proneWeight, false);
                }
                else
                {
                    ResetAllMovementWeights();
                    SetWeight(ref proneWeight, true);
                }

                // Reset Jump & Falling Weights
                jumpMovementWeight = 0;
                fallingWeight = 0;
            }
            else // In-Air Logic (Jumping / Falling)
            {
                SetWeight(ref jumpMovementWeight, jumpMovement.IsJumping);
                SetWeight(ref fallingWeight, !jumpMovement.IsJumping);
                SetWeaponAttackWeight();
                ResetAllMovementWeights();
            }
        }
        else // Death Logic
        {
            ResetAllMovementWeights();
            SetWeight(ref deadWeight, true);
        }

        ApplyWeightsToPlayable();
    }

    // Smoothly transitions weight using Lerp
    private void SetWeight(ref float weight, bool isActive, float speed = 5f)
    {
        weight = Mathf.Lerp(weight, isActive ? 1f : 0f, Time.deltaTime * speed);
    }

    // Handles movement blending based on weapon type
    private void SetWeaponMovementWeight()
    {
        switch (inventory.WeaponIndex)
        {
            case 1:
                SetWeight(ref basicMovementWeight, true);
                SetWeight(ref swordBasicMovementWeight, false);
                SetWeight(ref spearBasicMovementWeight, false);
                SetWeight(ref rifleBasicWeight, false);
                break;
            case 2:
                bool isSword = inventory.PrimaryWeapon.WeaponID == "001";
                bool isSpear = inventory.PrimaryWeapon.WeaponID == "002";
                SetWeight(ref swordBasicMovementWeight, isSword);
                SetWeight(ref spearBasicMovementWeight, isSpear);
                SetWeight(ref basicMovementWeight, false);
                SetWeight(ref rifleBasicWeight, false);
                break;
            case 3:
                SetWeight(ref rifleBasicWeight, inventory.SecondaryWeapon.WeaponID == "003");
                SetWeight(ref basicMovementWeight, false);
                break;
        }
    }

    // Handles attack blending based on weapon type
    private void SetWeaponAttackWeight()
    {
        SetWeight(ref bareHandWeight, inventory.WeaponIndex == 1);

        if (inventory.WeaponIndex == 2)
        {
            bool isSword = inventory.PrimaryWeapon.WeaponID == "001";
            bool isSpear = inventory.PrimaryWeapon.WeaponID == "002";
            SetWeight(ref swordWeight, isSword);
            SetWeight(ref spearWeight, isSpear);
        }
        else
        {
            SetWeight(ref swordWeight, false);
            SetWeight(ref spearWeight, false);
        }

        SetWeight(ref rifleAttackWeight, inventory.WeaponIndex == 3 && inventory.SecondaryWeapon.WeaponID == "003");
    }

    // Resets all movement-related weights
    private void ResetAllMovementWeights()
    {
        SetWeight(ref basicMovementWeight, false);
        SetWeight(ref swordBasicMovementWeight, false);
        SetWeight(ref spearBasicMovementWeight, false);
        SetWeight(ref rifleBasicWeight, false);
        SetWeight(ref rifleAttackWeight, false);
        SetWeight(ref bareHandWeight, false);
        SetWeight(ref swordWeight, false);
        SetWeight(ref spearWeight, false);
        SetWeight(ref crouchWeight, false);
        SetWeight(ref proneWeight, false);
    }

    // Applies weights to the playable animation system
    private void ApplyWeightsToPlayable()
    {
        mainPlayable.SetInputWeight(0, basicMovementWeight);
        mainPlayable.SetInputWeight(1, bareHandWeight);
        mainPlayable.SetInputWeight(2, jumpMovementWeight);
        mainPlayable.SetInputWeight(3, fallingWeight);
        mainPlayable.SetInputWeight(4, crouchWeight);
        mainPlayable.SetInputWeight(5, proneWeight);
        mainPlayable.SetInputWeight(6, deadWeight);
        mainPlayable.SetInputWeight(7, swordWeight);
        mainPlayable.SetInputWeight(8, swordBasicMovementWeight);
        mainPlayable.SetInputWeight(9, spearWeight);
        mainPlayable.SetInputWeight(10, spearBasicMovementWeight);
        mainPlayable.SetInputWeight(11, healWeight);
        mainPlayable.SetInputWeight(12, repairArmorWeight);
        mainPlayable.SetInputWeight(13, rifleBasicWeight);
        mainPlayable.SetInputWeight(14, rifleAttackWeight);
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
