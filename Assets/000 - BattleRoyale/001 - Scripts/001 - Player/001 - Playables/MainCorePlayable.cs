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
    [SerializeField] private BasicMovement bowBasicMovement;
    [SerializeField] private BowAttackPlayable bowAttackPlayables;

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
    [SerializeField] private float bowBasicWeight;
    [SerializeField] private float bowAttackWeight;

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
        mainPlayable = AnimationLayerMixerPlayable.Create(playableGraph, 17);
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

        if (bowBasicMovement != null)
        {
            bowBasicMovement.Initialize(playableGraph);
            playableGraph.Connect(bowBasicMovement.GetPlayable(), 0, mainPlayable, 15);
            mainPlayable.SetInputWeight(15, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(15, LowerBodyMask);
        }

        if (bowAttackPlayables != null)
        {
            bowAttackPlayables.Initialize(playableGraph);
            playableGraph.Connect(bowAttackPlayables.GetPlayable(), 0, mainPlayable, 16);
            mainPlayable.SetInputWeight(16, 0f); // Full-body layer weight
            mainPlayable.SetLayerMaskFromAvatarMask(16, upperBodyMask);
        }

        playableGraph.Play();
    }

    public void Update()
    {
        if (playableGraph.IsValid())
        {
            if (!deathMovement.IsDead)
            {

                deadWeight = 0;
                if (characterController.IsGrounded)
                {
                    if (healPlayables.Healing)
                    {
                        healWeight = Mathf.Lerp(healWeight, 1f, Time.deltaTime * 5f);
                    }
                    else
                    {
                        healWeight = Mathf.Lerp(healWeight, 0f, Time.deltaTime * 5f);
                    }

                    if (repairArmorPlayables.Repairing)
                    {
                        repairArmorWeight = Mathf.Lerp(repairArmorWeight, 1f, Time.deltaTime * 5f);
                    }
                    else
                    {
                        repairArmorWeight = Mathf.Lerp(repairArmorWeight, 0f, Time.deltaTime * 5f);
                    }

                    #region LOWER BODY MASK

                    if (!controller.IsCrouch && !controller.IsProne)
                    {
                        if (inventory.WeaponIndex == 1)
                        {
                            basicMovementWeight = Mathf.Lerp(basicMovementWeight, 1f, Time.deltaTime * 5f);
                            swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 0f, Time.deltaTime * 5f);
                            spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 0f, Time.deltaTime * 5f);
                            rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                            bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
                        }
                        else if (inventory.WeaponIndex == 2)
                        {
                            if (inventory.PrimaryWeapon.WeaponID == "001")
                            {
                                swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 1f, Time.deltaTime * 5f);
                                spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 0f, Time.deltaTime * 5f);
                            }
                            else if (inventory.PrimaryWeapon.WeaponID == "002")
                            {
                                spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 1f, Time.deltaTime * 5f);
                                swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 0f, Time.deltaTime * 5f);
                            }

                            basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                            rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                            bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
                        }
                        else if (inventory.WeaponIndex == 3)
                        {
                            if (inventory.SecondaryWeapon.WeaponID == "003")
                            {
                                rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 1f, Time.deltaTime * 5f);
                                bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
                            }
                            else if (inventory.SecondaryWeapon.WeaponID == "004")
                            {
                                bowBasicWeight = Mathf.Lerp(bowBasicWeight, 1f, Time.deltaTime * 5f);
                                rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                            }

                            basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                        }

                        crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                        proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                    }

                    #endregion

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

                        if (inventory.WeaponIndex == 2)
                        {
                            if (inventory.PrimaryWeapon.WeaponID == "001")
                            {
                                swordWeight = Mathf.Lerp(swordWeight, 1f, Time.deltaTime * 5f);
                                spearWeight = Mathf.Lerp(spearWeight, 0f, Time.deltaTime * 5f);
                            }
                            else if (inventory.PrimaryWeapon.WeaponID == "002")
                            {
                                spearWeight = Mathf.Lerp(spearWeight, 1f, Time.deltaTime * 5f);
                                swordWeight = Mathf.Lerp(swordWeight, 0f, Time.deltaTime * 5f);
                            }
                        }
                        else
                        {
                            swordWeight = Mathf.Lerp(swordWeight, 0f, Time.deltaTime * 5f);
                            spearWeight = Mathf.Lerp(spearWeight, 0f, Time.deltaTime * 5f);
                        }

                        if (inventory.WeaponIndex == 3)
                        {
                            if (inventory.SecondaryWeapon.WeaponID == "003")
                            {
                                rifleAttackWeight = Mathf.Lerp(rifleAttackWeight, 1f, Time.deltaTime * 5f);
                                bowAttackWeight = Mathf.Lerp(bowAttackWeight, 0f, Time.deltaTime * 5f);
                            }
                            else if (inventory.SecondaryWeapon.WeaponID == "004")
                            {
                                bowAttackWeight = Mathf.Lerp(bowAttackWeight, 1f, Time.deltaTime * 5f);
                                rifleAttackWeight = Mathf.Lerp(rifleAttackWeight, 0f, Time.deltaTime * 5f);
                            }
                        }
                        else
                        {
                            rifleAttackWeight = Mathf.Lerp(rifleAttackWeight, 0f, Time.deltaTime * 5f);
                            bowAttackWeight = Mathf.Lerp(bowAttackWeight, 0f, Time.deltaTime * 5f);
                        }

                        if (controller.IsCrouch)
                        {
                            crouchWeight = Mathf.Lerp(crouchWeight, 1f, Time.deltaTime * 5f);
                            proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                            basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                            swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 0f, Time.deltaTime * 5f);
                            spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 0f, Time.deltaTime * 5f);
                            rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                            bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
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
                        swordWeight = Mathf.Lerp(swordWeight, 0f, Time.deltaTime * 5f);
                        swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 0f, Time.deltaTime * 5f);
                        spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 0f, Time.deltaTime * 5f);
                        spearWeight = Mathf.Lerp(spearWeight, 0f, Time.deltaTime * 5f);
                        rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                        rifleAttackWeight = Mathf.Lerp(rifleAttackWeight, 0f, Time.deltaTime * 5f);
                        bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
                        bowAttackWeight = Mathf.Lerp(bowAttackWeight, 0f, Time.deltaTime * 5f);
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
                    swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 0f, Time.deltaTime * 5f);
                    bareHandWeight = Mathf.Lerp(bareHandWeight, 0f, Time.deltaTime * 5f);
                    crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                    proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                    swordWeight = Mathf.Lerp(swordWeight, 0f, Time.deltaTime * 5f);
                    spearWeight = Mathf.Lerp(spearWeight, 0f, Time.deltaTime * 5f);
                    spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 0f, Time.deltaTime * 5f);
                    healWeight = Mathf.Lerp(healWeight, 0f, Time.deltaTime * 5f);
                    repairArmorWeight = Mathf.Lerp(repairArmorWeight, 0f, Time.deltaTime * 5f);
                    rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                    rifleAttackWeight = Mathf.Lerp(rifleAttackWeight, 0f, Time.deltaTime * 5f);
                    bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
                    bowAttackWeight = Mathf.Lerp(bowAttackWeight, 0f, Time.deltaTime * 5f);
                }
            }
            else
            {
                basicMovementWeight = Mathf.Lerp(basicMovementWeight, 0f, Time.deltaTime * 5f);
                swordBasicMovementWeight = Mathf.Lerp(swordBasicMovementWeight, 0f, Time.deltaTime * 5f);
                spearBasicMovementWeight = Mathf.Lerp(spearBasicMovementWeight, 0f, Time.deltaTime * 5f);
                bareHandWeight = Mathf.Lerp(bareHandWeight, 0f, Time.deltaTime * 5f);
                crouchWeight = Mathf.Lerp(crouchWeight, 0f, Time.deltaTime * 5f);
                proneWeight = Mathf.Lerp(proneWeight, 0f, Time.deltaTime * 5f);
                jumpMovementWeight = Mathf.Lerp(jumpMovementWeight, 0f, Time.deltaTime * 5f);
                fallingWeight = Mathf.Lerp(fallingWeight, 0f, Time.deltaTime * 5f);
                swordWeight = Mathf.Lerp(swordWeight, 0f, Time.deltaTime * 5f);
                spearWeight = Mathf.Lerp(spearWeight, 0f, Time.deltaTime * 5f);
                healWeight = Mathf.Lerp(healWeight, 0f, Time.deltaTime * 5f);
                repairArmorWeight = Mathf.Lerp(repairArmorWeight, 0f, Time.deltaTime * 5f);
                deadWeight = Mathf.Lerp(deadWeight, 1f, Time.deltaTime * 5f);
                rifleBasicWeight = Mathf.Lerp(rifleBasicWeight, 0f, Time.deltaTime * 5f);
                rifleAttackWeight = Mathf.Lerp(rifleAttackWeight, 0f, Time.deltaTime * 5f);
                bowBasicWeight = Mathf.Lerp(bowBasicWeight, 0f, Time.deltaTime * 5f);
                bowAttackWeight = Mathf.Lerp(bowAttackWeight, 0f, Time.deltaTime * 5f);
            }


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
            mainPlayable.SetInputWeight(15, bowBasicWeight);
            mainPlayable.SetInputWeight(16, bowAttackWeight);
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
