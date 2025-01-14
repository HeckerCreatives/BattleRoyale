using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class CrouchMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip sideStrafeLeftClip;
    [SerializeField] private AnimationClip sideStrafeRightClip;
    [SerializeField] private AnimationClip backwardClip;
    [SerializeField] private AnimationClip moveClip;

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    //  ============================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 5);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var leftPlayable = AnimationClipPlayable.Create(graph, sideStrafeLeftClip);
        clipPlayables.Add(leftPlayable);

        var rightPlayable = AnimationClipPlayable.Create(graph, sideStrafeRightClip);
        clipPlayables.Add(rightPlayable);

        var backwardPlayable = AnimationClipPlayable.Create(graph, backwardClip);
        clipPlayables.Add(backwardPlayable);

        var movePlayable = AnimationClipPlayable.Create(graph, moveClip);
        clipPlayables.Add(movePlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(leftPlayable, 0, movementMixer, 1);
        graph.Connect(rightPlayable, 0, movementMixer, 2);
        graph.Connect(backwardPlayable, 0, movementMixer, 3);
        graph.Connect(movePlayable, 0, movementMixer, 4);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }
    }

    public override void Render()
    {
        if (!HasInputAuthority && !HasStateAuthority && playerController.IsCrouch)
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

        if (!playerController.IsCrouch) return;

        float xMovement = playerController.XMovement;
        float yMovement = playerController.YMovement;

        // Calculate weights for each movement
        float idleWeight = (xMovement == 0 && yMovement == 0) ? 1f : 0f;
        float leftWeight = Mathf.Clamp01(-xMovement); // Left strafe
        float rightWeight = Mathf.Clamp01(xMovement); // Right strafe
        float backwardWeight = Mathf.Clamp01(-yMovement); // Backward
        float sprintWeight = Mathf.Clamp01(yMovement > 0 ? yMovement : 0); // Forward (Sprint)

        // Normalize weights for side and forward/backward movement
        float totalWeight = leftWeight + rightWeight + backwardWeight + sprintWeight;
        if (totalWeight > 1f)
        {
            leftWeight /= totalWeight;
            rightWeight /= totalWeight;
            backwardWeight /= totalWeight;
            sprintWeight /= totalWeight;
        }

        // Apply weights to mixer inputs
        movementMixer.SetInputWeight(0, idleWeight);
        movementMixer.SetInputWeight(1, leftWeight);
        movementMixer.SetInputWeight(2, rightWeight);
        movementMixer.SetInputWeight(3, backwardWeight);
        movementMixer.SetInputWeight(4, sprintWeight);
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
