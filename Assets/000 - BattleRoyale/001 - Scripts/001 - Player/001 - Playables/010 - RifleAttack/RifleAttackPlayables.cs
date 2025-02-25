using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class RifleAttackPlayables : NetworkBehaviour
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
    [SerializeField] private AnimationClip reloadRifleClip;
    [SerializeField] private AnimationClip shootCooldownClip;
    [SerializeField] private AnimationClip shootClip;

    [Space]
    [SerializeField] private AnimationClip idleCrouchClip;
    [SerializeField] private AnimationClip shootCrouchClip;

    [Header("DEBUGGER LOCAL")]
    [SerializeField] private float idleWeight;
    [SerializeField] private float shootWeight;
    [SerializeField] private float reloadRifleWeight;
    [SerializeField] private float shootCooldownWeight;
    [SerializeField] private float idleCrouchWeight;
    [SerializeField] private float shootCrouchWeight;

    [field: Header("DEBUGGER NETWORK")]
    [Networked][field: SerializeField] public bool Attacking { get; set; } // Current combo step
    [Networked][field: SerializeField] public bool ShootCooldown { get; set; } // Current combo step
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

    public override void FixedUpdateNetwork()
    {
        InputControlls();
    }

    private void Update()
    {
        AnimationBlend();
    }

    public override void Render()
    {
        if (movementMixer.IsValid() && !HasStateAuthority && characterController.IsGrounded && playerInventory.WeaponIndex == 3)
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(ShootCooldown):
                        if (!HasInputAuthority) return;

                        if (ShootCooldown)
                        {
                            var cooldownPlayable = clipPlayables[2];
                            cooldownPlayable.SetTime(0f); // Reset time
                            cooldownPlayable.Play();    // Start playing

                            playerInventory.SecondaryWeaponSFX.PlayShootCooldown();
                        }
                        break;
                    case nameof(Attacking):

                        if (Attacking)
                        {
                            if (playerInventory.SecondaryWeapon.muzzleFlash != null)
                                playerInventory.SecondaryWeapon.muzzleFlash.SetActive(true);

                            AnimationClipPlayable shootPlayable;

                            if (playerController.IsCrouch)
                            {
                                shootPlayable = clipPlayables[5];
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
                            var reloadPlayable = clipPlayables[3];
                            reloadPlayable.SetTime(0);
                            reloadPlayable.Play();
                            playerInventory.SecondaryWeaponSFX.PlayReload();
                        }
                        break;
                }
            }
        }
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 7);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var shootPlayable = AnimationClipPlayable.Create(graph, shootClip);
        clipPlayables.Add(shootPlayable);

        var shootCooldownPlayable = AnimationClipPlayable.Create(graph, shootCooldownClip);
        clipPlayables.Add(shootCooldownPlayable);

        var reloadRiflePlayable = AnimationClipPlayable.Create(graph, reloadRifleClip);
        clipPlayables.Add(reloadRiflePlayable);

        var crouchIdlePlayable = AnimationClipPlayable.Create(graph, idleCrouchClip);
        clipPlayables.Add(crouchIdlePlayable);

        var crouchShootPlayable = AnimationClipPlayable.Create(graph, shootCrouchClip);
        clipPlayables.Add(crouchShootPlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(shootPlayable, 0, movementMixer, 1);
        graph.Connect(shootCooldownPlayable, 0, movementMixer, 2);
        graph.Connect(reloadRiflePlayable, 0, movementMixer, 3);
        graph.Connect(crouchIdlePlayable, 0, movementMixer, 4);
        graph.Connect(crouchShootPlayable, 0, movementMixer, 5);
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Attack();
        Reload();

        PreviousButtons = input.Buttons;
    }

    private void Reload()
    {
        if (!HasStateAuthority) return;

        if (playerInventory.WeaponIndex != 3) return;

        if (playerInventory.SecondaryWeapon == null) return;

        if (playerInventory.SecondaryWeapon.WeaponID != "003") return;

        if (playerInventory.RifleAmmoCount <= 0) return;

        if (playerInventory.SecondaryWeapon.Ammo >= 10) return;

        if (!Attacking && !ShootCooldown && !Reloading && !playerController.IsProne && controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Reload))
        {
            Reloading = true;

            var reloadPlayable = clipPlayables[3];
            reloadPlayable.SetTime(0);
            reloadPlayable.Play();

            Invoke(nameof(ReloadAmmo), reloadRifleClip.length * 0.8f);
            Invoke(nameof(ResetReload), reloadRifleClip.length + 0.25f);
        }
    }

    private void ReloadAmmo()
    {
        int ammoNeeded = 10 - playerInventory.SecondaryWeapon.Ammo; // How much ammo is needed to fill the magazine
        int ammoToLoad = Mathf.Min(ammoNeeded, playerInventory.RifleAmmoCount); // Take only what's available

        playerInventory.SecondaryWeapon.Ammo += ammoToLoad;
        playerInventory.RifleAmmoCount -= ammoToLoad;
    }

    private void ResetReload()
    {
        Reloading = false;
    }

    private void Attack()
    {
        if (!HasStateAuthority) return;

        if (playerInventory.WeaponIndex != 3) return;

        if (playerInventory.SecondaryWeapon == null) return;

        if (playerInventory.SecondaryWeapon.WeaponID != "003") return;

        if (!Attacking && !ShootCooldown && !Reloading && !playerController.IsProne && controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Melee))
        {
            if (playerInventory.SecondaryWeapon.Ammo <= 0) return;

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
            else
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

            Vector3 aimDir = (mouseWorldPosition - playerInventory.SecondaryWeapon.impactPoint.position).normalized;

            bulletObjectPool.TempBullets[bulletObjectPool.CurrentBulletIndex].GetComponent<BulletController>().Fire(playerInventory.SecondaryWeapon.impactPoint.position, hit);

            bulletObjectPool.SetEnabledBullet();

            AnimationClipPlayable shootPlayable;

            if (playerController.IsCrouch)
            {
                shootPlayable = clipPlayables[5];
                shootPlayable.SetTime(0f); // Reset time
                shootPlayable.Play();    // Start playing
            }
            else
            {
                shootPlayable = clipPlayables[1];
                shootPlayable.SetTime(0f); // Reset time
                shootPlayable.Play();    // Start playing
            }

            playerInventory.SecondaryWeapon.Ammo -= 1;

            Invoke(nameof(ShootCoolDown), shootClip.length + 0.25f);
        }
    }

    private void ShootCoolDown()
    {
        var cooldownPlayable = clipPlayables[2];
        cooldownPlayable.SetTime(0f); // Reset time
        cooldownPlayable.Play();    // Start playing

        ShootCooldown = true;

        Invoke(nameof(ResetAttack), shootCooldownClip.length);
    }

    private void ResetAttack()
    {
        Attacking = false;
        ShootCooldown = false;
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
                    shootCrouchWeight = Mathf.Lerp(shootCrouchWeight, 1f, Time.deltaTime * 4);
                    shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
                }
                else
                {
                    shootWeight = Mathf.Lerp(shootWeight, 1f, Time.deltaTime * 4);
                    shootCrouchWeight = Mathf.Lerp(shootCrouchWeight, 0f, Time.deltaTime * 4);
                }

                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 0f, Time.deltaTime * 4);
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 0f, Time.deltaTime * 4);
                idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                idleCrouchWeight = Mathf.Lerp(idleCrouchWeight, 0f, Time.deltaTime * 4);
            }
            if (ShootCooldown)
            {
                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 1f, Time.deltaTime * 4);
                idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
                idleCrouchWeight = Mathf.Lerp(idleCrouchWeight, 0f, Time.deltaTime * 4);
                shootCrouchWeight = Mathf.Lerp(shootCrouchWeight, 0f, Time.deltaTime * 4);
            }
            else if (Reloading)
            {
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 1f, Time.deltaTime * 4);
                idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
                idleCrouchWeight = Mathf.Lerp(idleCrouchWeight, 0f, Time.deltaTime * 4);
                shootCrouchWeight = Mathf.Lerp(shootCrouchWeight, 0f, Time.deltaTime * 4);
            }
            else
            {
                if (playerController.IsCrouch)
                {
                    idleCrouchWeight = Mathf.Lerp(idleCrouchWeight, 1f, Time.deltaTime * 4);
                    idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                }
                else
                {
                    idleWeight = Mathf.Lerp(idleWeight, 1f, Time.deltaTime * 4);
                    idleCrouchWeight = Mathf.Lerp(idleCrouchWeight, 0f, Time.deltaTime * 4);
                }

                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 0f, Time.deltaTime * 4);
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
                shootCrouchWeight = Mathf.Lerp(shootCrouchWeight, 0f, Time.deltaTime * 4);
            }


            movementMixer.SetInputWeight(0, idleWeight); // Idle animation active
            movementMixer.SetInputWeight(1, shootWeight);
            movementMixer.SetInputWeight(2, shootCooldownWeight);
            movementMixer.SetInputWeight(3, reloadRifleWeight);
            movementMixer.SetInputWeight(4, idleCrouchWeight);
            movementMixer.SetInputWeight(5, shootCrouchWeight);
        }
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
