using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthV2 : NetworkBehaviour
{
    [SerializeField] private float startingHealth;

    [Space]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [field: Header("DEBUGGER")]
    [Networked][field: SerializeField] public DedicatedServerManager ServerManager { get; set; }
    [Networked][field: SerializeField] public float CurrentHealth { get; set; }
    [Networked][field: SerializeField] public float CurrentArmor { get; set; }

    //  ========================

    private ChangeDetector _changeDetector;

    //==========================

    public override void Spawned()
    {
        CurrentHealth = startingHealth;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (!HasInputAuthority) return;

        healthSlider.value = CurrentHealth / 100;
        armorSlider.value = CurrentArmor / 100;
    }

    public override void Render()
    {
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

    public void ReduceHealth(float damage, string killer, NetworkObject nobject)
    {
        if (CurrentHealth <= 0) return;

        //DamagedHit++;

        if (ServerManager.CurrentGameState != GameState.ARENA) return;

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
            nobject.GetComponent<PlayerGameOverScreen>().HitPoints += remainingDamage;
        }

        // Check if player is dead
        if (CurrentHealth <= 0)
        {
            //deathMovement.MakePlayerDead();
            //nobject.GetComponent<KillCountCounterController>().KillCount++;
            //gameOverScreen.PlayerPlacement = ServerManager.RemainingPlayers.Count;
            //ServerManager.RemainingPlayers.Remove(Object.InputAuthority);

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
