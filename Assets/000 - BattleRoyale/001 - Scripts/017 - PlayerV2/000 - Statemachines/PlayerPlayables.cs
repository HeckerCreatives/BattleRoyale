using Cinemachine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using NUnit.Framework;
using System;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using UnityEngine.Rendering.PostProcessing;
using static Fusion.Sockets.NetBitBuffer;
using static MainCorePlayable;

public class PlayerPlayables : NetworkBehaviour
{
    private const int RECONCILE_DELAY_TICKS = 3;

    //  =====================

    public PlayerStamina stamina;
    public PlayerInventoryV2 inventory;
    public PlayerOwnObjectEnabler ownObjectEnabler;
    public PlayerCameraRotation cameraRotation;
    public PlayerAim aimWeights;
    public MeleeSoundController fistSoundController;

    [Space]
    [SerializeField] private CinemachineVirtualCamera vCam;
    [SerializeField] private CinemachineBasicMultiChannelPerlin vcamShaker;

    [Space]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform playerObj;
    [SerializeField] private Transform bone;
    [SerializeField] private Transform target;
    [SerializeField] private float maxPitchUp = 25f;
    [SerializeField] private float maxPitchDown = 20f;
    [SerializeField] private bool enableRoll = false;
    [SerializeField] private float rollDeg = 0f;   // set this from your input / leaning logic
    [SerializeField] private float maxRoll = 12f;

    [Space]
    [SerializeField] private Transform bonePunchRot;
    [SerializeField] private Vector3 punchrotationfix;

    [Space]
    public PlayerHealthV2 healthV2;
    public PlayerUpperMovement upperBodyMovement;
    public PlayerBasicMovement lowerBodyMovement;
    public NetworkObject bullets;
    public NetworkObject arrows;
    public Transform muzzlePoint;

    [Space]
    [SerializeField] private AvatarMask upperBodyMask;
    [SerializeField] private AvatarMask lowerBodyMask;

    [Space]
    public float enterSpeed;
    public float exitSpeed;

    [Space]
    [SerializeField] private ParticleSystem[] swordSlashList;

    [Space]
    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private Transform groundDetector;
    [SerializeField] private LayerMask groundMask;

