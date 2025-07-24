using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Mood
{
    JOMARIE, // DUMB BOT
    ALEX,   // SCARED BOT
    BIEN // AGGRESSIVE AND SMART BOT
}

public class Botdata : NetworkBehaviour
{
    [SerializeField] private List<ParticleSystem> bloodParticles;

    [Header("DEBUGGER")]
    [SerializeField] private int bloodIndex;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: SerializeField][Networked] public int BotIndex { get; set; }
    [field: SerializeField][Networked] public string BotName { get; set; }
    [field: SerializeField][Networked] public float CurrentHealth { get; set; }
    [field: SerializeField][Networked] public bool IsDead { get; set; }
    [field: SerializeField][Networked] public int Hitted { get; set; }

    //  ======================

    private ChangeDetector _changeDetector;

    //  ======================

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
            CurrentHealth = 100f;
    }

    public override void FixedUpdateNetwork()
    {
        CircleDamage();
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(Hitted):

                    if (HasStateAuthority) return;

                    DamageIndicator();

                    break;
            }
        }
    }

    private void DamageIndicator()
    {
        if (bloodIndex >= bloodParticles.Count - 1)
            bloodIndex = 0;
        else
            bloodIndex++;

        bloodParticles[bloodIndex].Play();
    }

    private void CircleDamage()
    {
        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (!ServerManager.DonePlayerBattlePositions) return;

        if (IsDead) return;

        float distanceFromCenter = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(ServerManager.SafeZone.transform.position.x, 0, ServerManager.SafeZone.transform.position.z));
        float radius = ServerManager.SafeZone.CurrentShrinkSize.x / 2; // Adjust based on your implementation

        if (distanceFromCenter > radius)
            CurrentHealth -= Runner.DeltaTime * ((ServerManager.SafeZone.ShrinkSizeIndex + 1) / 2);

        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                ServerManager.Bots.Remove(BotIndex);

                RPC_ReceiveKillNotification($"{BotName} was killed outside safe area");
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

        // Apply damage to the shield first
        //if (inventory.Armor != null)
        //{
        //    if (inventory.Armor.Supplies > 0)
        //    {
        //        if (inventory.Armor.Supplies >= remainingDamage)
        //        {
        //            inventory.Armor.Supplies -= Convert.ToInt32(remainingDamage);
        //            remainingDamage = 0; // Shield absorbed all damage
        //        }
        //        else
        //        {
        //            remainingDamage -= inventory.Armor.Supplies;
        //            inventory.Armor.Supplies = 0; // Shield fully depleted
        //        }
        //    }
        //}

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
                ServerManager.Bots.Remove(BotIndex);
                ServerManager.RemainingPlayers.Remove(Object.InputAuthority);

                RPC_ReceiveKillNotification($"{killer} KILLED {BotName}");
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


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ReceiveKillNotification(string message)
    {
        ServerManager.KillNotifController.ShowIndicator(message);
    }
}
