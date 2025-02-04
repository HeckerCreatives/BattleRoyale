using Fusion;
using Fusion.Addons.SimpleKCC;
using System.Collections;
using System.Collections.Generic;
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


    [Header("DEBUGGER LOCAL")]
    [SerializeField] private float idleWeight;
    [SerializeField] private float shootWeight;
    [SerializeField] private float reloadRifleWeight;
    [SerializeField] private float shootCooldownWeight;

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
                        if (ShootCooldown)
                        {
                            var cooldownPlayable = clipPlayables[2];
                            cooldownPlayable.SetTime(0f); // Reset time
                            cooldownPlayable.Play();    // Start playing
                        }
                        break;
                    case nameof(Attacking):

                        if (Attacking)
                        {
                            playerInventory.SecondaryWeapon.muzzleFlash.SetActive(true);

                            var shootPlayable = clipPlayables[1];
                            shootPlayable.SetTime(0f); // Reset time
                            shootPlayable.Play();    // Start playing
                        }
                        break;
                }
            }

            AnimationBlend();
        }
    }

    public void Initialize(PlayableGraph graph)
    {
        clipPlayables = new List<AnimationClipPlayable>();

        movementMixer = AnimationMixerPlayable.Create(graph, 4);

        var idlePlayable = AnimationClipPlayable.Create(graph, idleClip);
        clipPlayables.Add(idlePlayable);

        var shootPlayable = AnimationClipPlayable.Create(graph, shootClip);
        clipPlayables.Add(shootPlayable);

        var shootCooldownPlayable = AnimationClipPlayable.Create(graph, shootCooldownClip);
        clipPlayables.Add(shootCooldownPlayable);

        var reloadRiflePlayable = AnimationClipPlayable.Create(graph, reloadRifleClip);
        clipPlayables.Add(reloadRiflePlayable);

        graph.Connect(idlePlayable, 0, movementMixer, 0);
        graph.Connect(shootPlayable, 0, movementMixer, 1);
        graph.Connect(shootCooldownPlayable, 0, movementMixer, 2);
        graph.Connect(reloadRiflePlayable, 0, movementMixer, 3);
    }

    private void InputControlls()
    {
        if (GetInput<MyInput>(out var input) == false) return;

        controllerInput = input;

        Attack();

        PreviousButtons = input.Buttons;
    }

    private void Attack()
    {
        if (!HasStateAuthority) return;

        if (playerInventory.WeaponIndex != 3) return;

        if (playerInventory.SecondaryWeapon == null) return;

        if (!Attacking && !ShootCooldown && !playerController.IsProne && controllerInput.Buttons.WasPressed(PreviousButtons, InputButton.Melee) && characterController.IsGrounded)
        {
            //if (playerInventory.SecondaryWeapon.Ammo <= 0) return;

            Attacking = true;

            var shootPlayable = clipPlayables[1];
            SetAttackTickRate(shootPlayable); // Reset time
            shootPlayable.Play();    // Start playing

            SpawnBullet();

            playerInventory.SecondaryWeapon.Ammo -= 1;

            Invoke(nameof(ShootCoolDown), shootClip.length + 0.25f);
        }
    }

    private void SpawnBullet()
    {
        Ray tempRay = new Ray(controllerInput.CameraHitOrigin, controllerInput.CameraHitDirection);

        if (Runner.LagCompensation.Raycast(tempRay.origin, tempRay.direction, 999f, Object.InputAuthority, out LagCompensatedHit hit, raycastLayerMask, HitOptions.IncludePhysX))
        {
            Vector3 mouseWorldPosition = hit.Point;

            Vector3 aimDir = (mouseWorldPosition - playerInventory.SecondaryWeapon.impactPoint.position).normalized;

            NetworkObject tempbullet = Runner.Spawn(bulletNO, playerInventory.SecondaryWeapon.impactPoint.position, Quaternion.LookRotation(aimDir, Vector3.up), onBeforeSpawned: (NetworkRunner runner, NetworkObject nobject) =>
            {
                nobject.GetComponent<BulletController>().Fire(Object.InputAuthority, playerInventory.SecondaryWeapon.impactPoint.position, mainCorePlayable.Object, hit, playerNetworkLoader.Username);
            });

            tempbullet.GetComponent<BulletController>().CanTravel = true;
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
        if (movementMixer.IsValid())
        {
            if (Attacking)
            {
                shootWeight = Mathf.Lerp(shootWeight, 1f, Time.deltaTime * 4);
                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 0f, Time.deltaTime * 4);
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 0f, Time.deltaTime * 4);
                idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
            }
            if (ShootCooldown)
            {
                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 1f, Time.deltaTime * 4);
                idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
            }
            else if (Reloading)
            {
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 1f, Time.deltaTime * 4);
                idleWeight = Mathf.Lerp(idleWeight, 0f, Time.deltaTime * 4);
                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
            }
            else
            {
                idleWeight = Mathf.Lerp(idleWeight, 1f, Time.deltaTime * 4);
                shootCooldownWeight = Mathf.Lerp(shootCooldownWeight, 0f, Time.deltaTime * 4);
                reloadRifleWeight = Mathf.Lerp(reloadRifleWeight, 0f, Time.deltaTime * 4);
                shootWeight = Mathf.Lerp(shootWeight, 0f, Time.deltaTime * 4);
            }


            movementMixer.SetInputWeight(0, idleWeight); // Idle animation active
            movementMixer.SetInputWeight(1, shootWeight);
            movementMixer.SetInputWeight(2, shootCooldownWeight);
            movementMixer.SetInputWeight(3, reloadRifleWeight);
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
