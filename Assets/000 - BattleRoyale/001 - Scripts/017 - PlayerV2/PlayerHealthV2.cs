using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthV2 : NetworkBehaviour
{
    [SerializeField] private PlayerGameStats playerGameStats;

    [Space]
    [SerializeField] private float startingHealth;

    [Space]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [Space]
    [SerializeField] private PlayerPlayables playerPlayables;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] public DedicatedServerManager ServerManager { get; set; }
    [Networked][field: SerializeField] public float CurrentHealth { get; set; }
    [Networked][field: SerializeField] public float CurrentArmor { get; set; }
    [Networked][field: SerializeField] public bool IsHit { get; set; }
    [Networked][field: SerializeField] public bool IsSecondHit { get; set; }
    [Networked][field: SerializeField] public bool IsStagger { get; set; }
    [Networked][field: SerializeField] public bool IsDead { get; set; }

    //  ========================

    private ChangeDetector _changeDetector;

    //==========================

    public override void Spawned()
    {
        CurrentHealth = startingHealth;

        if (!HasInputAuthority) return;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        healthSlider.value = CurrentHealth / 100;
        armorSlider.value = CurrentArmor / 100;
    }

    public override void Render()
    {
        if (!HasInputAuthority) return;

        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(CurrentHealth):
                    healthSlider.value = CurrentHealth / 100;
                    break;
                case nameof(CurrentArmor):
                    armorSlider.value = CurrentArmor / 100;
                    break;
            }
        }
    }

    public void ApplyDamage(float damage, string killer, NetworkObject nobject)
    {
        if (IsDead) return;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        //DamagedHit++;

        float remainingDamage = damage;

        // Apply damage to the shield first
        if (CurrentArmor > 0)
        {
            if (CurrentArmor >= remainingDamage)
            {
                CurrentArmor -= remainingDamage;
                remainingDamage = 0; // Shield absorbed all damage
            }
            else
            {
                remainingDamage -= CurrentArmor;
                CurrentArmor = 0; // Shield fully depleted
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
}
