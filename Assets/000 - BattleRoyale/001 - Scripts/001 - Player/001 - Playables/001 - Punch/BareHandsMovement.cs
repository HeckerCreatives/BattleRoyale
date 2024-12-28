using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BareHandsMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private AnimationClip idleClip;

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    //  ============================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 1);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Runner.IsForward)
        {
            foreach (var playables in clipPlayables)
            {
                // Only update authoritative clients
                if (HasInputAuthority || HasStateAuthority)
                {
                    playables.SetTime(mainCorePlayable.TickRateAnimation);
                }
            }
        }

        CheckAnimation();
    }

    private void CheckAnimation()
    {
        if (!playerController.IsAttacking)
            movementMixer.SetInputWeight(0, 1f);
        else
        {
            movementMixer.SetInputWeight(0, 0f);
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
