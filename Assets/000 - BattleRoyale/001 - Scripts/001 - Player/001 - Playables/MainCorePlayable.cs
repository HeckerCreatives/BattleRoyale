using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class MainCorePlayable : NetworkBehaviour
{
    [SerializeField] private PlayerInventory inventory;

    [Space]
    [SerializeField] private Animator animator;
    [SerializeField] private AvatarMask upperBodyMask;

    [Space]
    [SerializeField] private BasicMovement basicMovement;
    [SerializeField] private BareHandsMovement bareHandsMovement;

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
        mainPlayable = AnimationLayerMixerPlayable.Create(playableGraph, 2);
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

        playableGraph.Play();
    }

    public override void Render()
    {
        if (playableGraph.IsValid())
        {
            //  Bare Hands
            if (inventory.WeaponIndex == 1)
            {
                mainPlayable.SetInputWeight(1, 1f);
            }
            else
            {
                mainPlayable.SetInputWeight(1, 0f);
            }
        }
    }

    private void OnDisable()
    {
        if (playableGraph.IsValid())
            playableGraph.Destroy();
    }

    public override void FixedUpdateNetwork()
    {
        TickRateAnimation = Runner.Tick * Runner.DeltaTime;
    }
}
