using Fusion.Addons.SimpleKCC;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fusion.NetworkBehaviour;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class FallingAnimation : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController controller;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private AnimationClip fallingClip;

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    //  ============================

    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  ============================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 1);

        var fallingPlayable = AnimationClipPlayable.Create(graph, fallingClip);
        clipPlayables.Add(fallingPlayable);

        graph.Connect(fallingPlayable, 0, movementMixer, 0);

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
            movementMixer.SetInputWeight(0, 1f); // Idle animation active
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority && movementMixer.IsValid() && !characterController.IsGrounded)
        {
            movementMixer.SetInputWeight(0, 1f); // Idle animation active
        }
    }

    private void TickRate()
    {
        if (Runner.IsForward)
        {
            if (clipPlayables == null) return;

            foreach (var playables in clipPlayables)
            {
                playables.SetTime(mainCorePlayable.TickRateAnimation);
            }
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
