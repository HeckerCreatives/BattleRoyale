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
    [SerializeField] private PlayerInventory playerInventory;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip moveClip;

    [Space]
    [SerializeField] private AnimationClip swordIdleClip;
    [SerializeField] private AnimationClip swordMoveClip;

    [Space]
    [SerializeField] private AnimationClip spearIdleClip;
    [SerializeField] private AnimationClip spearMoveClip;

    [Space]
    [SerializeField] private AnimationClip rifleIdleClip;
    [SerializeField] private AnimationClip rifleMoveClip;

    [Space]
    [SerializeField] private AnimationClip bowIdleClip;
    [SerializeField] private AnimationClip bowMoveClip;

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    //  ============================

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 10);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var movePlayable = AnimationClipPlayable.Create(graph, moveClip);
        clipPlayables.Add(movePlayable);

        var swordidlePlayable = AnimationClipPlayable.Create(graph, swordIdleClip);
        clipPlayables.Add(swordidlePlayable);

        var swordmovePlayable = AnimationClipPlayable.Create(graph, swordMoveClip);
        clipPlayables.Add(swordmovePlayable);

        var spearidlePlayable = AnimationClipPlayable.Create(graph, spearIdleClip);
        clipPlayables.Add(spearidlePlayable);

        var spearmovePlayable = AnimationClipPlayable.Create(graph, spearMoveClip);
        clipPlayables.Add(spearmovePlayable);

        var rifleidlePlayable = AnimationClipPlayable.Create(graph, rifleIdleClip);
        clipPlayables.Add(rifleidlePlayable);

        var riflemovePlayable = AnimationClipPlayable.Create(graph, rifleMoveClip);
        clipPlayables.Add(riflemovePlayable);

        var bowidlePlayable = AnimationClipPlayable.Create(graph, bowIdleClip);
        clipPlayables.Add(bowidlePlayable);

        var bowmovePlayable = AnimationClipPlayable.Create(graph, bowMoveClip);
        clipPlayables.Add(bowmovePlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(movePlayable, 0, movementMixer, 1);
        graph.Connect(swordidlePlayable, 0, movementMixer, 2);
        graph.Connect(swordmovePlayable, 0, movementMixer, 3);
        graph.Connect(spearidlePlayable, 0, movementMixer, 4);
        graph.Connect(spearmovePlayable, 0, movementMixer, 5);
        graph.Connect(rifleidlePlayable, 0, movementMixer, 6);
        graph.Connect(riflemovePlayable, 0, movementMixer, 7);
        graph.Connect(bowidlePlayable, 0, movementMixer, 8);
        graph.Connect(bowmovePlayable, 0, movementMixer, 9);
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

        if (playerInventory.WeaponIndex == 1)
        {
            movementMixer.SetInputWeight(0, idleWeight);
            movementMixer.SetInputWeight(1, moveWeight);
            movementMixer.SetInputWeight(2, 0);
            movementMixer.SetInputWeight(3, 0);
            movementMixer.SetInputWeight(4, 0);
            movementMixer.SetInputWeight(5, 0);
            movementMixer.SetInputWeight(6, 0);
            movementMixer.SetInputWeight(7, 0);
        }
        else if (playerInventory.WeaponIndex == 2)
        {
            if (playerInventory.PrimaryWeapon.WeaponID == "001")
            {
                movementMixer.SetInputWeight(2, idleWeight);
                movementMixer.SetInputWeight(3, moveWeight);
                movementMixer.SetInputWeight(4, 0);
                movementMixer.SetInputWeight(5, 0);
            }
            else if (playerInventory.PrimaryWeapon.WeaponID == "002")
            {
                movementMixer.SetInputWeight(4, idleWeight);
                movementMixer.SetInputWeight(5, moveWeight);
                movementMixer.SetInputWeight(2, 0);
                movementMixer.SetInputWeight(3, 0);
            }

            movementMixer.SetInputWeight(0, 0);
            movementMixer.SetInputWeight(1, 0);
            movementMixer.SetInputWeight(6, 0);
            movementMixer.SetInputWeight(7, 0);
        }
        else if (playerInventory.WeaponIndex == 3)
        {
            if (playerInventory.SecondaryWeapon.WeaponID == "003")
            {
                movementMixer.SetInputWeight(6, idleWeight);
                movementMixer.SetInputWeight(7, moveWeight);
                movementMixer.SetInputWeight(8, 0);
                movementMixer.SetInputWeight(9, 0);
            }
            else if (playerInventory.SecondaryWeapon.WeaponID == "004")
            {
                movementMixer.SetInputWeight(8, idleWeight);
                movementMixer.SetInputWeight(9, moveWeight);
                movementMixer.SetInputWeight(6, 0);
                movementMixer.SetInputWeight(7, 0);
            }

            movementMixer.SetInputWeight(0, 0);
            movementMixer.SetInputWeight(1, 0);
            movementMixer.SetInputWeight(2, 0);
            movementMixer.SetInputWeight(3, 0);
            movementMixer.SetInputWeight(4, 0);
            movementMixer.SetInputWeight(5, 0);
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
