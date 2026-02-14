using Fusion;
using Fusion.Addons.SimpleKCC;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Playables;
using static MainCorePlayable;

public class PlayerPlayables : NetworkBehaviour
{
    public PlayerStamina stamina;
    public PlayerInventoryV2 inventory;
    public PlayerOwnObjectEnabler ownObjectEnabler;
    public PlayerCameraRotation cameraRotation;
    public PlayerAim aimWeights;
    public MeleeSoundController fistSoundController;

    [Space]
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform bone;
    [SerializeField] private Transform target;

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
    public AnimationLayerMixerPlayable finalMixer;
    public AnimationScriptPlayable lookAtPlayable;
    public LookAtJobBoneIK job { get; set; }
    LagCompensatedHit hit = new LagCompensatedHit();

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

    public override void Render()
    {
        if (HasStateAuthority || HasInputAuthority) return;

        if (lowerBodyChanger.CurrentState == null || upperBodyChanger.CurrentState == null) return;

        upperBodyChanger.CurrentState.NetworkLocalUpdate();

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(PlayableUpperBoddyAnimationIndex):
                case nameof(PlayableUpperBodyAnimationTick):

                    if (PlayableUpperBodyAnimationTick != _lastProcessedTickUpper)
                    {
                        upperBodyChanger.ChangeState(upperBodyMovement.GetPlayableAnimation(PlayableUpperBoddyAnimationIndex));
                        _lastProcessedTickUpper = PlayableUpperBodyAnimationTick;
                    }

                    break;
                case nameof(PlayableLowerBoddyAnimationIndex):
                case nameof(PlayableLowerBodyAnimationTick):

                    if (PlayableLowerBodyAnimationTick != _lastProcessedTickLower)
                    {
                        lowerBodyChanger.ChangeState(lowerBodyMovement.GetPlayableAnimation(PlayableLowerBoddyAnimationIndex));
                        _lastProcessedTickLower = PlayableLowerBodyAnimationTick;
                    }

                    break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority && !HasInputAuthority) return;

        TickRateAnimation = Runner.Tick * Runner.DeltaTime;

        if (lowerBodyChanger.CurrentState == null || upperBodyChanger.CurrentState == null) return;