    [Space]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] grassClip;
    [SerializeField] private AudioClip[] dirtClip;
    [SerializeField] private AudioClip[] stoneClip;
    [SerializeField] private AudioClip[] woodClip;
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip rollClip;

    [Header("DEBUGGER")]
    [SerializeField] private int _lastProcessedTickUpper = -1;
    [SerializeField] private int _lastProcessedTickLower = -1;
    [SerializeField] private float[] textureValues;
    [SerializeField] Vector3 terrainPosition;
    [SerializeField] Vector3 mapPosition;
    [SerializeField] float xCoord;
    [SerializeField] float zCoord;
    [SerializeField] int posX;
    [SerializeField] int posZ;
    [SerializeField] AudioClip selectedClip;
    [SerializeField] AudioClip previousClip;
    [SerializeField] private int audioClipIndex;
    [SerializeField] private float _upperMismatchTime;
    [SerializeField] private float _lowerMismatchTime;
    [SerializeField] private int _upperMismatchStartTick = -1;
    [SerializeField] private int _lowerMismatchStartTick = -1;
    [SerializeField] private int _pendingUpperTick = -1;
    [SerializeField] private int _pendingLowerTick = -1;

    [field: Header("NETWORK DEBUGGER")]
    [Networked][field: SerializeField] public float TickRateAnimation { get; set; }
    [Networked][field: SerializeField] public int PlayableUpperBoddyAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableLowerBoddyAnimationIndex { get; set; }
    [Networked][field: SerializeField] public int PlayableUpperBodyAnimationTick { get; set; }
    [Networked][field: SerializeField] public int PlayableLowerBodyAnimationTick { get; set; }
    [Networked][field: SerializeField] public string PlayableState { get; set; }
    [Networked][field: SerializeField] public bool FinalAttack { get; set; }
    [Networked][field: SerializeField] public Ground CurrentGround { get; set; }

    //  =======================

    public PlayableGraph playableGraph;
    public UpperBodyChanger upperBodyChanger;
    public PlayablesChanger lowerBodyChanger;
    private UpperBodyAnimations _pendingUpperState;
    private AnimationPlayable _pendingLowerState;
    public AnimationLayerMixerPlayable finalMixer;
    public AnimationScriptPlayable lookAtPlayable;
    public AnimationScriptPlayable punchFixRotation;
    public LookAtJobBoneIK job { get; set; }
    LagCompensatedHit hit = new LagCompensatedHit();

    private ChangeDetector _changeDetector;

    private Vector3 rollAxisLocal;

    //  =======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);


        vcamShaker = vCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void OnEnable()
    {
        InitializePlayables();
    }

    private void OnDisable()
    {
        playableGraph.Destroy();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.V))
        {
            Debug.Log("DESTROY");
            playableGraph.Destroy();
        }
        else if (Input.GetKeyUp(KeyCode.B))
        {
            Debug.Log("RE INIT");
            InitializePlayables();
        }
    }

    public void PunchRotFix()
    {
        bonePunchRot.localRotation *= Quaternion.Euler(punchrotationfix);
    }

    public override void Render()
    {
        if (lowerBodyChanger.CurrentState == null || upperBodyChanger.CurrentState == null) return;
        if (HasStateAuthority) return;

        upperBodyChanger.CurrentState.NetworkLocalUpdate();

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayableUpperBoddyAnimationIndex):
                case nameof(PlayableUpperBodyAnimationTick):

                    if (PlayableUpperBodyAnimationTick != _lastProcessedTickUpper)
                    {
                        _pendingUpperTick = PlayableUpperBodyAnimationTick;
                        _pendingUpperState = upperBodyMovement.GetPlayableAnimation(PlayableUpperBoddyAnimationIndex);
                        _upperMismatchStartTick = Runner.Tick;
                    }

                    break;

                case nameof(PlayableLowerBoddyAnimationIndex):
                case nameof(PlayableLowerBodyAnimationTick):

                    if (PlayableLowerBodyAnimationTick != _lastProcessedTickLower)
                    {
                        _pendingLowerTick = PlayableLowerBodyAnimationTick;
                        _pendingLowerState = lowerBodyMovement.GetPlayableAnimation(PlayableLowerBoddyAnimationIndex);
                        _lowerMismatchStartTick = Runner.Tick;
                    }

                    break;
            }
        }

        ResolveUpperReplication();
        ResolveLowerReplication();
    }

    public override void FixedUpdateNetwork()
    {
        if (lowerBodyChanger.CurrentState == null || upperBodyChanger.CurrentState == null) return;

        upperBodyChanger.CurrentState.NetworkUpdate();
        lowerBodyChanger.CurrentState.NetworkUpdate();
    }

    private void ResolveUpperReplication()
    {
        if (_pendingUpperTick == -1)
            return;

        if (!HasInputAuthority)
        {
            upperBodyChanger.ChangeState(_pendingUpperState);
            _lastProcessedTickUpper = _pendingUpperTick;
            _pendingUpperTick = -1;
            return;
        }

        if (upperBodyChanger.CurrentState == _pendingUpperState)
        {
            _lastProcessedTickUpper = _pendingUpperTick;
            _pendingUpperTick = -1;
            _upperMismatchStartTick = -1;
            return;
        }

        if (Runner.Tick - _upperMismatchStartTick < RECONCILE_DELAY_TICKS)
            return;

        upperBodyChanger.ChangeState(_pendingUpperState);

        _lastProcessedTickUpper = _pendingUpperTick;
        _pendingUpperTick = -1;
        _upperMismatchStartTick = -1;
    }

    private void ResolveLowerReplication()
    {
        if (_pendingLowerTick == -1)
            return;

        if (!HasInputAuthority)
        {
            lowerBodyChanger.ChangeState(_pendingLowerState);
            _lastProcessedTickLower = _pendingLowerTick;
            _pendingLowerTick = -1;
            return;
        }

        if (lowerBodyChanger.CurrentState == _pendingLowerState)
        {
            _lastProcessedTickLower = _pendingLowerTick;
            _pendingLowerTick = -1;
            _lowerMismatchStartTick = -1;
            return;
        }

        if (Runner.Tick - _lowerMismatchStartTick < RECONCILE_DELAY_TICKS)
            return;

        lowerBodyChanger.ChangeState(_pendingLowerState);

        _lastProcessedTickLower = _pendingLowerTick;
        _pendingLowerTick = -1;
        _lowerMismatchStartTick = -1;
    }

    public void InitializePlayables()
    {
        // NOW create changers after mixers are valid
        lowerBodyChanger = new PlayablesChanger();
        upperBodyChanger = new UpperBodyChanger();

        playableGraph = PlayableGraph.Create("MyPlayableGraph");

        // Build your animation graph
        finalMixer = AnimationLayerMixerPlayable.Create(playableGraph, 2);

        // Initialize movement first so their mixers get created
        var lowerPlayable = lowerBodyMovement.Initialize();
        lowerBodyChanger.Initialize(lowerBodyMovement.IdlePlayable);
        var upperPlayable = upperBodyMovement.Initialize();
        upperBodyChanger.Initialize(upperBodyMovement.IdlePlayables);

        // Connect animation playables into the mixer
        playableGraph.Connect(lowerPlayable, 0, finalMixer, 0);
        finalMixer.SetInputWeight(0, 1f);

        playableGraph.Connect(upperPlayable, 0, finalMixer, 1);
        finalMixer.SetInputWeight(1, 1f);
        finalMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);


        Transform parentT = bone.parent;

        rollAxisLocal = PickLocalAxisClosestToWorldDir(bone, parentT.forward);

        job = new LookAtJobBoneIK
        {
            bone = playerAnimator.BindStreamTransform(bone),
            weight = 0f,
            pitchAxisLocal = Vector3.forward,
            pitchAxisSign = -1,
            pitchDeg = 0f
        };

        lookAtPlayable = AnimationScriptPlayable.Create(playableGraph, job);
        lookAtPlayable.SetInputCount(1);
        lookAtPlayable.ConnectInput(0, finalMixer, 0);
        lookAtPlayable.SetInputWeight(0, 1f);

        PunchBoneFixJob punchJob = new PunchBoneFixJob
        {
            bone = playerAnimator.BindStreamTransform(bone),
            weight = 0f,
            correctionEulerLocal = Vector3.zero
        };

        punchFixRotation = AnimationScriptPlayable.Create(playableGraph, punchJob);
        punchFixRotation.SetInputCount(1);

        // Chain it AFTER lookAtPlayable, not directly to finalMixer
        punchFixRotation.ConnectInput(0, lookAtPlayable, 0);
        punchFixRotation.SetInputWeight(0, 1f);

        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);

        // Output should use only the LAST playable in the chain
        playableOutput.SetSourcePlayable(punchFixRotation);

        playableGraph.Play();
    }

    public void SlashSwordParticles(int index)
    {
        swordSlashList[index].Play();
    }

    static Vector3 PickLocalAxisClosestToWorldDir(Transform bone, Vector3 desiredWorldDir)
    {
        Vector3[] axes =
        {
            Vector3.right, Vector3.up, Vector3.forward,
            -Vector3.right, -Vector3.up, -Vector3.forward
        };

        desiredWorldDir.Normalize();

        float best = -999f;
        Vector3 bestAxis = Vector3.forward;

        foreach (var a in axes)
        {
            Vector3 worldA = bone.TransformDirection(a).normalized;
            float d = Mathf.Abs(Vector3.Dot(worldA, desiredWorldDir));
            if (d > best)
            {
                best = d;
                bestAxis = a;
            }
        }
        return bestAxis;
    }

    public void SetLookAtWeight(float newWeight)
    {
        if (!lookAtPlayable.IsValid())
        {
            Debug.Log("look at playable not valid");
            return;
        }

        if (Runner == null) return;

        var currentJob = lookAtPlayable.GetJobData<LookAtJobBoneIK>();
        currentJob.weight = newWeight; // Smooth transition
        currentJob.pitchAxisLocal = Vector3.forward;
        currentJob.pitchAxisSign = -1;
        currentJob.pitchDeg = -cameraRotation._cinemachineTargetPitch;
        lookAtPlayable.SetJobData(currentJob);
    }

    public void SetPunchRotation(float newWeight)
    {
        if (!punchFixRotation.IsValid())
        {
            Debug.Log("punchFixRotation not valid");
            return;
        }

        if (Runner == null) return;

        var jobData = punchFixRotation.GetJobData<PunchBoneFixJob>();
        jobData.weight = newWeight;
        jobData.correctionEulerLocal = punchrotationfix;
        punchFixRotation.SetJobData(jobData);
    }

    public void SetAnimationUpperTick() => PlayableUpperBodyAnimationTick = Runner.Tick;

    public void SetAnimationLowerTick() => PlayableLowerBodyAnimationTick = Runner.Tick;

    public void SpawnBullets(Vector3 startPos, LagCompensatedHit hit, bool isRifle, float additionalTimer = 5f)
    {
        Runner.Spawn(bullets, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            obj.GetComponent<BulletController>().Fire((isRifle ? muzzlePoint.position : startPos), hit, additionalTimer);
        });
    }

    public void CameraShaker(float amplitude)
    {
        vcamShaker.m_AmplitudeGain = amplitude;
    }

    public void SpawnArrows() => Runner.Spawn(arrows);

    public void PlayJumpSoundEffect()
    {
        if (HasStateAuthority) return;

        footstepSource.PlayOneShot(jumpClip);
    }

    public void PlayRollSoundEffect()
    {
        if (HasStateAuthority) return;

        footstepSource.PlayOneShot(rollClip);
    }

    public void CheckGround()
    {
        if (Runner.LagCompensation.Raycast(groundDetector.position, Vector3.down, 10f, Object.InputAuthority, out hit, groundMask, HitOptions.IncludePhysX))
        {
            if (hit.GameObject == null) return;

            if (hit.GameObject.tag == "BattleAreaStage" || hit.GameObject.tag == "WaitingAreaStage") CurrentGround = Ground.TERRAIN;
            else if (hit.GameObject.tag == "Stone") CurrentGround = Ground.STONE;
            else if (hit.GameObject.tag == "Dirt") CurrentGround = Ground.DIRT;
            else if (hit.GameObject.tag == "Wood") CurrentGround = Ground.WOOD;
            else if (hit.GameObject.tag == "Grass") CurrentGround = Ground.GRASS;
        }
    }

    public void PlayFootstepSound()
    {
        if (HasStateAuthority) return;

        if (CurrentGround == MainCorePlayable.Ground.DIRT)
            footstepSource.PlayOneShot(GetClip(dirtClip));
        else if (CurrentGround == MainCorePlayable.Ground.STONE)
            footstepSource.PlayOneShot(GetClip(stoneClip));
        else if (CurrentGround == MainCorePlayable.Ground.WOOD)
            footstepSource.PlayOneShot(GetClip(woodClip));
        else if (CurrentGround == MainCorePlayable.Ground.GRASS)
            footstepSource.PlayOneShot(GetClip(grassClip));
    }

    AudioClip GetClip(AudioClip[] clipArray)
    {
        if (audioClipIndex > clipArray.Length)
            audioClipIndex = 0;

        selectedClip = clipArray[audioClipIndex];

        previousClip = selectedClip;
        return selectedClip;
    }
}

public struct LookAtJobBoneIK : IAnimationJob
{
    public TransformStreamHandle bone;
    public float weight;

    public float pitchDeg;         // feed from _cinemachineTargetPitch
    public Vector3 pitchAxisLocal; // Vector3.forward or Vector3.right
    public float pitchAxisSign;    // +1 or -1

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (weight <= 0f) return;
        if (!bone.IsValid(stream)) return;

        Quaternion baseLocal = bone.GetRotation(stream);

        Quaternion pitchDelta = Quaternion.AngleAxis(pitchDeg * pitchAxisSign, pitchAxisLocal);
        bone.SetRotation(stream, baseLocal * Quaternion.Slerp(Quaternion.identity, pitchDelta, weight));
    }
}

public struct PunchBoneFixJob : IAnimationJob
{
    public TransformStreamHandle bone;
    public float weight;

    public Vector3 correctionEulerLocal;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (!bone.IsValid(stream)) return;

        Quaternion animatedLocal = bone.GetLocalRotation(stream);

        Quaternion correction = Quaternion.Slerp(
            Quaternion.identity,
            Quaternion.Euler(correctionEulerLocal),
            weight
        );

        bone.SetLocalRotation(stream, animatedLocal * correction);
    }
}