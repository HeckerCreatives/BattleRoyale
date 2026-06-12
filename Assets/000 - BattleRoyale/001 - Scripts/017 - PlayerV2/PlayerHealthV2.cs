using Fusion;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Weapon category that caused a hit — networked as an int alongside Hitted so
// the victim (and every watching peer) plays the matching impact sound.
// Primary weapons (sword/spear) share one sound, secondary (rifle/bow) share
// one, punch keeps its own separate sound.
public static class HitWeaponType
{
    public const int Punch     = 0; // default — fist attacks
    public const int Primary   = 1; // sword "001" / spear "002"
    public const int Secondary = 2; // rifle "003" / bow "004"
    public const int Trap      = 3; // trap damage (optional clip, silent if unassigned)
}

public class PlayerHealthV2 : NetworkBehaviour
{
    public static readonly List<PlayerHealthV2> All = new();

    [SerializeField] private UserData userData;
    [SerializeField] private PlayerGameStats playerGameStats;
    [SerializeField] private Volume postProcessing;
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private PlayerInventoryV2 inventory;
    [SerializeField] private PlayerOwnObjectEnabler playerOwnObjectEnabler;
    [SerializeField] private InvincibleController invincible;

    [Space]
    [SerializeField] private NetworkObject healObj;
    [SerializeField] private NetworkObject repairObj;
    [SerializeField] private NetworkObject rifleAmmoObj;

    [Space]
    [SerializeField] private float startingHealth;
    [SerializeField] private float shaker;

    [Space]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [Space]
    [SerializeField] private Image damageIndicator;

    [Space]
    [SerializeField] private List<ParticleSystem> bloodParticlesA;
    [SerializeField] private ParticleSystem healParticles;
    [SerializeField] private ParticleSystem repairParticles;

    [Space]
    [SerializeField] private AudioClip[] gruntClips;
    [SerializeField] private AudioClip thumpGruntClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioSource damageSource;

    [Header("WEAPON IMPACT SOUNDS (heard when this player is hit)")]
    [Tooltip("Fist hits. If unassigned, falls back to fistSoundController.PlayHit().")]
    [SerializeField] private AudioClip punchHitClip;
    [Tooltip("Shared by sword + spear hits.")]
    [SerializeField] private AudioClip primaryHitClip;
    [Tooltip("Shared by rifle + bow hits.")]
    [SerializeField] private AudioClip secondaryHitClip;
    [Tooltip("Optional — trap damage impact. Silent if unassigned.")]
    [SerializeField] private AudioClip trapHitClip;

