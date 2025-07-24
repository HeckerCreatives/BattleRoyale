using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BotPlayables : NetworkBehaviour
{
    [SerializeField] private Animator botAnimator;
    [SerializeField] private BotBasicMovement basicMovement;


    [Header("DEBUGGER")]
    [SerializeField] private int _lastProcessedTick = -1;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][field: SerializeField] public float TickRateAnimation { get; set; }
    [Networked][field: SerializeField] public int PlayableAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableAnimationTick { get; set; }
    [Networked][field: SerializeField] public string PlayableState { get; set; }


    //  =======================

    public PlayableGraph playableGraph;
    public BotPlayableChanger changer;
    public AnimationMixerPlayable finalMixer;

    private ChangeDetector _changeDetector;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    private void OnDisable()
    {
        playableGraph.Destroy();
    }

    public override void Render()
    {
        if (HasStateAuthority) return;

        if (changer.CurrentState == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayableAnimationIndex):
                case nameof(PlayableAnimationTick):

                    if (PlayableState == "basic" && PlayableAnimationTick != _lastProcessedTick)
                    {
                        changer.ChangeState(basicMovement.GetPlayableAnimation(PlayableAnimationIndex));
                        _lastProcessedTick = PlayableAnimationTick;
                    }

                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        TickRateAnimation = Runner.Tick * Runner.DeltaTime;

        if (changer.CurrentState == null) return;

        changer.CurrentState.NetworkUpdate();
    }


    public void InitializePlayables()
    {
        changer = new BotPlayableChanger();

        playableGraph = PlayableGraph.Create();

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "BotAnimation", botAnimator);

        var splitter = AnimationMixerPlayable.Create(playableGraph, 1);

        playableGraph.Connect(splitter, 0, basicMovement.Initialize(), 0);

        finalMixer = AnimationMixerPlayable.Create(playableGraph, 1);

        playableGraph.Connect(basicMovement.mixerPlayable, 0, finalMixer, 0);   //  BASIC MOVEMENT
        //playableGraph.Connect(mixerPlayable2, 0, splitter2, 1);

        playableOutput.SetSourcePlayable(finalMixer);

        //changer.Initialize(basicMovement.IdlePlayable);

        finalMixer.SetInputWeight(0, 1f);

        playableGraph.Play();

        GraphVisualizerClient.Show(playableGraph);
    }

    public void SetAnimationTick() => PlayableAnimationTick = Runner.Tick;
}
