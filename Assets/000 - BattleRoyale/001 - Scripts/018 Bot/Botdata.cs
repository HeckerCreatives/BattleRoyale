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
    [Space]
    [SerializeField] private List<ParticleSystem> bloodParticles;

    [Space]
    [SerializeField] private LayerMask enemyLayerMask;
    [SerializeField] private float attackRadius;
    [SerializeField] private Transform impactFirstFistPoint;
    [SerializeField] private Transform impactSecondFistPoint;

    [Header("DEBUGGER")]
    [SerializeField] private int bloodIndex;

    [field: Header("DEBUGGER")]
    [field: SerializeField][Networked] public DedicatedServerManager ServerManager { get; set; }
    [field: SerializeField][Networked] public int BotIndex { get; set; }
    [field: SerializeField][Networked] public string BotName { get; set; }
    [field: SerializeField][Networked] public float CurrentHealth { get; set; }
    [field: SerializeField][Networked] public bool IsDead { get; set; }
    [field: SerializeField][Networked] public int Hitted { get; set; }
    [field: SerializeField][Networked] public bool IsHit { get; set; }
    [field: SerializeField][Networked] public bool IsStagger { get; set; }
    [field: SerializeField][Networked] public bool IsGettingUp { get; set; }
    [field: SerializeField][Networked] public TickTimer DeadTimer { get; set; }

    //  ======================

    private ChangeDetector _changeDetector;

    private readonly List<LagCompensatedHit> hitsFirstFist = new List<LagCompensatedHit>();
    private readonly List<LagCompensatedHit> hitsSecondFist = new List<LagCompensatedHit>();

    private readonly HashSet<NetworkObject> hitEnemiesFirstFist = new();
    private readonly HashSet<NetworkObject> hitEnemiesSecondFist = new();

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

        if (IsDead && DeadTimer.Expired(Runner))
            Runner.Despawn(Object);
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

    #region DAMAGE RECEIVED

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

                ServerManager.KillNotifController.RPC_ReceiveKillNotification($"{BotName} was killed outside safe area");

                DeadTimer = TickTimer.CreateFromSeconds(Runner, 5f);
            }
        }
    }

    public void FallDamage(float damage)
    {
        if (ServerManager.CurrentGameState != GameState.ARENA) return;

        if (!ServerManager.DonePlayerBattlePositions) return;

        if (IsDead) return;

        CurrentHealth -= damage;

        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                ServerManager.Bots.Remove(BotIndex);

                ServerManager.KillNotifController.RPC_ReceiveKillNotification($"{BotName} killed themselves");

                DeadTimer = TickTimer.CreateFromSeconds(Runner, 5f);
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
            //nobject.GetComponent<PlayerGameStats>().HitPoints += remainingDamage;
        }

        // Check if player is dead
        if (CurrentHealth <= 0)
        {
            if (HasStateAuthority)
                IsDead = true;

            if (IsDead)
            {
                if (nobject.tag == "Player")
                    nobject.GetComponent<PlayerGameStats>().KillCount++;

                ServerManager.Bots.Remove(BotIndex);

                ServerManager.KillNotifController.RPC_ReceiveKillNotification($"{killer} KILLED {BotName}");
                DeadTimer = TickTimer.CreateFromSeconds(Runner, 5f);
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

    private void DespawnBot() => Runner.Despawn(Object);

    #endregion

    #region DAMAGE GIVEN

    public void PerformFirstAttack(bool isFinal = false)
    {
        int hitCount = Runner.LagCompensation.OverlapSphere(
            impactFirstFistPoint.position,
            attackRadius,
            Object.InputAuthority,
            hitsFirstFist,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsFirstFist[i].Hitbox;
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == null)
            {
                continue;
            }

            if (hitObject == Object) continue;

            if (hitObject.tag == "Bot")
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();

                if (tempdata.IsStagger) return;
                if (tempdata.IsGettingUp) return;
                if (tempdata.IsDead) return;

                if (!hitEnemiesFirstFist.Contains(hitObject))
                {
                    hitEnemiesFirstFist.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    if (isFinal) tempdata.IsStagger = true;
                    else tempdata.IsHit = true;

                    tempdata.ApplyDamage(tempdamage, BotName, Object);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();

                if (tempplayables.healthV2.IsStagger) return;
                if (tempplayables.healthV2.IsGettingUp) return;

                // Avoid duplicate hits
                if (!hitEnemiesFirstFist.Contains(hitObject))
                {
                    // Mark as hit
                    hitEnemiesFirstFist.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();

                    if (isFinal) healthV2.IsStagger = true;
                    else healthV2.IsHit = true;

                    healthV2.ApplyDamage(tempdamage, BotName, Object);
                }
            }
        }
    }

    public void PerformSecondAttack()
    {
        int hitCount = Runner.LagCompensation.OverlapSphere(
            impactSecondFistPoint.position,
            attackRadius,
            Object.InputAuthority,
            hitsSecondFist,
            enemyLayerMask,
            HitOptions.IgnoreInputAuthority
        );

        for (int i = 0; i < hitCount; i++)
        {
            var hitbox = hitsSecondFist[i].Hitbox;
            if (hitbox == null)
            {
                continue;
            }

            NetworkObject hitObject = hitbox.transform.root.GetComponent<NetworkObject>();

            if (hitObject == Object) continue;

            if (hitObject == null)
            {
                continue;
            }

            if (hitObject.tag == "Bot")
            {
                Botdata tempdata = hitObject.GetComponent<Botdata>();

                if (tempdata.IsStagger) return;
                if (tempdata.IsGettingUp) return;
                if (tempdata.IsDead) return;

                if (!hitEnemiesSecondFist.Contains(hitObject))
                {
                    hitEnemiesSecondFist.Add(hitObject);

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    tempdata.IsHit = true;

                    tempdata.ApplyDamage(tempdamage, BotName, Object);
                }
            }
            else
            {
                PlayerPlayables tempplayables = hitObject.GetComponent<PlayerPlayables>();

                if (tempplayables.healthV2.IsStagger) return;
                if (tempplayables.healthV2.IsGettingUp) return;

                // Avoid duplicate hits
                if (!hitEnemiesSecondFist.Contains(hitObject))
                {
                    // Mark as hit
                    hitEnemiesSecondFist.Add(hitObject);

                    //bareHandsMovement.CanDamage = false;

                    string tag = hitbox.tag;

                    float tempdamage = tag switch
                    {
                        "Head" => 30f,
                        "Body" => 25f,
                        "Thigh" => 20f,
                        "Shin" => 15f,
                        "Foot" => 10f,
                        "Arm" => 20f,
                        "Forearm" => 15f,
                        _ => 0f
                    };

                    PlayerHealthV2 healthV2 = hitObject.GetComponent<PlayerHealthV2>();

                    healthV2.IsHit = true;

                    healthV2.ApplyDamage(tempdamage, BotName, Object);
                }
            }
        }
    }

    public void ResetFirstAttack()
    {
        hitEnemiesFirstFist.Clear();
    }

    public void ResetSecondAttack()
    {
        hitEnemiesSecondFist.Clear();
    }

    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(impactFirstFistPoint.position, attackRadius);


        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(impactSecondFistPoint.position, attackRadius);
    }
}
