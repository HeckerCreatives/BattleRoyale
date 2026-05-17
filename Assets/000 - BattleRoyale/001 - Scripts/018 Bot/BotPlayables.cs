using Fusion;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BotPlayables : NetworkBehaviour
{
    public Botdata GetBotData
    {
        get => botData;
    }

    public BotBasicMovement BasicMovement
    {
        get => basicMovement;
    }

    public BotInventory Inventroy
    {
        get => inventory;
    }

    //  ==================

    [SerializeField] private BotBasicMovement basicMovement;
    [SerializeField] private Botdata botData;
    [SerializeField] private BotInventory inventory;

    [Space]
    public float enterSpeed;
    public float exitSpeed;

    [Space]
    [SerializeField] private Animator botAnimator;

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

    [Header("Animation Logging")]
    [Tooltip("When enabled, logs every animation state change with the currently equipped weapon.")]
    [SerializeField] private bool logAnimationChanges = true;
    private string _lastLoggedStateName;

    [field: Header("Particles")]
    [field: SerializeField] public ParticleSystem[] PunchSlashes { get; private set; }
    [field: SerializeField] public ParticleSystem PunchImpact { get; private set; }
    [field: SerializeField] public ParticleSystem[] SwordSlashes { get; private set; }
    [field: SerializeField] public ParticleSystem SwordImpact { get; private set; }
    [field: SerializeField] public ParticleSystem[] SpearSlashes { get; private set; }
    [field: SerializeField] public HitIndicatorController[] HitIndicatorPool { get; private set; }

    private int _comboCount;
    private int _hitIndicatorIndex;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        InitializePlayables();
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
                        LogAnimationStateIfChanged();
                        _lastProcessedTick = PlayableAnimationTick;
                    }

                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        TickRateAnimation = Runner.Tick * Runner.DeltaTime;

        if (HasStateAuthority && inventory != null)
            inventory.SyncEquipFlagsToWeaponIndex();

        if (changer.CurrentState == null) return;

        var nextState = changer.CurrentState.NetworkUpdate();
        if (nextState != null)
            changer.ChangeState(nextState);

        LogAnimationStateIfChanged();
    }

    private void LogAnimationStateIfChanged()
    {
        if (!logAnimationChanges || changer.CurrentState == null) return;

        string stateName = changer.CurrentState.GetType().Name;
        if (stateName == _lastLoggedStateName) return;
        _lastLoggedStateName = stateName;

        string weaponLabel = DescribeEquippedWeapon();
        string targetLabel = DescribeTarget();
        string botLabel = botData != null ? $"Bot#{botData.BotIndex}" : name;
        Debug.Log($"[BotAnim] {botLabel} → {stateName}  | weapon: {weaponLabel}  | target: {targetLabel}");
    }

    private string DescribeTarget()
    {
        var mc = basicMovement != null ? basicMovement.MovementController : null;
        if (mc == null) return "no-controller";
        if (mc.detectedTarget == null) return "none";

        float dist = Vector3.Distance(transform.position, mc.detectedTarget.transform.position);
        bool los = mc.HasLineOfSightToTarget();
        bool canRifle = mc.CanRifleShoot();
        bool canBow = mc.CanBowShoot();
        return $"{mc.detectedTarget.name} dist={dist:F1}m LOS={los} canRifle={canRifle} canBow={canBow}";
    }

    private string DescribeEquippedWeapon()
    {
        if (inventory == null) return "no-inventory";

        switch (inventory.WeaponIndex)
        {
            case 1:
                return "Hands(1)";
            case 2:
            {
                string id = inventory.GetPrimaryWeaponID();
                string name = id == "001" ? "Sword" : id == "002" ? "Spear" : "Primary?";
                return $"{name}({id}) idx=2";
            }
            case 3:
            {
                string id = inventory.GetSecondaryWeaponID();
                string name = id == "003" ? "Rifle" : id == "004" ? "Bow" : "Secondary?";
                int clip = inventory.SecondaryWeapon != null ? inventory.SecondaryWeapon.Supplies : 0;
                int reserve = id == "003" ? inventory.RifleMagazine : id == "004" ? inventory.BowMagazine : 0;
                return $"{name}({id}) idx=3 clip={clip} reserve={reserve}";
            }
            default:
                return $"Unknown(idx={inventory.WeaponIndex})";
        }
    }


    public void InitializePlayables()
    {
        changer = new BotPlayableChanger();

        playableGraph = PlayableGraph.Create();

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "BotAnimation", botAnimator);

        basicMovement.Initialize();

        finalMixer = AnimationMixerPlayable.Create(playableGraph, 1);
        playableGraph.Connect(basicMovement.mixerPlayable, 0, finalMixer, 0);
        playableOutput.SetSourcePlayable(finalMixer);

        changer.Initialize(basicMovement.IdlePlayable);

        finalMixer.SetInputWeight(0, 1f);

        playableGraph.Play();

        GraphVisualizerClient.Show(playableGraph);
    }

    public void SetAnimationTick() => PlayableAnimationTick = Runner.Tick;

    public void SlashPunchParticles(int index)
    {
        if (HasStateAuthority) return;
        PunchSlashes[index].Play();
    }

    public void SlashPunchParticlesStop(int index)
    {
        if (HasStateAuthority) return;
        if (PunchSlashes[index].isPlaying) PunchSlashes[index].Stop();
    }

    public void SlashSwordParticles(int index)
    {
        if (HasStateAuthority) return;
        SwordSlashes[index].Play();
    }

    public void SlashSwordParticlesStop(int index)
    {
        if (HasStateAuthority) return;
        if (SwordSlashes[index].isPlaying) SwordSlashes[index].Stop();
    }

    public void SlashSpearParticles(int index)
    {
        if (HasStateAuthority) return;
        SpearSlashes[index].Play();
    }

    public void SlashSpearParticlesStop(int index)
    {
        if (HasStateAuthority) return;
        if (SpearSlashes[index].isPlaying) SpearSlashes[index].Stop();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlayPunchHit()
    {
        if (HasStateAuthority) return;
        PunchImpact.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySwordHit()
    {
        if (HasStateAuthority) return;
        SwordImpact.Play();
    }

    public void RegisterComboHit(Vector3 enemyPosition)
    {
        if (HasStateAuthority) return;
        if (HitIndicatorPool == null || HitIndicatorPool.Length == 0) return;

        _comboCount++;
        string label = $"COMBO x {_comboCount}";

        var indicator = HitIndicatorPool[_hitIndicatorIndex];
        _hitIndicatorIndex = (_hitIndicatorIndex + 1) % HitIndicatorPool.Length;
        indicator.Show(enemyPosition, label);

        CancelInvoke(nameof(ResetCombo));
        Invoke(nameof(ResetCombo), 2f);
    }

    private void ResetCombo() => _comboCount = 0;
}
