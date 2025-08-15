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
    public LookAtJobBoneIK job;
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
        finalMixer.SetLayerMaskFromAvatarMask(0, lowerBodyMask);
        lowerBodyChanger.Initialize(lowerBodyMovement.IdlePlayable);

        playableGraph.Connect(upperBodyMovement.Initialize(), 0, finalMixer, 1);
        finalMixer.SetInputWeight(1, 1f);
        finalMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);
        upperBodyChanger.Initialize(upperBodyMovement.IdlePlayables);

        // Setup constraint job
        job = new LookAtJobBoneIK
        {
            bone = playerAnimator.BindStreamTransform(bone),
            target = playerAnimator.BindSceneTransform(target),
            weight = 0f,
            restRotation = bone.rotation
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
            return;

        var currentJob = lookAtPlayable.GetJobData<LookAtJobBoneIK>();
        currentJob.weight = newWeight; // Smooth transition
        lookAtPlayable.SetJobData(currentJob);
    }

    public void SetAnimationUpperTick() => PlayableUpperBodyAnimationTick = Runner.Tick;

    public void SetAnimationLowerTick() => PlayableLowerBodyAnimationTick = Runner.Tick;

    public void SpawnBullets(Vector3 startPos, LagCompensatedHit hit)
    {
        Runner.Spawn(bullets, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
        {
            obj.GetComponent<BulletController>().Fire(startPos, hit);
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
        Terrain tempterrain = ownObjectEnabler.ServerManager.CurrentGameState == GameState.WAITINGAREA
            ? ownObjectEnabler.ServerManager.waitingAreaArena
            : ownObjectEnabler.ServerManager.battleFieldArena;

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
    public TransformStreamHandle bone; // The bone to rotate
    public TransformSceneHandle target; // The target to look at
    public float weight;
    public Quaternion restRotation;

    public void ProcessRootMotion(AnimationStream stream) { }

    public void ProcessAnimation(AnimationStream stream)
    {
        if (!bone.IsValid(stream) || !target.IsValid(stream))
            return;

        Quaternion baseRotation = bone.GetRotation(stream);

        if (weight <= 0f)
        {
            bone.SetRotation(stream, baseRotation);
            return;
        }

        Vector3 forward = target.GetRotation(stream) * Vector3.forward;
        Vector3 up = Vector3.up; // bone's current up in local space

        Quaternion lookRotation = Quaternion.LookRotation(forward, up);
        Quaternion finalRot = Quaternion.Slerp(baseRotation, lookRotation, weight);

        bone.SetRotation(stream, finalRot);
    }
}