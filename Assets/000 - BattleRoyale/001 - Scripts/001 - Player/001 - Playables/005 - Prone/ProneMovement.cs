using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class ProneMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip moveClip;

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    //  ============================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 2);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var movePlayable = AnimationClipPlayable.Create(graph, moveClip);
        clipPlayables.Add(movePlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(movePlayable, 0, movementMixer, 1);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }
    }

    public override void Render()
    {
        if (!HasInputAuthority && !HasStateAuthority && playerController.IsProne)
        {
            foreach (var playables in clipPlayables)
            {
                double currentPlayableTime = playables.GetTime();
                if (Mathf.Abs((float)(currentPlayableTime - mainCorePlayable.TickRateAnimation)) > 0.01f) // Adjust threshold as needed
                    playables.SetTime(mainCorePlayable.TickRateAnimation);
            }

            UpdateBlendTreeWeights();
        }
    }

    public override void FixedUpdateNetwork()
    {
        TickRate();
        UpdateBlendTreeWeights();
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

    private void UpdateBlendTreeWeights()
    {
        if (!movementMixer.IsValid()) return;

        if (!playerController.IsProne) return;

        float xMovement = playerController.XMovement;
        float yMovement = playerController.YMovement;

        // Calculate weights for each movement
        float idleWeight = (xMovement == 0 && yMovement == 0) ? 1f : 0f;
        float moveWeight = (xMovement != 0 || yMovement != 0) ? 1f : 0f; // Left strafe

        // Apply weights to mixer inputs
        movementMixer.SetInputWeight(0, idleWeight);
        movementMixer.SetInputWeight(1, moveWeight);
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
