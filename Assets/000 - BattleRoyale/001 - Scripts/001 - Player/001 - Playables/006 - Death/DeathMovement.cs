using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class DeathMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;

    [Space]
    [SerializeField] private AnimationClip deathClip;

    [field: SerializeField] [Networked] public bool IsDead { get; set; }

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    //  ============================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 2);

        var deathPlayable = AnimationClipPlayable.Create(graph, deathClip);
        clipPlayables.Add(deathPlayable);

        graph.Connect(deathPlayable, 0, movementMixer, 0);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }
    }

    public override void Render()
    {
        if (!HasStateAuthority && movementMixer.IsValid())
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(IsDead):
                        if (IsDead)
                        {
                            var punchOnePlayable = clipPlayables[0];
                            punchOnePlayable.SetTime(0);
                            punchOnePlayable.Play();
                            movementMixer.SetInputWeight(0, 1f);
                        }
                        break;
                }
            }
        }
    }

    public void MakePlayerDead()
    {
        if (!movementMixer.IsValid()) return;

        if (IsDead) return;
        IsDead = true;
        var punchOnePlayable = clipPlayables[0];
        punchOnePlayable.SetTime(0);
        punchOnePlayable.Play();
        movementMixer.SetInputWeight(0, 1f);
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