        upperBodyChanger.CurrentState.NetworkUpdate();
        lowerBodyChanger.CurrentState.NetworkUpdate();
    }

    public void InitializePlayables()
    {
        lowerBodyChanger = new PlayablesChanger();
        upperBodyChanger = new UpperBodyChanger();

        playableGraph = PlayableGraph.Create("MyPlayableGraph");

        // Build your animation graph
        finalMixer = AnimationLayerMixerPlayable.Create(playableGraph, 2);

        // Connect animation playables into the mixer
        playableGraph.Connect(lowerBodyMovement.Initialize(), 0, finalMixer, 0);
        finalMixer.SetInputWeight(0, 1f);
        //finalMixer.SetLayerMaskFromAvatarMask(0, lowerBodyMask);
        lowerBodyChanger.Initialize(lowerBodyMovement.IdlePlayable);

        playableGraph.Connect(upperBodyMovement.Initialize(), 0, finalMixer, 1);
        finalMixer.SetInputWeight(1, 1f);
        finalMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);
        upperBodyChanger.Initialize(upperBodyMovement.IdlePlayables);

        // ---------- LOOK AT JOB SETUP ----------
        Transform boneT = bone;
        Transform parentT = boneT != null ? boneT.parent : null;

        if (boneT == null || parentT == null)
        {
            Debug.LogError("Bone is not assigned or has no parent. Assign Chest/UpperChest from the character rig.");
            return;
        }

        if (target == null)
        {
            Debug.LogError("Target is not assigned.");
            return;
        }

        // STEP 2: Compute axis correction ONCE (local-space)
        // Align the bone's current world forward to the character's forward
        Vector3 boneForwardWorld = boneT.rotation * Vector3.forward;
        Vector3 desiredForwardWorld = playerAnimator.transform.forward;

        Quaternion worldCorrection = Quaternion.FromToRotation(boneForwardWorld, desiredForwardWorld);

        Quaternion parentWorldRot = parentT.rotation;
        Quaternion localAxisCorrection =
        Quaternion.Inverse(parentWorldRot) * worldCorrection * parentWorldRot;

        // 🔧 Fix: flip forward/back
        localAxisCorrection = localAxisCorrection * Quaternion.Euler(0f, 180f, 0f);

        // Create job
        job = new LookAtJobBoneIK
        {
            bone = playerAnimator.BindStreamTransform(boneT),
            parent = playerAnimator.BindStreamTransform(parentT),
            target = playerAnimator.BindSceneTransform(target),
            weight = 0f,
            axisOffset = localAxisCorrection
        };

        // Create script playable with input slot
        lookAtPlayable = AnimationScriptPlayable.Create(playableGraph, job);
        lookAtPlayable.SetInputCount(1);
        // Connect finalMixer ➜ lookAtPlayable
        lookAtPlayable.ConnectInput(0, finalMixer, 0);
        lookAtPlayable.SetInputWeight(0, 1f);

        // Output
        var playableOutput = AnimationPlayableOutput.Create(playableGraph, "Animation", playerAnimator);
        playableOutput.SetSourcePlayable(lookAtPlayable);

        // Play!
        playableGraph.Play();
    }

    public void SetLookAtWeight(float newWeight)
    {
        if (!lookAtPlayable.IsValid())
        {
            Debug.Log("look at playable not valid");
            return;
        }

        var currentJob = lookAtPlayable.GetJobData<LookAtJobBoneIK>();
        currentJob.weight = newWeight; // Smooth transition
        lookAtPlayable.SetJobData(currentJob);
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

    public void SpawnArrows() => Runner.Spawn(arrows);

    public void PlayJumpSoundEffect() => footstepSource.PlayOneShot(jumpClip);

    public void PlayRollSoundEffect() => footstepSource.PlayOneShot(rollClip);

    public void CheckGround()
    {
        if (Runner.LagCompensation.Raycast(groundDetector.position, Vector3.down, 10f, Object.InputAuthority, out hit, groundMask, HitOptions.IncludePhysX))
        {
            if (hit.GameObject == null) return;

            if (hit.GameObject.tag == "BattleAreaStage" || hit.GameObject.tag == "WaitingAreaStage") CurrentGround = Ground.TERRAIN;
            else if (hit.GameObject.tag == "Stone") CurrentGround = Ground.STONE;
            else if (hit.GameObject.tag == "Dirt") CurrentGround = Ground.DIRT;
            else if (hit.GameObject.tag == "Wood") CurrentGround = Ground.WOOD;
        }
    }

    public void PlayFootstepSound()
    {
        GetTerrainTexture();

        if (CurrentGround == MainCorePlayable.Ground.TERRAIN)
        {
            if (textureValues[0] > 0)
            {
                footstepSource.PlayOneShot(GetClip(grassClip));
            }
            if (textureValues[1] > 0)
            {
                footstepSource.PlayOneShot(GetClip(dirtClip));
            }
        }
        else if (CurrentGround == MainCorePlayable.Ground.DIRT)
            footstepSource.PlayOneShot(GetClip(dirtClip));
        else if (CurrentGround == MainCorePlayable.Ground.STONE)
            footstepSource.PlayOneShot(GetClip(stoneClip));
        else if (CurrentGround == MainCorePlayable.Ground.WOOD)
            footstepSource.PlayOneShot(GetClip(woodClip));
    }

    AudioClip GetClip(AudioClip[] clipArray)
    {
        if (audioClipIndex > clipArray.Length)
            audioClipIndex = 0;

        selectedClip = clipArray[audioClipIndex];

        previousClip = selectedClip;
        return selectedClip;
    }


    public void GetTerrainTexture()
    {
        ConvertPosition(transform.position);
    }

    private void ConvertPosition(Vector3 playerPosition)
    {
        Terrain tempterrain = ownObjectEnabler.ServerManager.battleFieldArena;

        if (tempterrain == null || tempterrain.terrainData == null)
            return;

        // Get terrain dimensions
        TerrainData terrainData = tempterrain.terrainData;
        Vector3 terrainSize = terrainData.size;
        int alphamapWidth = terrainData.alphamapWidth;
        int alphamapHeight = terrainData.alphamapHeight;

        // Convert world position to normalized [0,1] terrain coordinates
        Vector3 relativePos = playerPosition - tempterrain.transform.position;
        Vector3 normalizedPos = new Vector3(
            relativePos.x / terrainSize.x,
            0,
            relativePos.z / terrainSize.z
        );

        // Clamp and convert to alphamap coordinates
        normalizedPos.x = Mathf.Clamp01(normalizedPos.x);
        normalizedPos.z = Mathf.Clamp01(normalizedPos.z);

        posX = Mathf.FloorToInt(normalizedPos.x * (alphamapWidth - 1));
        posZ = Mathf.FloorToInt(normalizedPos.z * (alphamapHeight - 1));

        CheckTexture(tempterrain);
    }

    private void CheckTexture(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;

        // Verify array bounds
        if (posX < 0 || posX >= terrainData.alphamapWidth ||
            posZ < 0 || posZ >= terrainData.alphamapHeight)
            return;

        float[,,] aMap = terrainData.GetAlphamaps(posX, posZ, 1, 1);
        int numTextures = aMap.GetLength(2);

        // Ensure textureValues array matches available textures
        if (textureValues.Length < numTextures)
            Array.Resize(ref textureValues, numTextures);

        for (int i = 0; i < numTextures; i++)
            textureValues[i] = aMap[0, 0, i];
    }
}

public struct LookAtJobBoneIK : IAnimationJob
{
    public TransformStreamHandle bone;     // rotated bone (Chest/UpperChest recommended)
    public TransformStreamHandle parent;   // bone parent (for world->local conversion)
    public TransformSceneHandle target;    // world target
    public float weight;

    // Local-space correction so "bone forward" matches Unity's +Z lookrotation expectation
    public Quaternion axisOffset;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (weight <= 0f) return;

        if (!bone.IsValid(stream) || !parent.IsValid(stream) || !target.IsValid(stream))
            return;

        // Current local rotation (already includes blended animation)
        Quaternion baseLocal = bone.GetRotation(stream);

        Vector3 bonePos = bone.GetPosition(stream);
        Vector3 targetPos = target.GetPosition(stream);

        Vector3 dirW = targetPos - bonePos;
        if (dirW.sqrMagnitude < 1e-6f)
            return;

        dirW.Normalize();

        // World desired look rotation (+Z forward)
        Quaternion worldLook = Quaternion.LookRotation(dirW, Vector3.up);

        // Convert world -> local (parent space)
        Quaternion parentWorld = parent.GetRotation(stream);
        Quaternion localLook = Quaternion.Inverse(parentWorld) * worldLook;

        // Apply correction for rigs where bone's forward axis isn't +Z
        Quaternion desiredLocal = localLook * axisOffset;

        // Blend from current animation pose to IK pose
        Quaternion finalLocal = Quaternion.Slerp(baseLocal, desiredLocal, weight);

        bone.SetRotation(stream, finalLocal);
    }
}
