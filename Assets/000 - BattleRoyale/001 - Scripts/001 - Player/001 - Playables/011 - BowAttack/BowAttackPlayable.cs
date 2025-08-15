using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class BowAttackPlayable : NetworkBehaviour
{
    [SerializeField] private MainCorePlayable mainCorePlayable;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private DeathMovement deathMovement;
    [SerializeField] private HealPlayables heal;
    [SerializeField] private RepairArmorPlayables repairArmorPlayables;
    [SerializeField] private PlayerNetworkLoader playerNetworkLoader;
    [SerializeField] private BulletObjectPool bulletObjectPool;


    [Space]
    [SerializeField] private NetworkObject bulletNO;
    [SerializeField] private LayerMask raycastLayerMask;

    [Space]
    [SerializeField] private SimpleKCC characterController;

    [Space]
    [SerializeField] private AnimationClip idleClip;
    [SerializeField] private AnimationClip reloadBowClip;
    [SerializeField] private AnimationClip shootClip;

    [Space]
    [SerializeField] private AnimationClip idleCrouchClip;
    [SerializeField] private AnimationClip shootCrouchClip;

    [Header("DEBUGGER LOCAL")]
    [SerializeField] private float idleWeight;
    [SerializeField] private float shootWeight;
    [SerializeField] private float reloadBowWeight;
    [SerializeField] private float idleCrouchWeight;
    [SerializeField] private float shootCrouchWeight;

    [field: Header("DEBUGGER NETWORK")]
    [Networked][field: SerializeField] public bool Attacking { get; set; } // Current combo step
    [Networked][field: SerializeField] public bool Reloading { get; set; } // Time when the last attack was performed
    [Networked] public NetworkButtons PreviousButtons { get; set; }

    //  ============================

    private AnimationMixerPlayable movementMixer;
    private List<AnimationClipPlayable> clipPlayables;

    private ChangeDetector _changeDetector;

    MyInput controllerInput;

    //  ============================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    private void Update()
    {
        AnimationBlend();
    }

    public override void Render()
    {
        if (movementMixer.IsValid() && !HasStateAuthority && playerInventory.WeaponIndex == 3)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(Attacking):

                        if (Attacking)
                        {
                            if (playerInventory.SecondaryWeapon.muzzleFlash != null)
                                playerInventory.SecondaryWeapon.muzzleFlash.SetActive(true);

                            AnimationClipPlayable shootPlayable;

                            if (playerController.IsCrouch)
                            {
                                shootPlayable = clipPlayables[3];
                                shootPlayable.SetTime(0f); // Reset time
                                shootPlayable.Play();    // Start playing

                                playerInventory.SecondaryWeaponSFX.PlayGunshot();
                                break;
                            }

                            shootPlayable = clipPlayables[1];
                            shootPlayable.SetTime(0f); // Reset time
                            shootPlayable.Play();    // Start playing

                            playerInventory.SecondaryWeaponSFX.PlayGunshot();
                        }
                        break;
                    case nameof(Reloading):
                        if (Reloading)
                        {
                            var reloadPlayable = clipPlayables[2];
                            reloadPlayable.SetTime(0);
                            reloadPlayable.Play();
                        }
                        break;
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Attack();

        PreviousButtons = input.Buttons;
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 5);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var shootPlayable = AnimationClipPlayable.Create(graph, shootClip);
        clipPlayables.Add(shootPlayable);

        var reloadBowPlayable = AnimationClipPlayable.Create(graph, reloadBowClip);
        clipPlayables.Add(reloadBowPlayable);

        var crouchIdlePlayable = AnimationClipPlayable.Create(graph, idleCrouchClip);
        clipPlayables.Add(crouchIdlePlayable);

        var crouchShootPlayable = AnimationClipPlayable.Create(graph, shootCrouchClip);
        clipPlayables.Add(crouchShootPlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(shootPlayable, 0, movementMixer, 1);
        graph.Connect(reloadBowPlayable, 0, movementMixer, 2);
        graph.Connect(crouchIdlePlayable, 0, movementMixer, 3);
        graph.Connect(crouchShootPlayable, 0, movementMixer, 4);
    }

    private void AnimationBlend()
    {
        if (Object == null) return;

        if (movementMixer.IsValid())
        {
            if (Attacking)
            {
                if (playerController.IsCrouch)
                {
                    shootCrouchWeight = Mathf.MoveTowards(shootCrouchWeight, 1f, Time.deltaTime * 4);
                    shootWeight = Mathf.MoveTowards(shootWeight, 0f, Time.deltaTime * 4);
                }
                else
                {
                    shootWeight = Mathf.MoveTowards(shootWeight, 1f, Time.deltaTime * 4);
                    shootCrouchWeight = Mathf.MoveTowards(shootCrouchWeight, 0f, Time.deltaTime * 4);
                }

                reloadBowWeight = Mathf.MoveTowards(reloadBowWeight, 0f, Time.deltaTime * 4);
                idleWeight = Mathf.MoveTowards(idleWeight, 0f, Time.deltaTime * 4);
                idleCrouchWeight = Mathf.MoveTowards(idleCrouchWeight, 0f, Time.deltaTime * 4);
            }
            else if (Reloading)
            {
                reloadBowWeight = Mathf.MoveTowards(reloadBowWeight, 1f, Time.deltaTime * 4);
                idleWeight = Mathf.MoveTowards(idleWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.MoveTowards(shootWeight, 0f, Time.deltaTime * 4);
                idleCrouchWeight = Mathf.MoveTowards(idleCrouchWeight, 0f, Time.deltaTime * 4);
                shootCrouchWeight = Mathf.MoveTowards(shootCrouchWeight, 0f, Time.deltaTime * 4);
            }
            else
            {
                if (playerController.IsCrouch)
                {
                    idleCrouchWeight = Mathf.MoveTowards(idleCrouchWeight, 1f, Time.deltaTime * 4);
                    idleWeight = Mathf.MoveTowards(idleWeight, 0f, Time.deltaTime * 4);
                }
                else
                {
                    idleWeight = Mathf.MoveTowards(idleWeight, 1f, Time.deltaTime * 4);
                    idleCrouchWeight = Mathf.MoveTowards(idleCrouchWeight, 0f, Time.deltaTime * 4);
                }

                reloadBowWeight = Mathf.MoveTowards(reloadBowWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.MoveTowards(shootWeight, 0f, Time.deltaTime * 4);
                shootCrouchWeight = Mathf.MoveTowards(shootCrouchWeight, 0f, Time.deltaTime * 4);
            }


            movementMixer.SetInputWeight(0, idleWeight); // Idle animation active
            movementMixer.SetInputWeight(1, shootWeight);
            movementMixer.SetInputWeight(2, reloadBowWeight);
            movementMixer.SetInputWeight(3, idleCrouchWeight);
            movementMixer.SetInputWeight(4, shootCrouchWeight);
        }
    }

    private void Attack()
    {
        if (!HasStateAuthority) return;

        if (playerInventory.WeaponIndex != 3) return;

        if (playerInventory.SecondaryWeapon == null) return;

        if (playerInventory.SecondaryWeapon.WeaponID != "004") return;

        if (!Attacking && !Reloading && !playerController.IsProne && controllerInput.HoldInputButtons.WasPressed(PreviousButtons, HoldInputButtons.Shoot))
        {
            if (playerInventory.SecondaryWeapon.Ammo <= 0 && playerInventory.ArrowAmmoCount <= 0) return;

            Attacking = true;

            SpawnBullet();
        }
    }

    private async void SpawnBullet()
    {
        Ray tempRay = new Ray(controllerInput.CameraHitOrigin, controllerInput.CameraHitDirection);

        LagCompensatedHit hit = new LagCompensatedHit();
        bool validTargetFound = false;
        Vector3 raystart = tempRay.origin;
        float resettemptime = Runner.Tick + (shootClip.length + 0.25f);

        while (!validTargetFound)
        {
            if (Runner.LagCompensation.Raycast(raystart, tempRay.direction, 999f, Object.InputAuthority, out hit, raycastLayerMask, HitOptions.IncludePhysX))
            {
                NetworkObject hitObject = hit.Hitbox?.Root.Object;

                if (hitObject != null && hitObject.InputAuthority == Object.InputAuthority)
                {
                    raystart = hit.Point + tempRay.direction * 0.2f;
                    continue;
                }

                validTargetFound = true;
            }

            if (Runner.Tick >= resettemptime)
                break;

            await Task.Yield();
        }

        if (validTargetFound)
        {
            if (hit.Hitbox != null)
            {
                PlayerHealth playerHealth = hit.Hitbox.Root.GetComponent<PlayerHealth>();

                string tag = hit.Hitbox.tag;

                float tempdamage = tag switch
                {
                    "Head" => 75f,
                    "Body" => 55f,
                    "Thigh" => 45f,
                    "Shin" => 40f,
                    "Foot" => 35f,
                    "Arm" => 50f,
                    "Forearm" => 40f,
                    _ => 0f
                };

                Debug.Log($"Hit by {playerNetworkLoader.Username} to {hit.Hitbox.Root.GetBehaviour<PlayerNetworkLoader>().Username}, damage: {tempdamage} in {tag}");

                playerHealth.ReduceHealth(tempdamage, playerNetworkLoader.Username, mainCorePlayable.Object);
            }

            Vector3 mouseWorldPosition = hit.Point;

            Vector3 aimDir = (mouseWorldPosition - playerInventory.SecondaryWeapon.impactPoint.transform.transform.position).normalized;

            bulletObjectPool.TempArrows[bulletObjectPool.CurrentArrowIndex].GetComponent<ArrowController>().Fire(playerInventory.SecondaryWeapon.impactPoint, aimDir, hit);

            bulletObjectPool.SetEnabledArrow();

            AnimationClipPlayable shootPlayable;

            if (playerController.IsCrouch)
            {
                shootPlayable = clipPlayables[4];
                shootPlayable.SetTime(0f); // Reset time
                shootPlayable.Play();    // Start playing
            }
            else
            {
                shootPlayable = clipPlayables[1];
                shootPlayable.SetTime(0f); // Reset time
                shootPlayable.Play();    // Start playing
            }

            if (playerInventory.SecondaryWeapon.Ammo > 0)
                playerInventory.SecondaryWeapon.Ammo -= 1;
            else
                playerInventory.ArrowAmmoCount -= 1;

            Invoke(nameof(ReloadBow), shootClip.length + 0.25f);
        }
        else
            ResetAttack();
    }

    private void ReloadBow()
    {
        Attacking = false;

        if (playerInventory.ArrowAmmoCount <= 0 && playerInventory.SecondaryWeapon.Ammo <= 0)
        {
            ResetAttack();
            return;
        }

        var cooldownPlayable = clipPlayables[2];
        cooldownPlayable.SetTime(0f); // Reset time
        cooldownPlayable.Play();    // Start playing

        Reloading = true;

        Invoke(nameof(ResetAttack), reloadBowClip.length + 0.25f);
    }

    private void ResetAttack()
    {
        Reloading = false;
        Attacking = false;
    }

    private void SetAttackTickRate(AnimationClipPlayable playables)
    {
        double syncedTime = mainCorePlayable.TickRateAnimation % playables.GetAnimationClip().length;

        // Get the current playable time
        double currentPlayableTime = playables.GetTime();

        // Check if the time difference exceeds the threshold
        if (Mathf.Abs((float)(currentPlayableTime - syncedTime)) > 5f)
        {
            // If the animation is looping or still playing, set the time
            if (currentPlayableTime < playables.GetAnimationClip().length)
            {
                playables.SetTime(syncedTime);
            }
            else
            {
                playables.SetTime(0);
            }
        }
        else
        {
            playables.SetTime(0);
        }
    }

    public Playable GetPlayable()
    {
        return movementMixer;
    }
}
