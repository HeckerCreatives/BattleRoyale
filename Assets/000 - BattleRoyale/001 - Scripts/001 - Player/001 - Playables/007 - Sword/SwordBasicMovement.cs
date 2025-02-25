using Fusion.Addons.SimpleKCC;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Fusion.NetworkBehaviour;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class SwordBasicMovement : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SimpleKCC characterController;
    [SerializeField] private PlayerInventory playerInventory;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip sideStrafeLeftClip;
    [SerializeField] private AnimationClip sideStrafeRightClip;
    [SerializeField] private AnimationClip backwardClip;
    [SerializeField] private AnimationClip sprintClip;

    [Space]
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private int weaponIndex;
    [SerializeField] private string weaponid;
    [SerializeField] private AudioClip[] grassClip;
    [SerializeField] private AudioClip[] dirtClip;
    [SerializeField] private AudioClip[] stoneClip;
    [SerializeField] private AudioClip[] woodClip;

    [Header("DEBUGGER")]
    [SerializeField] private float[] textureValues;
    [SerializeField] Vector3 terrainPosition;
    [SerializeField] Vector3 mapPosition;
    [SerializeField] float xCoord;
    [SerializeField] float zCoord;
    [SerializeField] int posX;
    [SerializeField] int posZ;
    [SerializeField] AudioClip selectedClip;
    [SerializeField] AudioClip previousClip;

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private float lastFrameTime = 0f;
    private float[] footstepPercents = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1f };
    private float[] footstepTimes;

    //  ============================

    public override void Spawned()
    {
        textureValues = new float[2];
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 5);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var leftPlayable = AnimationClipPlayable.Create(graph, sideStrafeLeftClip);
        clipPlayables.Add(leftPlayable);

        var rightPlayable = AnimationClipPlayable.Create(graph, sideStrafeRightClip);
        clipPlayables.Add(rightPlayable);

        var backwardPlayable = AnimationClipPlayable.Create(graph, backwardClip);
        clipPlayables.Add(backwardPlayable);

        var sprintPlayable = AnimationClipPlayable.Create(graph, sprintClip);
        clipPlayables.Add(sprintPlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(leftPlayable, 0, movementMixer, 1);
        graph.Connect(rightPlayable, 0, movementMixer, 2);
        graph.Connect(backwardPlayable, 0, movementMixer, 3);
        graph.Connect(sprintPlayable, 0, movementMixer, 4);

        // Initialize all weights to 0
        for (int i = 0; i < movementMixer.GetInputCount(); i++)
        {
            movementMixer.SetInputWeight(i, 0.0f);
        }

        footstepTimes = new float[footstepPercents.Length];

        for (int i = 0; i < footstepPercents.Length; i++)
        {
            footstepTimes[i] = sprintClip.length * footstepPercents[i];
        }
    }

    public override void Render()
    {
        if (!HasInputAuthority && !HasStateAuthority && movementMixer.IsValid())
        {
            foreach (var playables in clipPlayables)
            {
                double currentPlayableTime = playables.GetTime();
                if (Mathf.Abs((float)(currentPlayableTime - mainCorePlayable.TickRateAnimation)) > 0.01f) // Adjust threshold as needed
                    playables.SetTime(mainCorePlayable.TickRateAnimation);
            }

            UpdateBlendTreeWeights();
        }

        PlayFootstepSound();
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

        float xMovement = playerController.XMovement;
        float yMovement = playerController.YMovement;

        // Calculate weights for each movement
        float idleWeight = (xMovement == 0 && yMovement == 0) ? 1f : 0f;
        float leftWeight = Mathf.Clamp01(-xMovement); // Left strafe
        float rightWeight = Mathf.Clamp01(xMovement); // Right strafe
        float backwardWeight = Mathf.Clamp01(-yMovement); // Backward
        float sprintWeight = Mathf.Clamp01(yMovement > 0 ? yMovement : 0); // Forward (Sprint)

        // Normalize weights for side and forward/backward movement
        float totalWeight = leftWeight + rightWeight + backwardWeight + sprintWeight;
        if (totalWeight > 1f)
        {
            leftWeight /= totalWeight;
            rightWeight /= totalWeight;
            backwardWeight /= totalWeight;
            sprintWeight /= totalWeight;
        }

        // Apply weights to mixer inputs
        movementMixer.SetInputWeight(0, idleWeight);
        movementMixer.SetInputWeight(1, leftWeight);
        movementMixer.SetInputWeight(2, rightWeight);
        movementMixer.SetInputWeight(3, backwardWeight);
        movementMixer.SetInputWeight(4, sprintWeight);
    }

    private void PlayFootstepSound()
    {
        if (!movementMixer.IsValid() || HasStateAuthority || !characterController.IsGrounded || mainCorePlayable.ServerManager == null || playerController.IsProne || playerController.IsCrouch) return;
        if (playerInventory.WeaponIndex != weaponIndex) return;

        if (!IsValidWeapon(playerInventory.WeaponIndex)) return;
        if (playerController.XMovement == 0 && playerController.YMovement == 0) return;

        GetTerrainTexture();

        float currentTime = (float)clipPlayables[4].GetTime() % sprintClip.length;

        for (int i = 0; i < footstepTimes.Length; i++)
        {
            if (lastFrameTime < footstepTimes[i] && currentTime >= footstepTimes[i])
            {
                if (mainCorePlayable.CurrentGround == MainCorePlayable.Ground.TERRAIN)
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
                else if (mainCorePlayable.CurrentGround == MainCorePlayable.Ground.DIRT)
                    footstepSource.PlayOneShot(GetClip(dirtClip));
                else if (mainCorePlayable.CurrentGround == MainCorePlayable.Ground.STONE)
                    footstepSource.PlayOneShot(GetClip(stoneClip));
                else if (mainCorePlayable.CurrentGround == MainCorePlayable.Ground.WOOD)
                    footstepSource.PlayOneShot(GetClip(woodClip));
            }
        }

        lastFrameTime = currentTime;
    }

    AudioClip GetClip(AudioClip[] clipArray)
    {
        int attempts = 3;
        selectedClip = clipArray[UnityEngine.Random.Range(0, clipArray.Length - 1)];

        while (selectedClip == previousClip && attempts > 0)
        {
            selectedClip =
            clipArray[UnityEngine.Random.Range(0, clipArray.Length - 1)];

            attempts--;
        }

        previousClip = selectedClip;
        return selectedClip;
    }

    private bool IsValidWeapon(int weaponIndex)
    {
        return weaponIndex switch
        {
            2 => playerInventory.PrimaryWeapon.WeaponID == weaponid,
            3 => playerInventory.SecondaryWeapon.WeaponID == weaponid,
            _ => true
        };
    }

    public void GetTerrainTexture()
    {
        ConvertPosition(transform.position);
    }

    private void ConvertPosition(Vector3 playerPosition)
    {
        Terrain tempterrain = mainCorePlayable.ServerManager.CurrentGameState == GameState.WAITINGAREA ? mainCorePlayable.ServerManager.waitingAreaArena : mainCorePlayable.ServerManager.battleFieldArena;

        terrainPosition = playerPosition - tempterrain.transform.position;
        mapPosition = new Vector3(terrainPosition.x / tempterrain.terrainData.size.x, 0,
        terrainPosition.z / tempterrain.terrainData.size.z);

        xCoord = mapPosition.x * tempterrain.terrainData.alphamapWidth;
        zCoord = mapPosition.z * tempterrain.terrainData.alphamapHeight;

        posX = (int)xCoord;
        posZ = (int)zCoord;

        CheckTexture(tempterrain);
    }
    private void CheckTexture(Terrain terrain)
    {
        float[,,] aMap = terrain.terrainData.GetAlphamaps(posX, posZ, 1, 1);

        int numTextures = aMap.GetLength(2); // Get the number of available textures

        if (numTextures < 2)
        {
            Debug.LogError($"Expected at least 2 terrain textures, but found {numTextures}");
            return;
        }

        textureValues[0] = aMap[0, 0, 0];

        if (numTextures > 1)
        {
            textureValues[1] = aMap[0, 0, 1];
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
