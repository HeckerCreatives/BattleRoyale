using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlayerHealthV2 : NetworkBehaviour
{
    [SerializeField] private UserData userData;
    [SerializeField] private PlayerGameStats playerGameStats;
    [SerializeField] private Volume postProcessing;
    [SerializeField] private KillNotificationController killNotif;
    [SerializeField] private PlayerPlayables playerPlayables;
    [SerializeField] private PlayerInventoryV2 inventory;
    [SerializeField] private PlayerOwnObjectEnabler playerOwnObjectEnabler;

    [Space]
    [SerializeField] private float startingHealth;

    [Space]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [Space]
    [SerializeField] private Image damageIndicator;

    [Space]
    [SerializeField] private List<ParticleSystem> bloodParticlesA;
    [SerializeField] private ParticleSystem healParticles;
    [SerializeField] private ParticleSystem repairParticles;

    [Header("DEBUGGER")]
    [SerializeField] private int bloodIndex;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] public DedicatedServerManager ServerManager { get; set; }
    [Networked][field: SerializeField] public float CurrentHealth { get; set; }
    [Networked][field: SerializeField] public bool IsHit { get; set; }
    [Networked][field: SerializeField] public int Hitted { get; set; }
    [Networked][field: SerializeField] public bool IsSecondHit { get; set; }
    [Networked][field: SerializeField] public bool IsStagger { get; set; }
    [Networked][field: SerializeField] public bool DamagedSafeZone { get; set; }
    [Networked][field: SerializeField] public bool FallDamage { get; set; }
    [Networked][field: SerializeField] public bool IsDead { get; set; }

    //  ========================

    private ChangeDetector _changeDetector;

    int damageIndicatorLT;

    Vignette circleVignette;

    //==========================

    public override void Spawned()
    {
        CurrentHealth = startingHealth;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (!HasInputAuthority) return;


        healthSlider.value = CurrentHealth / 100;
        ArmorUI();
    }

    public override void Render()
    {

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):
                    if (!HasInputAuthority) return;

                    healthSlider.value = CurrentHealth / 100;
                    break;
                case nameof(Hitted):

                    DamageIndicator();

                    break;
                case nameof(IsSecondHit):

                    if (!IsSecondHit) return;

                    DamageIndicator();

                    break;
                case nameof(IsStagger):

                    if (!IsStagger) return;

                    DamageIndicator();

                    break;
                case nameof(DamagedSafeZone):

                    if (!DamagedSafeZone) return;

                    DamageIndicatorWithoutBlood();

                    break;
                case nameof(FallDamage):

                    if (!FallDamage) return;

                    DamageIndicatorWithoutBlood();

                    break;
            }
        }

        if (HasInputAuthority)
        {
            ArmorUI();

            if (postProcessing.profile.TryGet<Vignette>(out circleVignette))
            {
                if (DamagedSafeZone)
                {
                    circleVignette.active = true;
                    circleVignette.intensity.value = Mathf.MoveTowards(circleVignette.intensity.value, 0.5f, Time.deltaTime * 2f);
                }
                else
                {
                    circleVignette.intensity.value = Mathf.MoveTowards(circleVignette.intensity.value, 0f, Time.deltaTime * 2f);
                    if (circleVignette.intensity.value == 0)
                    {
                        circleVignette.active = false;
                    }
                }
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        CircleDamage();
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
        float temphealth = CurrentHealth + 30f;

        CurrentHealth = Mathf.Clamp(temphealth, 0f, 100f);

        inventory.HealCount -= 1;

        if (HasInputAuthority) healParticles.Play();
    }

    public void RepairArmor()
    {
        float temparmor = inventory.Armor.Supplies + 30f;

        inventory.Armor.Supplies = (int)Mathf.Clamp(temparmor, 0f, 100f);

        inventory.ArmorRepairCount -= 1;

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
        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (!ServerManager.DonePlayerBattlePositions) return;

        if (IsDead) return;

        float distanceFromCenter = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(ServerManager.SafeZone.transform.position.x, 0, ServerManager.SafeZone.transform.position.z));
        float radius = ServerManager.SafeZone.CurrentShrinkSize.x / 2; // Adjust based on your implementation

        if (distanceFromCenter > radius)
        {
            DamagedSafeZone = true;
            CurrentHealth -= Runner.DeltaTime * ((ServerManager.SafeZone.ShrinkSizeIndex + 1) / 2);
        }
        else
            DamagedSafeZone = false;

        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                playerGameStats.PlayerPlacement = ServerManager.RemainingPlayers.Count;
                ServerManager.RemainingPlayers.Remove(Object.InputAuthority);

                RPC_ReceiveKillNotification($"{playerOwnObjectEnabler.Username} killed outside safe area");
            }
        }
    }

    public void ApplyDamage(float damage, string killer, NetworkObject nobject)
    {
        if (IsDead) return;

        Hitted++;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        //DamagedHit++;

        float remainingDamage = damage;

        //  Block
        if (playerPlayables.PlayableAnimationIndex == 12)
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
            nobject.GetComponent<PlayerGameStats>().HitPoints += remainingDamage;
        }

        // Check if player is dead
        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                nobject.GetComponent<PlayerGameStats>().KillCount++;
                playerGameStats.PlayerPlacement = ServerManager.RemainingPlayers.Count;
                ServerManager.RemainingPlayers.Remove(Object.InputAuthority);


                RPC_ReceiveKillNotification($"{killer} KILLED {playerOwnObjectEnabler.Username}");
            }
            //string statustext = killer == loader.Username ? $"{loader.Username} Killed themself" : $"{killer} KILLED {loader.Username}";

            //RPC_ReceiveKillNotification(statustext);

            //if (playerInventory.PrimaryWeapon != null)
            //{
            //    playerInventory.PrimaryWeapon.DropPrimaryWeapon();
            //    playerInventory.PrimaryWeapon = null;
            //}

            //if (playerInventory.Shield != null)
            //{
            //    playerInventory.Shield.DropShield();
            //    playerInventory.Shield = null;
            //}
        }
    }

    public void FallDamae(float damage)
    {
        if (IsDead) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                playerGameStats.PlayerPlacement = ServerManager.RemainingPlayers.Count;
                ServerManager.RemainingPlayers.Remove(Object.InputAuthority);
                RPC_ReceiveKillNotification($"{playerOwnObjectEnabler.Username} killed by fall damage");
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReceiveKillNotification(string message)
    {
        if (HasStateAuthority) return;

        killNotif.ShowMessage(message);
    }
}
