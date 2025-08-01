using Fusion;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class PlayerPlayables : NetworkBehaviour
{
    public PlayerStamina stamina;
    public PlayerInventoryV2 inventory;
    public PlayerOwnObjectEnabler ownObjectEnabler;

    [Space]
    [SerializeField] private Animator playerAnimator;

    [Space]
    public PlayerHealthV2 healthV2;
    public PlayerBasicMovement upperBodyMovement;
    public PlayerBasicMovement lowerBodyMovement;
    public HealMovement healMovements;

    [Space]
    [SerializeField] private AvatarMask upperBodyMask;
    [SerializeField] private AvatarMask lowerBodyMask;

    [Space]
    public float enterSpeed;
    public float exitSpeed;

    [Header("DEBUGGER")]
    [SerializeField] private int _lastProcessedTickUpper = -1;
    [SerializeField] private int _lastProcessedTickLower = -1;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][field: SerializeField] public float TickRateAnimation { get; set; }
    [Networked][field: SerializeField] public int PlayableUpperBoddyAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableLowerBoddyAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableUpperBodyAnimationTick { get; set; }
    [Networked][field: SerializeField] public int PlayableLowerBodyAnimationTick { get; set; }
    [Networked][field: SerializeField] public string PlayableState { get; set; }

    //  =======================

    public PlayableGraph playableGraph;
    public PlayablesChanger lowerBodyChanger;
    public PlayablesChanger upperBodyChanger;
    public AnimationLayerMixerPlayable finalMixer;

    private ChangeDetector _changeDetector;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    public void OnEnable()
    {
        InitializePlayables();
    }

    private void OnDisable()
    {
        playableGraph.Destroy();
    }

    public override void Render()
    {
        if (HasStateAuthority || HasInputAuthority) return;

        if (lowerBodyChanger.CurrentState == null) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayableUpperBoddyAnimationIndex):
                case nameof(PlayableUpperBodyAnimationTick):

                    if (PlayableState == "basic" && PlayableUpperBodyAnimationTick != _lastProcessedTickUpper)
                    {
                        upperBodyChanger.ChangeState(upperBodyMovement.GetPlayableAnimation(PlayableUpperBoddyAnimationIndex));
                        _lastProcessedTickUpper = PlayableUpperBodyAnimationTick;
                    }

                    break;
                case nameof(PlayableLowerBoddyAnimationIndex):
                case nameof(PlayableLowerBodyAnimationTick):

                    if (PlayableState == "basic" && PlayableLowerBodyAnimationTick != _lastProcessedTickUpper)
                    {
                        lowerBodyChanger.ChangeState(lowerBodyMovement.GetPlayableAnimation(PlayableUpperBoddyAnimationIndex));
                        _lastProcessedTickLower = PlayableLowerBodyAnimationTick;
                    }

                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        TickRateAnimation = Runner.Tick * Runner.DeltaTime;

        if (lowerBodyChanger.CurrentState == null) return;

        lowerBodyChanger.CurrentState.NetworkUpdate();
    }

    public void InitializePlayables()
    {
        lowerBodyChanger = new PlayablesChanger(this);
        upperBodyChanger = new PlayablesChanger(this);

        playableGraph = PlayableGraph.Create("Player Animation");

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation Output", playerAnimator);

        finalMixer = AnimationLayerMixerPlayable.Create(playableGraph, 2);

        //  INITIALIZE ANIMATIONS
        playableGraph.Connect(lowerBodyMovement.Initialize(), 0, finalMixer, 0);   //  BASIC MOVEMENT
        lowerBodyChanger.Initialize(lowerBodyMovement.IdlePlayable);
        playableGraph.Connect(upperBodyMovement.Initialize(), 0, finalMixer, 1);
        upperBodyChanger.Initialize(upperBodyMovement.IdlePlayable);

        finalMixer.SetInputWeight(0, 1f);
        finalMixer.SetLayerMaskFromAvatarMask(0, lowerBodyMask);
        finalMixer.SetInputWeight(1, 1f);
        finalMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);

        playableOutput.SetSourcePlayable(finalMixer);

        playableGraph.Play();

        GraphVisualizerClient.Show(playableGraph);
    }

    public void SetAnimationTick() => PlayableUpperBodyAnimationTick = Runner.Tick;

    public void SetAnimationLowerTick() => PlayableLowerBodyAnimationTick = Runner.Tick;
}
