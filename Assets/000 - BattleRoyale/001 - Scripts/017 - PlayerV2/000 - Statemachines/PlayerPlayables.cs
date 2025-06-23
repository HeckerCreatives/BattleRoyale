using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerPlayables : NetworkBehaviour
{
    public PlayerStamina stamina;

    [Space]
    [SerializeField] private Animator playerAnimator;

    [Space]
    public PlayerBasicMovement basicMovement;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][field: SerializeField] public float TickRateAnimation { get; set; }

    //  =======================

    public PlayableGraph playableGraph;
    public PlayablesChanger changer;
    public AnimationMixerPlayable finalMixer;

    //  =======================

    private void OnEnable()
    {
        changer = new PlayablesChanger();

        playableGraph = PlayableGraph.Create();

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "PlayerAnimation", playerAnimator);

        var splitter = AnimationMixerPlayable.Create(playableGraph, 1);

        playableGraph.Connect(splitter, 0, basicMovement.Initialize(), 0);

        finalMixer = AnimationMixerPlayable.Create(playableGraph, 1);

        playableGraph.Connect(basicMovement.mixerPlayable, 0, finalMixer, 0);   //  BASIC MOVEMENT
        //playableGraph.Connect(mixerPlayable2, 0, splitter2, 1);

        playableOutput.SetSourcePlayable(finalMixer);

        changer.Initialize(basicMovement.IdlePlayable);

        finalMixer.SetInputWeight(0, 1f);

        playableGraph.Play();

        GraphVisualizerClient.Show(playableGraph);
    }

    private void OnDisable()
    {
        playableGraph.Destroy();
    }

    public override void Render()
    {
        if (changer.CurrentState == null) return;

        changer.CurrentState.LogicUpdate();
    }

    public override void FixedUpdateNetwork()
    {
        TickRateAnimation = Runner.Tick * Runner.DeltaTime;

        if (changer.CurrentState == null) return;

        changer.CurrentState.NetworkUpdate();
    }
}