    [Header("DEBUGGER")]
    [SerializeField] private int bloodIndex;
    [SerializeField] AudioClip selectedClip;
    [SerializeField] AudioClip previousClip;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] public float CurrentHealth { get; set; }
    //[Networked][field: SerializeField] public bool IsHit { get; set; }
    //[Networked][field: SerializeField] public bool IsHitUpper { get; set; }
    [Networked][field: SerializeField] public int Hitted { get; set; }
    // Written in the same tick as Hitted++, so when the Hitted change is
    // detected on a peer, the snapshot already carries the weapon category.
    [Networked][field: SerializeField] public int LastHitWeaponType { get; set; }
    [Networked][field: SerializeField] public bool IsStagger { get; set; }
    [Networked][field: SerializeField] public bool IsGettingUp { get; set; }
    [Networked][field: SerializeField] public bool DamagedSafeZone { get; set; }
    [Networked][field: SerializeField] public float SafeZoneDamaged { get; set; }
    [Networked][field: SerializeField] public float FallDamageValue { get; set; }
    [Networked][field: SerializeField] public int FallDamage { get; set; }
    [Networked][field: SerializeField] public bool IsDead { get; set; }
    [Networked][field: SerializeField] public int Healed { get; set; }
    [Networked][field: SerializeField] public int Repaired { get; set; }

    //  ========================

    private ChangeDetector _changeDetector;

    int damageIndicatorLT;

    Vignette circleVignette;

    Coroutine ShakerCoroutine;

    //==========================

    public override void Spawned()
    {
        All.Add(this);
        CurrentHealth = startingHealth;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (!HasInputAuthority) return;


        healthSlider.value = CurrentHealth / 100;
        ArmorUI();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        All.Remove(this);
    }

    public override void Render()
    {

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):
                    if (!HasInputAuthority) break;

                    healthSlider.value = CurrentHealth / 100;
                    break;
                case nameof(Hitted):
                    if (HasStateAuthority) break;

                    DamageIndicator();

                    HitSoundEffects();

                    if (!HasInputAuthority) break;

                    if (ShakerCoroutine != null) StopCoroutine(ShakerCoroutine);

                    ShakerCoroutine = StartCoroutine(Shaker());
                    break;
                case nameof(IsStagger):
                    if (HasStateAuthority) break;

                    if (!IsStagger) break;

                    DamageIndicator();

                    break;
                case nameof(SafeZoneDamaged):
                    if (!HasInputAuthority) break;

                    DamageIndicatorWithoutBlood();

                    break;
                case nameof(FallDamage):
                    if (!HasInputAuthority) break;

                    DamageIndicatorWithoutBlood();
                    FallSoundEffects();

                    break;
                case nameof(IsDead):
                    if (!HasInputAuthority) break;
                    DeathSoundEffect();
                    break;
                case nameof(Healed):

                    if (HasStateAuthority) break;

                    healParticles.Play();

                    break;
                case nameof(Repaired):

                    if (HasStateAuthority) break;

                    repairParticles.Play();

                    break;
            }
        }

        if (HasInputAuthority)
            ArmorUI();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        CircleDamage();
    }


    IEnumerator Shaker()
    {
        playerPlayables.CameraShaker(shaker);
        yield return new WaitForSecondsRealtime(0.15f);
        playerPlayables.CameraShaker(0f);
    }

    private void ShakerCamera()
    {

        playerPlayables.CameraShaker(shaker);
    }

    private void OffCameraShaker()
    {

        playerPlayables.CameraShaker(0f);
    }

    private void HitSoundEffects()
    {
        if (HasStateAuthority) return;

        damageSource.PlayOneShot(GetClip(gruntClips));

        // Weapon-category impact sound. Primary (sword/spear) and secondary
        // (rifle/bow) each share one clip; punch keeps its dedicated fist hit.
        switch (LastHitWeaponType)
        {
            case HitWeaponType.Primary:
                if (primaryHitClip != null) damageSource.PlayOneShot(primaryHitClip);
                break;
            case HitWeaponType.Secondary:
                if (secondaryHitClip != null) damageSource.PlayOneShot(secondaryHitClip);
                break;
            case HitWeaponType.Trap:
                if (trapHitClip != null) damageSource.PlayOneShot(trapHitClip);
                break;
            default: // HitWeaponType.Punch
                if (punchHitClip != null)
                    damageSource.PlayOneShot(punchHitClip);
                else
                    playerPlayables.fistSoundController.PlayHit(); // legacy fallback
                break;
        }
    }

    private void FallSoundEffects()
    {
        if (HasStateAuthority) return;

        damageSource.PlayOneShot(thumpGruntClip);

    }

    private void DeathSoundEffect()
    {
        if (HasStateAuthority) return;

        if (CurrentHealth <= 0)
            damageSource.PlayOneShot(deathClip);
    }

    private void ArmorUI()
    {
        if (inventory.Armor == null)
            armorSlider.value = 0;
        else
            armorSlider.value = inventory.Armor.Supplies / 100f;
    }

    public void HealHealth()
    {
        float temphealth = CurrentHealth + 35f;

        CurrentHealth = Mathf.Clamp(temphealth, 0f, 100f);

        inventory.HealCount -= 1;

        Healed++;

        if (HasInputAuthority) healParticles.Play();
    }

    public void RepairArmor()
    {
        float temparmor = inventory.Armor.Supplies + 40f;

        inventory.Armor.Supplies = (int)Mathf.Clamp(temparmor, 0f, 100f);

        inventory.ArmorRepairCount -= 1;

        Repaired++;

        if (HasInputAuthority) repairParticles.Play();
    }

    private void DamageIndicator()
    {
        if (bloodIndex >= bloodParticlesA.Count - 1)
            bloodIndex = 0;
        else
            bloodIndex++;

        bloodParticlesA[bloodIndex].Play();

        if (!HasInputAuthority) return;

        if (damageIndicatorLT != 0) LeanTween.cancel(damageIndicatorLT);

        damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 255f), 0.12f).setEase(LeanTweenType.easeInOutSine).setOnComplete(() =>
        {
            damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 0), 5f).setEase(LeanTweenType.easeInSine).setDelay(2f).id;
        }).id;
    }

    private void DamageIndicatorWithoutBlood()
    {
        if (!HasInputAuthority) return;

        if (damageIndicatorLT != 0) LeanTween.cancel(damageIndicatorLT);

        damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 255f), 0.12f).setEase(LeanTweenType.easeInOutSine).setOnComplete(() =>
        {
            damageIndicatorLT = LeanTween.color(damageIndicator.rectTransform, new Color(255f, 255f, 255f, 0), 5f).setEase(LeanTweenType.easeInSine).setDelay(2f).id;
        }).id;
    }

    private void CircleDamage()
    {
        if (!Runner) return;

        if (!HasStateAuthority) return;

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;

        if (!MultiplayerServerManager.Instance.DonePlayerBattlePositions) return;

        if (IsDead) return;

        if (invincible.IsInvincible) return;

        float distanceFromCenter = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(SafeZoneServerController.Instance.SafeZone.transform.position.x, 0, SafeZoneServerController.Instance.SafeZone.transform.position.z));
        float radius = SafeZoneServerController.Instance.SafeZone.CurrentShrinkSize.x / 2; // Adjust based on your implementation

        if (distanceFromCenter > radius)
        {
            DamagedSafeZone = true;
            SafeZoneDamaged = Runner.Tick;
            CurrentHealth -= Runner.DeltaTime * ((SafeZoneServerController.Instance.SafeZone.ShrinkSizeIndex + 1) / 2);
        }
        else
            DamagedSafeZone = false;

        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                playerGameStats.PlayerPlacement = PlayerJoinedController.Instance.RemainingPlayers.Count;
                PlayerJoinedController.Instance.RemainingPlayers.Remove(playerOwnObjectEnabler.Username.ToString());

                KillNotifServerController.Instance.KillNotifController.RPC_ReceiveKillNotification($"{playerOwnObjectEnabler.Username} was killed outside safe area");

                DropItems();
            }
        }
    }

    public void ApplyDamage(float damage, string killer, NetworkObject nobject, int weaponType = HitWeaponType.Punch)
    {
        if (!HasStateAuthority) return;

        if (IsDead) return;

        if (invincible.IsInvincible) return;

        LastHitWeaponType = weaponType;
        Hitted++;

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;

        //DamagedHit++;

        float remainingDamage = damage;

        //  Block
        if (playerPlayables.PlayableLowerBoddyAnimationIndex == 12)
        {
            remainingDamage -= (remainingDamage * .15f);
        }

        // Apply damage to the shield first
        if (inventory.Armor != null)
        {
            if (inventory.Armor.Supplies > 0)
            {
                if (inventory.Armor.Supplies >= remainingDamage)
                {
                    inventory.Armor.Supplies -= Convert.ToInt32(remainingDamage);
                    remainingDamage = 0; // Shield absorbed all damage
                }
                else
                {
                    remainingDamage -= inventory.Armor.Supplies;
                    inventory.Armor.Supplies = 0; // Shield fully depleted
                }
            }
        }

        // Apply remaining damage to health
        if (remainingDamage > 0)
        {
            CurrentHealth = (byte)Mathf.Max(0, CurrentHealth - remainingDamage);

            if (nobject.CompareTag("Player"))
                nobject.GetComponent<PlayerGameStats>().HitPoints += remainingDamage;
        }

        // Check if player is dead
        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                if (nobject.CompareTag("Player"))
                    nobject.GetComponent<PlayerGameStats>().KillCount++;

                playerGameStats.PlayerPlacement = PlayerJoinedController.Instance.RemainingPlayers.Count;
                PlayerJoinedController.Instance.RemainingPlayers.Remove(playerOwnObjectEnabler.Username.ToString());

                KillNotifServerController.Instance.KillNotifController.RPC_ReceiveKillNotification($"{killer} KILLED {playerOwnObjectEnabler.Username}");

                DropItems();
            }
            //string statustext = killer == loader.Username ? $"{loader.Username} Killed themself" : $"{killer} KILLED {loader.Username}";

            //RPC_ReceiveKillNotification(statustext);
        }
    }

    private void DropItems()
    {
        if (inventory.PrimaryWeapon != null)
        {
            inventory.PrimaryWeapon.DropWeapon();
            inventory.PrimaryWeapon = null;
        }

        if (inventory.SecondaryWeapon != null)
        {
            inventory.SecondaryWeapon.DropWeapon();
            inventory.SecondaryWeapon = null;
        }

        if (inventory.Armor != null)
        {
            inventory.Armor.DropArmor();
            inventory.Armor = null;
        }

        if (inventory.HealCount > 0)
        {
            Runner.Spawn(healObj, transform.position, Quaternion.identity, null, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<HealWeaponItem>().InitializeItem(transform.position, Quaternion.identity, inventory.HealCount);
            });
        }

        if (inventory.ArmorRepairCount > 0)
        {
            Runner.Spawn(repairObj, transform.position, Quaternion.identity, null, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<RepairWeaponItem>().InitializeItem(transform.position, Quaternion.identity, inventory.HealCount);
            });
        }

        if (inventory.BowMagazine > 0)
        {
            inventory.MagazineContainer.DropWeapon();
            inventory.MagazineContainer = null;
        }

        if (inventory.RifleMagazine > 0)
        {
            Runner.Spawn(rifleAmmoObj, transform.position, Quaternion.identity, null, onBeforeSpawned: (NetworkRunner runner, NetworkObject obj) =>
            {
                obj.GetComponent<AmmoRifleWeaponItem>().InitializeOnStart(transform.position, Quaternion.identity, inventory.RifleMagazine);
            });
        }
    }

    public void FallDamae()
    {
        if (!HasStateAuthority) return;

        if (IsDead) return;

        if (invincible.IsInvincible) return;

        FallDamage = Runner.Tick;

        if (MultiplayerServerManager.Instance.CurrentGameState != GameState.ARENA) return;

        CurrentHealth -= FallDamageValue;

        FallDamageValue = 0f;

        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                playerGameStats.PlayerPlacement = PlayerJoinedController.Instance.RemainingPlayers.Count;
                PlayerJoinedController.Instance.RemainingPlayers.Remove(playerOwnObjectEnabler.Username.ToString());
                KillNotifServerController.Instance.KillNotifController.RPC_ReceiveKillNotification($"{playerOwnObjectEnabler.Username} killed by fall damage");

                DropItems();
            }
        }
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
}
